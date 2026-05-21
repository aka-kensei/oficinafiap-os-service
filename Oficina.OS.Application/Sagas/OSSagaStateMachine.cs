using MassTransit;
using Microsoft.Extensions.Logging;
using Oficina.Contracts.Events;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Enums;
using Oficina.OS.Domain.Repositories;

namespace Oficina.OS.Application.Sagas;

/// <summary>
/// Máquina de estado da Saga da OS — orquestrada pelo OS Service.
///
/// Fluxo feliz:
///   OSCriada → DiagnosticoIniciado → OrcamentoPropostoPelaOficina
///   → (publica) OrcamentoSolicitado → OrcamentoGerado
///   → (espera) OSAprovadaPeloCliente → PagamentoAprovado
///   → ExecucaoIniciada → ExecucaoFinalizada → Concluída
///
/// Compensações:
///   - OSReprovadaPeloCliente em AguardandoAprovacaoCliente → estorna estoque + cancela orçamento
///   - PagamentoRecusado em AguardandoPagamento → volta para AguardandoAprovacaoCliente
///   - OrcamentoFalhou → marca saga como Falhou
///   - Timeout (24h) sem pagamento → publica OSCancelada
/// </summary>
public class OSSagaStateMachine : MassTransitStateMachine<SagaOS>
{
    public State AguardandoDiagnostico { get; private set; } = null!;
    public State EmDiagnostico { get; private set; } = null!;
    public State OrcamentoSolicitado { get; private set; } = null!;
    public State AguardandoAprovacaoCliente { get; private set; } = null!;
    public State AguardandoPagamento { get; private set; } = null!;
    public State EmExecucao { get; private set; } = null!;
    public State Finalizada { get; private set; } = null!;
    public State Compensando { get; private set; } = null!;

    public Event<OSCriada> OSCriadaEvento { get; private set; } = null!;
    public Event<DiagnosticoIniciado> DiagnosticoIniciadoEvento { get; private set; } = null!;
    public Event<OrcamentoPropostoPelaOficina> OrcamentoPropostoEvento { get; private set; } = null!;
    public Event<OrcamentoGerado> OrcamentoGeradoEvento { get; private set; } = null!;
    public Event<OrcamentoFalhou> OrcamentoFalhouEvento { get; private set; } = null!;
    public Event<OSAprovadaPeloCliente> OSAprovadaEvento { get; private set; } = null!;
    public Event<OSReprovadaPeloCliente> OSReprovadaEvento { get; private set; } = null!;
    public Event<PagamentoAprovado> PagamentoAprovadoEvento { get; private set; } = null!;
    public Event<PagamentoRecusado> PagamentoRecusadoEvento { get; private set; } = null!;
    public Event<ExecucaoIniciada> ExecucaoIniciadaEvento { get; private set; } = null!;
    public Event<ExecucaoFinalizada> ExecucaoFinalizadaEvento { get; private set; } = null!;

    // Schedule desabilitado temporariamente — requer plugin rabbitmq_delayed_message_exchange.
    // Em prod, reabilitar declaracao + Schedule(...) e Unschedule(...) nas transicoes abaixo.
    // public Schedule<SagaOS, OSCancelada> PrazoPagamentoTimeout { get; private set; } = null!;

    public OSSagaStateMachine(ILogger<OSSagaStateMachine> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => OSCriadaEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => DiagnosticoIniciadoEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => OrcamentoPropostoEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => OrcamentoGeradoEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => OrcamentoFalhouEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => OSAprovadaEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => OSReprovadaEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => PagamentoAprovadoEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => PagamentoRecusadoEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => ExecucaoIniciadaEvento, x => x.CorrelateById(c => c.Message.CorrelationId));
        Event(() => ExecucaoFinalizadaEvento, x => x.CorrelateById(c => c.Message.CorrelationId));

        // Schedule(() => PrazoPagamentoTimeout, x => x.PrazoPagamentoTimeoutTokenId, s =>
        // {
        //     s.Delay = TimeSpan.FromHours(24);
        //     s.Received = r => r.CorrelateById(c => c.Message.CorrelationId);
        // });

        Initially(
            When(OSCriadaEvento)
                .Then(ctx =>
                {
                    ctx.Saga.OrdemDeServicoId = ctx.Message.OrdemDeServicoId;
                    ctx.Saga.Transicionar(SagaState.AguardandoDiagnostico, nameof(OSCriada));
                    logger.LogInformation(
                        "Saga {CorrelationId}: OS {OSId} criada — aguardando diagnóstico",
                        ctx.Saga.CorrelationId, ctx.Saga.OrdemDeServicoId);
                })
                .TransitionTo(AguardandoDiagnostico));

        During(AguardandoDiagnostico,
            When(DiagnosticoIniciadoEvento)
                .ThenAsync(ctx => MarcarStatusOSAsync(ctx.GetPayload<IServiceProvider>(),
                    ctx.Saga.OrdemDeServicoId, os => os.MarcarDiagnosticoIniciado()))
                .Then(ctx => ctx.Saga.Transicionar(SagaState.EmDiagnostico, nameof(DiagnosticoIniciado)))
                .TransitionTo(EmDiagnostico));

        During(EmDiagnostico,
            When(OrcamentoPropostoEvento)
                .ThenAsync(async ctx =>
                {
                    var sp = ctx.GetPayload<IServiceProvider>();
                    await MarcarStatusOSAsync(sp, ctx.Saga.OrdemDeServicoId,
                        os => os.MarcarAguardandoAprovacao(ctx.Message.ValorTotal));
                    ctx.Saga.ValorOrcamento = ctx.Message.ValorTotal;
                })
                .Publish(ctx => new OrcamentoSolicitado(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrdemDeServicoId,
                    Guid.Empty,
                    string.Empty,
                    ctx.Message.ValorTotal,
                    ctx.Message.Itens,
                    DateTime.UtcNow))
                .Then(ctx => ctx.Saga.Transicionar(SagaState.OrcamentoSolicitado, nameof(OrcamentoPropostoPelaOficina)))
                .TransitionTo(OrcamentoSolicitado));

        During(OrcamentoSolicitado,
            When(OrcamentoGeradoEvento)
                .Then(ctx =>
                {
                    ctx.Saga.OrcamentoId = ctx.Message.OrcamentoId;
                    ctx.Saga.Transicionar(SagaState.AguardandoAprovacaoCliente, nameof(OrcamentoGerado));
                })
                .TransitionTo(AguardandoAprovacaoCliente),
            When(OrcamentoFalhouEvento)
                .Then(ctx => ctx.Saga.RegistrarFalha(ctx.Message.Motivo))
                .Finalize());

        During(AguardandoAprovacaoCliente,
            When(OSAprovadaEvento)
                // .Schedule(PrazoPagamentoTimeout, ctx => new OSCancelada(
                //     ctx.Saga.CorrelationId,
                //     ctx.Saga.OrdemDeServicoId,
                //     "Pagamento não realizado em 24h",
                //     DateTime.UtcNow))
                .Then(ctx => ctx.Saga.Transicionar(SagaState.AguardandoPagamento, nameof(OSAprovadaPeloCliente)))
                .TransitionTo(AguardandoPagamento),
            When(OSReprovadaEvento)
                // Reprovar pelo cliente equivale a cancelar a OS no nosso fluxo de saga
                // (estorna pecas + termina a OS). os.Reprovar() so volta para Recebida,
                // o que reabriria a OS - nao e a intencao da compensacao.
                .ThenAsync(ctx => CompensarAsync(ctx.GetPayload<IServiceProvider>(),
                    ctx.Saga.OrdemDeServicoId, os => os.Cancelar()))
                .Then(ctx =>
                {
                    // Nao chamar Transicionar(Cancelada) porque "Cancelada" nao e declarado
                    // como State no state machine - causaria UnknownStateException no Finalize.
                    // O .Finalize() abaixo seta CurrentState para "Final" automaticamente.
                    ctx.Saga.UltimoEventoConsumido = nameof(OSReprovadaPeloCliente);
                    ctx.Saga.AtualizadaEm = DateTime.UtcNow;
                    ctx.Saga.ConcluidaEm = DateTime.UtcNow;
                    ctx.Saga.MotivoFalha = ctx.Message.Motivo;
                })
                .Finalize());

        During(AguardandoPagamento,
            When(PagamentoAprovadoEvento)
                // .Unschedule(PrazoPagamentoTimeout)
                .ThenAsync(ctx => MarcarStatusOSAsync(ctx.GetPayload<IServiceProvider>(),
                    ctx.Saga.OrdemDeServicoId, os => os.MarcarEmExecucao()))
                .Then(ctx =>
                {
                    ctx.Saga.PagamentoId = ctx.Message.PagamentoId;
                    ctx.Saga.Transicionar(SagaState.EmExecucao, nameof(PagamentoAprovado));
                })
                .TransitionTo(EmExecucao),
            When(PagamentoRecusadoEvento)
                // .Unschedule(PrazoPagamentoTimeout)
                .Then(ctx => ctx.Saga.Transicionar(SagaState.AguardandoAprovacaoCliente, nameof(PagamentoRecusado)))
                .TransitionTo(AguardandoAprovacaoCliente));

        During(EmExecucao,
            When(ExecucaoFinalizadaEvento)
                .ThenAsync(ctx => MarcarStatusOSAsync(ctx.GetPayload<IServiceProvider>(),
                    ctx.Saga.OrdemDeServicoId, os => os.MarcarFinalizada()))
                .Then(ctx => ctx.Saga.Transicionar(SagaState.Finalizada, nameof(ExecucaoFinalizada)))
                .TransitionTo(Finalizada));

        During(Finalizada,
            Ignore(ExecucaoIniciadaEvento));

        SetCompletedWhenFinalized();
    }

    private static async Task MarcarStatusOSAsync(
        IServiceProvider sp,
        Guid osId,
        Action<OrdemDeServico> acao)
    {
        var repo = (IOrdemDeServicoRepository)sp.GetService(typeof(IOrdemDeServicoRepository))!;
        var uow = (Interfaces.IUnitOfWork)sp.GetService(typeof(Interfaces.IUnitOfWork))!;

        var os = await repo.ObterPorIdCompletaAsync(osId);
        if (os == null) return;

        acao(os);
        await repo.AtualizarAsync(os);
        await uow.SaveChangesAsync();
    }

    private static async Task CompensarAsync(
        IServiceProvider sp,
        Guid osId,
        Action<OrdemDeServico> acaoCompensatoria)
    {
        var repo = (IOrdemDeServicoRepository)sp.GetService(typeof(IOrdemDeServicoRepository))!;
        var pecaRepo = (IPecaRepository)sp.GetService(typeof(IPecaRepository))!;
        var uow = (Interfaces.IUnitOfWork)sp.GetService(typeof(Interfaces.IUnitOfWork))!;

        var os = await repo.ObterPorIdCompletaAsync(osId);
        if (os == null) return;

        foreach (var item in os.ItensPeca)
        {
            var peca = await pecaRepo.ObterPorIdAsync(item.PecaId);
            peca?.AdicionarEstoque(item.Quantidade);
            if (peca != null) await pecaRepo.AtualizarAsync(peca);
        }

        acaoCompensatoria(os);
        await repo.AtualizarAsync(os);
        await uow.SaveChangesAsync();
    }
}
