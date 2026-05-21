namespace Oficina.OS.Domain.Entities;

public class ItemServico
{
    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public virtual OrdemDeServico? OrdemDeServico { get; private set; }
    public Guid ServicoId { get; private set; }
    public string DescricaoServico { get; private set; } = string.Empty;
    public decimal PrecoCobrado { get; private set; }

    protected ItemServico() { }

    public ItemServico(Guid ordemDeServicoId, Guid servicoId, string descricaoServico, decimal precoCobrado)
    {
        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        ServicoId = servicoId;
        DescricaoServico = descricaoServico;
        PrecoCobrado = precoCobrado;
    }
}
