namespace Oficina.Contracts.Events;

public record ExecucaoFinalizada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    decimal? TempoTotalHoras,
    DateTime FinalizadaEm);
