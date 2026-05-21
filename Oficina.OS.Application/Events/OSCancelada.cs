namespace Oficina.Contracts.Events;

public record OSCancelada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string Motivo,
    DateTime CanceladaEm);
