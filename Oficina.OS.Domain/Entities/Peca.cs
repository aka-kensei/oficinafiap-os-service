using Oficina.OS.Domain.Exceptions;

namespace Oficina.OS.Domain.Entities;

public class Peca
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public decimal PrecoVenda { get; private set; }
    public int QuantidadeEstoque { get; private set; }

    protected Peca() { }

    public Peca(string descricao, decimal precoVenda, int estoqueInicial)
    {
        ValidarDados(descricao, precoVenda, estoqueInicial);

        Id = Guid.NewGuid();
        Descricao = descricao;
        PrecoVenda = precoVenda;
        QuantidadeEstoque = estoqueInicial;
    }

    public void AtualizarDados(string descricao, decimal precoVenda)
    {
        ValidarDados(descricao, precoVenda, 0);
        Descricao = descricao;
        PrecoVenda = precoVenda;
    }

    public void AdicionarEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade a adicionar deve ser positiva.");
        QuantidadeEstoque += quantidade;
    }

    public void RemoverEstoque(int quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("A quantidade a remover deve ser positiva.");
        if (QuantidadeEstoque < quantidade)
            throw new DomainException($"Estoque insuficiente para a peça {Descricao}.");

        QuantidadeEstoque -= quantidade;
    }

    private static void ValidarDados(string descricao, decimal precoVenda, int estoque)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("A descrição da peça não pode ser vazia.");
        if (precoVenda < 0)
            throw new DomainException("O preço de venda não pode ser negativo.");
        if (estoque < 0)
            throw new DomainException("O estoque inicial não pode ser negativo.");
    }
}
