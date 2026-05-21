namespace Oficina.Contracts.Events;

public record ItemOrcamento(
    string Tipo,
    Guid Id,
    string Descricao,
    int Quantidade,
    decimal PrecoUnitario);

public record OrcamentoSolicitado(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid ClienteId,
    string ClienteEmail,
    decimal ValorTotal,
    IReadOnlyList<ItemOrcamento> Itens,
    DateTime SolicitadoEm);
