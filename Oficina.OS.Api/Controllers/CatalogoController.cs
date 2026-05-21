using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.UseCases;

namespace Oficina.OS.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
[Authorize]
public class CatalogoController : ControllerBase
{
    private readonly IPecaUseCase _pecaUseCase;
    private readonly IServicoUseCase _servicoUseCase;

    public CatalogoController(IPecaUseCase pecaUseCase, IServicoUseCase servicoUseCase)
    {
        _pecaUseCase = pecaUseCase;
        _servicoUseCase = servicoUseCase;
    }

    [HttpPost("pecas")]
    public async Task<ActionResult<PecaViewModel>> CadastrarPeca([FromBody] CadastrarPecaInput input)
        => Ok(await _pecaUseCase.CadastrarAsync(input));

    [HttpGet("pecas")]
    public async Task<ActionResult<IEnumerable<PecaViewModel>>> ListarPecas()
        => Ok(await _pecaUseCase.ListarAsync());

    [HttpPut("pecas/{id:guid}")]
    public async Task<ActionResult<PecaViewModel>> AtualizarPeca(Guid id, [FromBody] AtualizarPecaInput input)
    {
        var vm = await _pecaUseCase.AtualizarAsync(id, input);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpPatch("pecas/{id:guid}/estoque")]
    public async Task<ActionResult<PecaViewModel>> AjustarEstoque(Guid id, [FromBody] AjustarEstoqueInput input)
    {
        var vm = await _pecaUseCase.AjustarEstoqueAsync(id, input);
        return vm == null ? NotFound() : Ok(vm);
    }

    [HttpPost("servicos")]
    public async Task<ActionResult<ServicoViewModel>> CadastrarServico([FromBody] CadastrarServicoInput input)
        => Ok(await _servicoUseCase.CadastrarAsync(input));

    [HttpGet("servicos")]
    public async Task<ActionResult<IEnumerable<ServicoViewModel>>> ListarServicos()
        => Ok(await _servicoUseCase.ListarAsync());
}
