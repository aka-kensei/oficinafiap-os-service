namespace Oficina.Contracts.Events;

public record OSAprovadaPeloCliente(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid OrcamentoId,
    DateTime AprovadaEm);
