using Oficina.OS.Domain.Exceptions;

namespace Oficina.OS.Domain.Entities;

public class Servico
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public decimal TempoEstimadoHoras { get; private set; }

    protected Servico() { }

    public Servico(string descricao, decimal preco, decimal tempoEstimadoHoras)
    {
        ValidarDados(descricao, preco, tempoEstimadoHoras);

        Id = Guid.NewGuid();
        Descricao = descricao;
        Preco = preco;
        TempoEstimadoHoras = tempoEstimadoHoras;
    }

    public void Atualizar(string descricao, decimal preco, decimal tempoEstimadoHoras)
    {
        ValidarDados(descricao, preco, tempoEstimadoHoras);
        Descricao = descricao;
        Preco = preco;
        TempoEstimadoHoras = tempoEstimadoHoras;
    }

    private static void ValidarDados(string descricao, decimal preco, decimal tempoEstimadoHoras)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("A descrição do serviço não pode ser vazia.");
        if (preco < 0)
            throw new DomainException("O preço do serviço não pode ser negativo.");
        if (tempoEstimadoHoras <= 0)
            throw new DomainException("O tempo estimado deve ser positivo.");
    }
}
