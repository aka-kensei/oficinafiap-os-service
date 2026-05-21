using Oficina.OS.Domain.Enums;
using Oficina.OS.Domain.Exceptions;

namespace Oficina.OS.Domain.Entities;

/// <summary>
/// Agregado raiz da OS no microsserviço de Ordens de Serviço.
///
/// Diferença em relação à versão monolítica (Fase 3):
/// transições de status que dependem da oficina (diagnóstico, execução) e do pagamento
/// agora são acionadas via eventos recebidos do Execução Service e do Billing Service.
/// As ações locais (aprovar, reprovar, entregar) seguem expostas pelo OS Service.
/// </summary>
public class OrdemDeServico
{
    public Guid Id { get; private set; }

    public Guid ClienteId { get; private set; }
    public virtual Cliente? Cliente { get; private set; }

    public Guid VeiculoId { get; private set; }
    public virtual Veiculo? Veiculo { get; private set; }

    public StatusOS Status { get; private set; }

    private readonly List<ItemPeca> _itensPeca = new();
    public IReadOnlyCollection<ItemPeca> ItensPeca => _itensPeca.AsReadOnly();

    private readonly List<ItemServico> _itensServico = new();
    public IReadOnlyCollection<ItemServico> ItensServico => _itensServico.AsReadOnly();

    public decimal ValorOrcamento { get; private set; }

    public DateTime DataCriacao { get; private set; }
    public DateTime? DataInicioExecucao { get; private set; }
    public DateTime? DataFinalizacao { get; private set; }

    public decimal? TempoTotalExecucaoHoras => DataInicioExecucao.HasValue && DataFinalizacao.HasValue
        ? (decimal)(DataFinalizacao.Value - DataInicioExecucao.Value).TotalHours
        : null;

    protected OrdemDeServico() { }

    public OrdemDeServico(Guid clienteId, Guid veiculoId)
    {
        Id = Guid.NewGuid();
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOS.Recebida;
        DataCriacao = DateTime.UtcNow;
        ValorOrcamento = 0;
    }

    public void AdicionarPeca(Peca peca, int quantidade)
    {
        if (Status != StatusOS.Recebida && Status != StatusOS.EmDiagnostico)
            throw new DomainException("Não é possível adicionar peças nesta fase da OS.");

        if (_itensPeca.Any(p => p.PecaId == peca.Id))
            throw new DomainException("Peça já adicionada.");

        _itensPeca.Add(new ItemPeca(Id, peca.Id, peca.Descricao, quantidade, peca.PrecoVenda));
        RecalcularOrcamento();
    }

    public void AdicionarServico(Servico servico)
    {
        if (Status != StatusOS.Recebida && Status != StatusOS.EmDiagnostico)
            throw new DomainException("Não é possível adicionar serviços nesta fase da OS.");

        _itensServico.Add(new ItemServico(Id, servico.Id, servico.Descricao, servico.Preco));
        RecalcularOrcamento();
    }

    private void RecalcularOrcamento()
    {
        ValorOrcamento = _itensServico.Sum(s => s.PrecoCobrado)
                       + _itensPeca.Sum(p => p.SubTotal);
    }

    public void MarcarDiagnosticoIniciado()
    {
        if (Status != StatusOS.Recebida)
            throw new DomainException("OS deve estar 'Recebida' para iniciar o diagnóstico.");
        Status = StatusOS.EmDiagnostico;
    }

    public void MarcarAguardandoAprovacao(decimal valorOrcamento)
    {
        if (Status != StatusOS.EmDiagnostico)
            throw new DomainException("OS deve estar em diagnóstico para aguardar aprovação.");
        if (valorOrcamento <= 0)
            throw new DomainException("Valor do orçamento deve ser positivo.");

        ValorOrcamento = valorOrcamento;
        Status = StatusOS.AguardandoAprovacao;
    }

    public void Aprovar()
    {
        if (Status != StatusOS.AguardandoAprovacao)
            throw new DomainException("OS deve estar 'Aguardando Aprovação' para ser aprovada.");
    }

    public void MarcarEmExecucao()
    {
        if (Status != StatusOS.AguardandoAprovacao)
            throw new DomainException("OS deve estar 'Aguardando Aprovação' para entrar em execução.");

        Status = StatusOS.EmExecucao;
        DataInicioExecucao = DateTime.UtcNow;
    }

    public void Reprovar()
    {
        if (Status != StatusOS.AguardandoAprovacao)
            throw new DomainException("OS deve estar 'Aguardando Aprovação' para ser reprovada.");
        Status = StatusOS.Recebida;
    }

    public void MarcarFinalizada()
    {
        if (Status != StatusOS.EmExecucao)
            throw new DomainException("OS deve estar 'Em Execução' para ser finalizada.");

        Status = StatusOS.Finalizada;
        DataFinalizacao = DateTime.UtcNow;
    }

    public void Entregar()
    {
        if (Status != StatusOS.Finalizada)
            throw new DomainException("OS deve estar 'Finalizada' para ser entregue.");
        Status = StatusOS.Entregue;
    }

    public void Cancelar()
    {
        if (Status is StatusOS.EmExecucao or StatusOS.Finalizada or StatusOS.Entregue)
            throw new DomainException("Não é possível cancelar OS em execução ou já entregue.");
        Status = StatusOS.Cancelada;
    }
}
