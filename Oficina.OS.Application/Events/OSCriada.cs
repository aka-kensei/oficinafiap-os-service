namespace Oficina.Contracts.Events;

/// <summary>
/// Evento publicado pelo OS Service quando uma nova OS é aberta.
/// Carrega snapshot denormalizado do cliente e veículo para que Billing e Execução
/// não precisem consultar o OS Service em tempo síncrono.
///
/// Namespace `Oficina.Contracts.Events` é compartilhado entre os 3 microsserviços
/// para que MassTransit roteie a mesma mensagem pelo mesmo exchange RabbitMQ.
/// </summary>
public record OSCriada(
    Guid CorrelationId,
    Guid OrdemDeServicoId,
    Guid ClienteId,
    string ClienteNome,
    string ClienteCpf,
    string ClienteEmail,
    string ClienteTelefone,
    Guid VeiculoId,
    string VeiculoPlaca,
    string VeiculoMarca,
    string VeiculoModelo,
    int VeiculoAno,
    DateTime DataCriacao);
