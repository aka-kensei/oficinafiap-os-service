namespace Oficina.Contracts.Events;

public record PagamentoRecusado(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid OrcamentoId,
    string Motivo,
    DateTime RecusadoEm);
