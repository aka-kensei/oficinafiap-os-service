using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.UseCases;

namespace Oficina.OS.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteUseCase _useCase;

    public ClientesController(IClienteUseCase useCase) => _useCase = useCase;

    [HttpPost]
    public async Task<ActionResult<ClienteViewModel>> Criar([FromBody] CriarClienteInput input)
    {
        var vm = await _useCase.CriarAsync(input);
        return CreatedAtAction(nameof(ObterPorId), new { id = vm.Id }, vm);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteViewModel>> ObterPorId(Guid id)
    {
        var vm = await _useCase.ObterPorIdAsync(id);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpGet("cpf/{cpf}")]
    public async Task<ActionResult<ClienteViewModel>> ObterPorCpf(string cpf)
    {
        var vm = await _useCase.ObterPorCpfAsync(cpf);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteViewModel>> Atualizar(Guid id, [FromBody] AtualizarClienteInput input)
    {
        var vm = await _useCase.AtualizarAsync(id, input);
        return vm == null ? NotFound() : Ok(vm);
    }
}
