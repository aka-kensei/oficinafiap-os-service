using FluentAssertions;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.ValueObjects;
using Xunit;

namespace Oficina.OS.Domain.UnitTests.Entities;

public class ClienteTests
{
    private static CPF CpfValido() => CPF.Criar("52998224725")!;

    [Fact]
    public void Construtor_ComDadosValidos_CriaCliente()
    {
        var cpf = CpfValido();
        var c = new Cliente("João Silva", cpf, "joao@email.com", "11999999999");

        c.Id.Should().NotBeEmpty();
        c.Nome.Should().Be("João Silva");
        c.Cpf.Should().Be(cpf);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Construtor_ComNomeVazio_LancaDomainException(string nome)
    {
        var act = () => new Cliente(nome, CpfValido(), "x@y.com", "11");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AtualizarDados_ComDadosValidos_AtualizaCampos()
    {
        var c = new Cliente("João", CpfValido(), "j@e.com", "11");
        c.AtualizarDados("José", "jose@email.com", "22");

        c.Nome.Should().Be("José");
        c.Email.Should().Be("jose@email.com");
        c.Telefone.Should().Be("22");
    }
}

public class VeiculoTests
{
    private static Placa PlacaValida() => Placa.Criar("ABC1234")!;

    [Fact]
    public void Construtor_ComDadosValidos_CriaVeiculo()
    {
        var v = new Veiculo(PlacaValida(), "Toyota", "Corolla", 2020, Guid.NewGuid());
        v.Id.Should().NotBeEmpty();
        v.Marca.Should().Be("Toyota");
        v.Ano.Should().Be(2020);
    }

    [Fact]
    public void Construtor_ComMarcaVazia_LancaDomainException()
    {
        var act = () => new Veiculo(PlacaValida(), "", "X", 2020, Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(1800)]
    [InlineData(3000)]
    public void Construtor_ComAnoInvalido_LancaDomainException(int ano)
    {
        var act = () => new Veiculo(PlacaValida(), "X", "Y", ano, Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }
}
