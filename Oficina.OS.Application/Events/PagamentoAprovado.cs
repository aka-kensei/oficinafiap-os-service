namespace Oficina.Contracts.Events;

public record PagamentoAprovado(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid OrcamentoId,
    Guid PagamentoId,
    string MercadoPagoPaymentId,
    decimal ValorPago,
    DateTime AprovadoEm);
