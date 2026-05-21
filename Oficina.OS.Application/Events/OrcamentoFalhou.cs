namespace Oficina.Contracts.Events;

public record OrcamentoFalhou(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string Motivo,
    DateTime FalhouEm);
