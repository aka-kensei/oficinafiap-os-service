using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.OS.Application.Sagas;
using Oficina.OS.Domain.Entities;

namespace Oficina.OS.Infrastructure.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("Clientes");
        b.HasKey(c => c.Id);
        b.Property(c => c.Nome).IsRequired().HasMaxLength(200);
        b.Property(c => c.Cpf).IsRequired().HasMaxLength(11);
        b.HasIndex(c => c.Cpf).IsUnique();
        b.Property(c => c.Email).HasMaxLength(200);
        b.Property(c => c.Telefone).HasMaxLength(20);
    }
}

public class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> b)
    {
        b.ToTable("Veiculos");
        b.HasKey(v => v.Id);
        b.Property(v => v.Placa).IsRequired().HasMaxLength(8);
        b.HasIndex(v => v.Placa).IsUnique();
        b.Property(v => v.Marca).IsRequired().HasMaxLength(100);
        b.Property(v => v.Modelo).IsRequired().HasMaxLength(100);
        b.HasOne(v => v.Cliente).WithMany().HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(v => v.ClienteId);
    }
}

public class PecaConfiguration : IEntityTypeConfiguration<Peca>
{
    public void Configure(EntityTypeBuilder<Peca> b)
    {
        b.ToTable("Pecas");
        b.HasKey(p => p.Id);
        b.Property(p => p.Descricao).IsRequired().HasMaxLength(200);
        b.Property(p => p.PrecoVenda).HasColumnType("decimal(18,2)");
    }
}

public class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> b)
    {
        b.ToTable("Servicos");
        b.HasKey(s => s.Id);
        b.Property(s => s.Descricao).IsRequired().HasMaxLength(200);
        b.Property(s => s.Preco).HasColumnType("decimal(18,2)");
        b.Property(s => s.TempoEstimadoHoras).HasColumnType("decimal(8,2)");
    }
}

public class OrdemDeServicoConfiguration : IEntityTypeConfiguration<OrdemDeServico>
{
    public void Configure(EntityTypeBuilder<OrdemDeServico> b)
    {
        b.ToTable("OrdensDeServico");
        b.HasKey(o => o.Id);
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(o => o.ValorOrcamento).HasColumnType("decimal(18,2)");
        b.HasOne(o => o.Cliente).WithMany().HasForeignKey(o => o.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o => o.Veiculo).WithMany().HasForeignKey(o => o.VeiculoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(o => o.ItensPeca).WithOne(i => i.OrdemDeServico!).HasForeignKey(i => i.OrdemDeServicoId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(o => o.ItensServico).WithOne(i => i.OrdemDeServico!).HasForeignKey(i => i.OrdemDeServicoId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(o => o.ClienteId);
        b.HasIndex(o => o.VeiculoId);
        b.HasIndex(o => o.Status);
    }
}

public class ItemPecaConfiguration : IEntityTypeConfiguration<ItemPeca>
{
    public void Configure(EntityTypeBuilder<ItemPeca> b)
    {
        b.ToTable("ItensPeca");
        b.HasKey(i => i.Id);
        b.Property(i => i.DescricaoPeca).IsRequired().HasMaxLength(200);
        b.Property(i => i.PrecoUnitarioCobrado).HasColumnType("decimal(18,2)");
    }
}

public class ItemServicoConfiguration : IEntityTypeConfiguration<ItemServico>
{
    public void Configure(EntityTypeBuilder<ItemServico> b)
    {
        b.ToTable("ItensServico");
        b.HasKey(i => i.Id);
        b.Property(i => i.DescricaoServico).IsRequired().HasMaxLength(200);
        b.Property(i => i.PrecoCobrado).HasColumnType("decimal(18,2)");
    }
}

public class SagaOSConfiguration : IEntityTypeConfiguration<SagaOS>
{
    public void Configure(EntityTypeBuilder<SagaOS> b)
    {
        b.ToTable("SagasOS");
        b.HasKey(s => s.CorrelationId);
        b.Property(s => s.CurrentState).IsRequired().HasMaxLength(40);
        b.Property(s => s.UltimoEventoConsumido).HasMaxLength(80);
        b.Property(s => s.MotivoFalha).HasMaxLength(500);
        b.Property(s => s.ValorOrcamento).HasColumnType("decimal(18,2)");
        b.Property(s => s.Version).IsConcurrencyToken();
        b.HasIndex(s => s.OrdemDeServicoId).IsUnique();
        b.HasIndex(s => s.CurrentState);
    }
}
