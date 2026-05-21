namespace Oficina.Contracts.Events;

public record DiagnosticoIniciado(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    string? MecanicoResponsavel,
    DateTime IniciadoEm);
