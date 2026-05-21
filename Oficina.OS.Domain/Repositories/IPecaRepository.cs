using Oficina.OS.Domain.Entities;

namespace Oficina.OS.Domain.Repositories;

public interface IPecaRepository
{
    Task AdicionarAsync(Peca peca);
    Task<Peca?> ObterPorIdAsync(Guid id);
    Task<IEnumerable<Peca>> ListarAsync();
    Task AtualizarAsync(Peca peca);
}
