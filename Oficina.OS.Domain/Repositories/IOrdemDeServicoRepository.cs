using Oficina.OS.Domain.Entities;

namespace Oficina.OS.Domain.Repositories;

public interface IOrdemDeServicoRepository
{
    Task AdicionarAsync(OrdemDeServico os);
    Task<OrdemDeServico?> ObterPorIdAsync(Guid id);
    Task<OrdemDeServico?> ObterPorIdCompletaAsync(Guid id);
    Task<IEnumerable<OrdemDeServico>> ListarPorClienteAsync(Guid clienteId);
    Task<IEnumerable<OrdemDeServico>> ListarParaPainelAsync();
    Task AtualizarAsync(OrdemDeServico os);
}
