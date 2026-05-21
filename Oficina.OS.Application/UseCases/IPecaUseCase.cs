using Oficina.OS.Application.DTOs;

namespace Oficina.OS.Application.UseCases;

public interface IPecaUseCase
{
    Task<PecaViewModel> CadastrarAsync(CadastrarPecaInput input);
    Task<IEnumerable<PecaViewModel>> ListarAsync();
    Task<PecaViewModel?> AtualizarAsync(Guid id, AtualizarPecaInput input);
    Task<PecaViewModel?> AjustarEstoqueAsync(Guid id, AjustarEstoqueInput input);
}
