using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Domain.Repositories;

public interface IVeiculoRepository
{
    Task AdicionarAsync(Veiculo veiculo);
    Task<Veiculo?> ObterPorIdAsync(Guid id);
    Task<Veiculo?> ObterPorPlacaAsync(Placa placa);
    Task<IEnumerable<Veiculo>> ListarPorClienteAsync(Guid clienteId);
    Task AtualizarAsync(Veiculo veiculo);
}
