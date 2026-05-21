using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public CPF Cpf { get; private set; } = null!;
    public string Email { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;

    protected Cliente() { }

    public Cliente(string nome, CPF cpf, string email, string telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente não pode ser vazio.");

        Id = Guid.NewGuid();
        Nome = nome;
        Cpf = cpf;
        Email = email;
        Telefone = telefone;
    }

    public void AtualizarDados(string nome, string email, string telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente não pode ser vazio.");

        Nome = nome;
        Email = email;
        Telefone = telefone;
    }
}
