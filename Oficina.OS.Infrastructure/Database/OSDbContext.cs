using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Oficina.OS.Application.Sagas;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.ValueObjects;
using Oficina.OS.Infrastructure.Database.Converters;

namespace Oficina.OS.Infrastructure.Database;

public class OSDbContext : DbContext
{
    public OSDbContext(DbContextOptions<OSDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<OrdemDeServico> OrdensDeServico => Set<OrdemDeServico>();
    public DbSet<ItemPeca> ItensPeca => Set<ItemPeca>();
    public DbSet<ItemServico> ItensServico => Set<ItemServico>();
    public DbSet<SagaOS> SagasOS => Set<SagaOS>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<Placa>().HaveConversion<PlacaConverter>();
        configurationBuilder.Properties<CPF>().HaveConversion<CpfConverter>();
    }
}
