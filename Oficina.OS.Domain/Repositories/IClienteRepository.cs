using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Domain.Repositories;

public interface IClienteRepository
{
    Task AdicionarAsync(Cliente cliente);
    Task<Cliente?> ObterPorIdAsync(Guid id);
    Task<Cliente?> ObterPorCpfAsync(CPF cpf);
    Task AtualizarAsync(Cliente cliente);
}
