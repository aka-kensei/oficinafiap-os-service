namespace Oficina.Contracts.Events;

public record ExecucaoIniciada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string? MecanicoResponsavel,
    DateTime IniciadaEm);
