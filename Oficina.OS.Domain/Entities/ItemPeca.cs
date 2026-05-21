using Oficina.OS.Domain.Exceptions;

namespace Oficina.OS.Domain.Entities;

public class ItemPeca
{
    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public virtual OrdemDeServico? OrdemDeServico { get; private set; }
    public Guid PecaId { get; private set; }
    public string DescricaoPeca { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitarioCobrado { get; private set; }

    public decimal SubTotal => Quantidade * PrecoUnitarioCobrado;

    protected ItemPeca() { }

    public ItemPeca(Guid ordemDeServicoId, Guid pecaId, string descricaoPeca, int quantidade, decimal precoUnitarioCobrado)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser positiva.");
        if (precoUnitarioCobrado < 0)
            throw new DomainException("Preço unitário não pode ser negativo.");

        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        PecaId = pecaId;
        DescricaoPeca = descricaoPeca;
        Quantidade = quantidade;
        PrecoUnitarioCobrado = precoUnitarioCobrado;
    }
}
