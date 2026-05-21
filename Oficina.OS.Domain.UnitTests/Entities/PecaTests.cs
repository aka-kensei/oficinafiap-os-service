using FluentAssertions;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Exceptions;
using Xunit;

namespace Oficina.OS.Domain.UnitTests.Entities;

public class PecaTests
{
    [Fact]
    public void Construtor_ComDadosValidos_CriaPeca()
    {
        var peca = new Peca("Filtro de óleo", 49.90m, 10);

        peca.Id.Should().NotBeEmpty();
        peca.Descricao.Should().Be("Filtro de óleo");
        peca.PrecoVenda.Should().Be(49.90m);
        peca.QuantidadeEstoque.Should().Be(10);
    }

    [Theory]
    [InlineData("", 10, 5)]
    [InlineData(" ", 10, 5)]
    public void Construtor_ComDescricaoVazia_LancaDomainException(string descricao, decimal preco, int estoque)
    {
        var act = () => new Peca(descricao, preco, estoque);
        act.Should().Throw<DomainException>().WithMessage("*descrição*");
    }

    [Fact]
    public void Construtor_ComPrecoNegativo_LancaDomainException()
    {
        var act = () => new Peca("X", -1m, 5);
        act.Should().Throw<DomainException>().WithMessage("*preço*");
    }

    [Fact]
    public void Construtor_ComEstoqueNegativo_LancaDomainException()
    {
        var act = () => new Peca("X", 1m, -1);
        act.Should().Throw<DomainException>().WithMessage("*estoque*");
    }

    [Fact]
    public void AdicionarEstoque_ComQuantidadePositiva_IncrementaEstoque()
    {
        var peca = new Peca("Filtro", 10m, 5);
        peca.AdicionarEstoque(3);
        peca.QuantidadeEstoque.Should().Be(8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AdicionarEstoque_ComQuantidadeNaoPositiva_LancaDomainException(int qtd)
    {
        var peca = new Peca("X", 1m, 5);
        var act = () => peca.AdicionarEstoque(qtd);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoverEstoque_ComQuantidadeSuficiente_DecrementaEstoque()
    {
        var peca = new Peca("X", 1m, 5);
        peca.RemoverEstoque(3);
        peca.QuantidadeEstoque.Should().Be(2);
    }

    [Fact]
    public void RemoverEstoque_ComQuantidadeInsuficiente_LancaDomainException()
    {
        var peca = new Peca("X", 1m, 3);
        var act = () => peca.RemoverEstoque(5);
        act.Should().Throw<DomainException>().WithMessage("*insuficiente*");
    }

    [Fact]
    public void AtualizarDados_ComDadosValidos_AtualizaDescricaoEPreco()
    {
        var peca = new Peca("X", 1m, 5);
        peca.AtualizarDados("Y", 2m);

        peca.Descricao.Should().Be("Y");
        peca.PrecoVenda.Should().Be(2m);
        peca.QuantidadeEstoque.Should().Be(5); // estoque preservado
    }
}
