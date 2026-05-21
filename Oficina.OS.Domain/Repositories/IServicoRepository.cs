using Oficina.OS.Domain.Entities;

namespace Oficina.OS.Domain.Repositories;

public interface IServicoRepository
{
    Task AdicionarAsync(Servico servico);
    Task<Servico?> ObterPorIdAsync(Guid id);
    Task<IEnumerable<Servico>> ListarAsync();
    Task AtualizarAsync(Servico servico);
}
