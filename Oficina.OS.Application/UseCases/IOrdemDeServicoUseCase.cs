using Oficina.OS.Application.DTOs;

namespace Oficina.OS.Application.UseCases;

public interface IOrdemDeServicoUseCase
{
    Task<OrdemDeServicoViewModel> CriarAsync(CriarOSInput input);
    Task<OrdemDeServicoViewModel?> ObterAsync(Guid id);
    Task<IEnumerable<OrdemDeServicoViewModel>> ListarPainelAsync();
    Task<OrdemDeServicoViewModel?> AprovarAsync(Guid id);
    Task<OrdemDeServicoViewModel?> ReprovarAsync(Guid id, string? motivo);
    Task<OrdemDeServicoViewModel?> EntregarAsync(Guid id);
}
