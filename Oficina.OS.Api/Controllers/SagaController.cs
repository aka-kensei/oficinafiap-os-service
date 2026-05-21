using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oficina.OS.Application.Sagas;
using Oficina.OS.Infrastructure.Database;

namespace Oficina.OS.Api.Controllers;

/// <summary>
/// Endpoints de observabilidade do estado das Sagas — úteis para debug e demo do Saga Pattern.
/// </summary>
[ApiController]
[Route("api/sagas")]
[Authorize]
public class SagaController : ControllerBase
{
    private readonly OSDbContext _ctx;

    public SagaController(OSDbContext ctx) => _ctx = ctx;

    [HttpGet("{osId:guid}")]
    public async Task<ActionResult<SagaOS>> ObterPorOS(Guid osId)
    {
        var saga = await _ctx.SagasOS.FirstOrDefaultAsync(s => s.OrdemDeServicoId == osId);
        return saga == null ? NotFound() : Ok(saga);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SagaOS>>> Listar([FromQuery] string? state)
    {
        var query = _ctx.SagasOS.AsQueryable();
        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(s => s.CurrentState == state);
        return Ok(await query.OrderByDescending(s => s.AtualizadaEm).Take(100).ToListAsync());
    }
}
