using Oficina.OS.Application.DTOs;

namespace Oficina.OS.Application.UseCases;

public interface IClienteUseCase
{
    Task<ClienteViewModel> CriarAsync(CriarClienteInput input);
    Task<ClienteViewModel?> ObterPorIdAsync(Guid id);
    Task<ClienteViewModel?> ObterPorCpfAsync(string cpf);
    Task<ClienteViewModel?> AtualizarAsync(Guid id, AtualizarClienteInput input);
}
