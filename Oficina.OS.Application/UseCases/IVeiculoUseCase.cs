using Oficina.OS.Application.DTOs;

namespace Oficina.OS.Application.UseCases;

public interface IVeiculoUseCase
{
    Task<VeiculoViewModel> CadastrarAsync(CadastrarVeiculoInput input);
    Task<VeiculoViewModel?> ObterPorPlacaAsync(string placa);
    Task<IEnumerable<VeiculoViewModel>> ListarPorClienteAsync(Guid clienteId);
}
