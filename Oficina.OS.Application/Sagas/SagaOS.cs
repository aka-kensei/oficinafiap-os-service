using MassTransit;
using Oficina.OS.Domain.Enums;
using Oficina.OS.Domain.Exceptions;

namespace Oficina.OS.Application.Sagas;

/// <summary>
/// Estado da Saga orquestrada pelo OS Service — vive na camada Application
/// porque depende de tipos MassTransit (SagaStateMachineInstance, ISagaVersion).
/// Persistida no SQL Server do OS Service (tabela SagasOS).
/// </summary>
public class SagaOS : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }

    public Guid OrdemDeServicoId { get; set; }
    // CurrentState fica vazio até a primeira transição — MassTransit usa o state Initial
    // ate consumir OSCriada. Qualquer string aqui (ex. "Iniciada") precisaria ser declarada
    // como State() na state machine, senão MassTransit dispara UnknownStateException.
    public string CurrentState { get; set; } = string.Empty;

    public Guid? OrcamentoId { get; set; }
    public Guid? PagamentoId { get; set; }
    public decimal? ValorOrcamento { get; set; }

    public DateTime CriadaEm { get; set; }
    public DateTime AtualizadaEm { get; set; }
    public DateTime? ConcluidaEm { get; set; }
    public DateTime? PrazoLimitePagamento { get; set; }

    public string? UltimoEventoConsumido { get; set; }
    public string? MotivoFalha { get; set; }

    public Guid? PrazoPagamentoTimeoutTokenId { get; set; }

    public SagaOS()
    {
        CriadaEm = DateTime.UtcNow;
        AtualizadaEm = DateTime.UtcNow;
    }

    public void Transicionar(SagaState novoEstado, string evento)
    {
        CurrentState = novoEstado.ToString();
        AtualizadaEm = DateTime.UtcNow;
        UltimoEventoConsumido = evento;

        if (novoEstado is SagaState.Concluida or SagaState.Cancelada or SagaState.Falhou)
            ConcluidaEm = DateTime.UtcNow;
    }

    public void RegistrarFalha(string motivo)
    {
        MotivoFalha = motivo;
        Transicionar(SagaState.Falhou, "Falha");
    }

    public bool EstaConcluida() => ConcluidaEm.HasValue;

    public bool PodeReceberEvento(string eventoEsperado)
    {
        if (EstaConcluida())
            throw new DomainException(
                $"Saga {CorrelationId} já concluída ({CurrentState}) — não pode processar '{eventoEsperado}'.");
        return true;
    }
}
