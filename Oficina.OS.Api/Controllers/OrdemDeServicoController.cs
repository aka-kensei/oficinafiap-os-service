using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.UseCases;

namespace Oficina.OS.Api.Controllers;

[ApiController]
[Route("api/ordemdeservico")]
[Authorize]
public class OrdemDeServicoController : ControllerBase
{
    private readonly IOrdemDeServicoUseCase _useCase;

    public OrdemDeServicoController(IOrdemDeServicoUseCase useCase) => _useCase = useCase;

    [HttpPost]
    public async Task<ActionResult<OrdemDeServicoViewModel>> Criar([FromBody] CriarOSInput input)
    {
        var vm = await _useCase.CriarAsync(input);
        return CreatedAtAction(nameof(Obter), new { id = vm.Id }, vm);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<OrdemDeServicoViewModel>> Obter(Guid id)
    {
        var vm = await _useCase.ObterAsync(id);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpGet("painel")]
    public async Task<ActionResult<IEnumerable<OrdemDeServicoViewModel>>> Painel()
        => Ok(await _useCase.ListarPainelAsync());

    [HttpPatch("{id:guid}/aprovar")]
    public async Task<ActionResult<OrdemDeServicoViewModel>> Aprovar(Guid id)
    {
        var vm = await _useCase.AprovarAsync(id);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpPatch("{id:guid}/reprovar")]
    public async Task<ActionResult<OrdemDeServicoViewModel>> Reprovar(Guid id, [FromBody] ReprovarInput? input = null)
    {
        var vm = await _useCase.ReprovarAsync(id, input?.Motivo);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpPatch("{id:guid}/entregar")]
    public async Task<ActionResult<OrdemDeServicoViewModel>> Entregar(Guid id)
    {
        var vm = await _useCase.EntregarAsync(id);
        return vm == null ? NotFound() : Ok(vm);
    }
}

public record ReprovarInput(string? Motivo);
