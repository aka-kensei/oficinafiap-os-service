namespace Oficina.Contracts.Events;

public record OrcamentoPropostoPelaOficina(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    decimal ValorTotal,
    IReadOnlyList<ItemOrcamento> Itens,
    DateTime PropostoEm);
