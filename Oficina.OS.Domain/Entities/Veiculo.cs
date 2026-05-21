using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Domain.Entities;

public class Veiculo
{
    public Guid Id { get; private set; }
    public Placa Placa { get; private set; } = null!;
    public string Marca { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public int Ano { get; private set; }
    public Guid ClienteId { get; private set; }
    public virtual Cliente? Cliente { get; private set; }

    protected Veiculo() { }

    public Veiculo(Placa placa, string marca, string modelo, int ano, Guid clienteId)
    {
        if (string.IsNullOrWhiteSpace(marca))
            throw new DomainException("Marca não pode ser vazia.");
        if (string.IsNullOrWhiteSpace(modelo))
            throw new DomainException("Modelo não pode ser vazio.");
        if (ano < 1900 || ano > DateTime.UtcNow.Year + 1)
            throw new DomainException("Ano do veículo inválido.");

        Id = Guid.NewGuid();
        Placa = placa;
        Marca = marca;
        Modelo = modelo;
        Ano = ano;
        ClienteId = clienteId;
    }

    public void AtualizarDados(string marca, string modelo, int ano)
    {
        Marca = marca;
        Modelo = modelo;
        Ano = ano;
    }
}
