using Oficina.OS.Application.DTOs;

namespace Oficina.OS.Application.UseCases;

public interface IServicoUseCase
{
    Task<ServicoViewModel> CadastrarAsync(CadastrarServicoInput input);
    Task<IEnumerable<ServicoViewModel>> ListarAsync();
}
