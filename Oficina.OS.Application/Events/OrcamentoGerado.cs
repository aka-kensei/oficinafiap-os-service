namespace Oficina.Contracts.Events;

public record OrcamentoGerado(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid OrcamentoId,
    decimal ValorTotal,
    string LinkPagamentoMercadoPago,
    string? QrCodeBase64,
    DateTime ValidoAte,
    DateTime GeradoEm);
