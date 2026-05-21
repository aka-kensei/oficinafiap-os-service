namespace Oficina.OS.Application.DTOs;

public record CriarClienteInput(string Nome, string Cpf, string Email, string Telefone);

public record AtualizarClienteInput(string Nome, string Email, string Telefone);

public record ClienteViewModel(Guid Id, string Nome, string Cpf, string Email, string Telefone);
