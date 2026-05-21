using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.UseCases;

namespace Oficina.OS.Api.Controllers;

[ApiController]
[Route("api/veiculos")]
[Authorize]
public class VeiculosController : ControllerBase
{
    private readonly IVeiculoUseCase _useCase;

    public VeiculosController(IVeiculoUseCase useCase) => _useCase = useCase;

    [HttpPost]
    public async Task<ActionResult<VeiculoViewModel>> Cadastrar([FromBody] CadastrarVeiculoInput input)
    {
        var vm = await _useCase.CadastrarAsync(input);
        return CreatedAtAction(nameof(ObterPorPlaca), new { placa = vm.Placa }, vm);
    }

    [HttpGet("placa/{placa}")]
    public async Task<ActionResult<VeiculoViewModel>> ObterPorPlaca(string placa)
    {
        var vm = await _useCase.ObterPorPlacaAsync(placa);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<ActionResult<IEnumerable<VeiculoViewModel>>> ListarPorCliente(Guid clienteId)
    {
        var lista = await _useCase.ListarPorClienteAsync(clienteId);
        return Ok(lista);
    }
}
