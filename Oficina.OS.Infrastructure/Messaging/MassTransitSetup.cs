using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oficina.OS.Application.Sagas;
using Oficina.OS.Infrastructure.Database;

namespace Oficina.OS.Infrastructure.Messaging;

public static class MassTransitSetup
{
    /// <summary>
    /// Configura MassTransit com RabbitMQ + Saga persistido via EF Core + Outbox pattern.
    /// O OS Service é o orquestrador: hospeda a Saga state machine e o scheduler de timeouts.
    /// </summary>
    public static IServiceCollection AddOSMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddSagaStateMachine<OSSagaStateMachine, SagaOS>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<OSDbContext>();
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;

                    r.UseSqlServer();
                });

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "rabbitmq.messaging-ns.svc.cluster.local", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "oficina");
                    h.Password(configuration["RabbitMq:Password"] ?? string.Empty);
                });

                // O Schedule da Saga (PrazoPagamentoTimeout de 24h) foi temporariamente
                // desabilitado porque requer plugin rabbitmq_delayed_message_exchange.
                // Em prod, instalar o plugin + reabilitar UseDelayedMessageScheduler.
                cfg.UseInMemoryOutbox(ctx);

                // Retry absorve race conditions em que eventos do Execucao chegam fora
                // de ordem (ex.: OrcamentoPropostoPelaOficina antes do DiagnosticoIniciado
                // ter sido aplicado) — sem isso o evento vai pra DLQ e a saga trava.
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
