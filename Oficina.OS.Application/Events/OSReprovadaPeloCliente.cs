namespace Oficina.Contracts.Events;

public record OSReprovadaPeloCliente(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string? Motivo,
    DateTime ReprovadaEm);
