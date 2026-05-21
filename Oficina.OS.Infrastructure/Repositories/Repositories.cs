using Microsoft.EntityFrameworkCore;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Domain.ValueObjects;
using Oficina.OS.Infrastructure.Database;

namespace Oficina.OS.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly OSDbContext _ctx;
    public ClienteRepository(OSDbContext ctx) => _ctx = ctx;

    public async Task AdicionarAsync(Cliente cliente) => await _ctx.Clientes.AddAsync(cliente);
    public Task<Cliente?> ObterPorIdAsync(Guid id) => _ctx.Clientes.FirstOrDefaultAsync(c => c.Id == id);
    public Task<Cliente?> ObterPorCpfAsync(CPF cpf) => _ctx.Clientes.FirstOrDefaultAsync(c => c.Cpf == cpf);
    public Task AtualizarAsync(Cliente cliente)
    {
        _ctx.Clientes.Update(cliente);
        return Task.CompletedTask;
    }
}

public class VeiculoRepository : IVeiculoRepository
{
    private readonly OSDbContext _ctx;
    public VeiculoRepository(OSDbContext ctx) => _ctx = ctx;

    public async Task AdicionarAsync(Veiculo veiculo) => await _ctx.Veiculos.AddAsync(veiculo);
    public Task<Veiculo?> ObterPorIdAsync(Guid id) => _ctx.Veiculos.FirstOrDefaultAsync(v => v.Id == id);
    public Task<Veiculo?> ObterPorPlacaAsync(Placa placa) => _ctx.Veiculos.FirstOrDefaultAsync(v => v.Placa == placa);

    public async Task<IEnumerable<Veiculo>> ListarPorClienteAsync(Guid clienteId)
        => await _ctx.Veiculos.Where(v => v.ClienteId == clienteId).ToListAsync();

    public Task AtualizarAsync(Veiculo veiculo)
    {
        _ctx.Veiculos.Update(veiculo);
        return Task.CompletedTask;
    }
}

public class PecaRepository : IPecaRepository
{
    private readonly OSDbContext _ctx;
    public PecaRepository(OSDbContext ctx) => _ctx = ctx;

    public async Task AdicionarAsync(Peca peca) => await _ctx.Pecas.AddAsync(peca);
    public Task<Peca?> ObterPorIdAsync(Guid id) => _ctx.Pecas.FirstOrDefaultAsync(p => p.Id == id);
    public async Task<IEnumerable<Peca>> ListarAsync() => await _ctx.Pecas.ToListAsync();

    public Task AtualizarAsync(Peca peca)
    {
        _ctx.Pecas.Update(peca);
        return Task.CompletedTask;
    }
}

public class ServicoRepository : IServicoRepository
{
    private readonly OSDbContext _ctx;
    public ServicoRepository(OSDbContext ctx) => _ctx = ctx;

    public async Task AdicionarAsync(Servico servico) => await _ctx.Servicos.AddAsync(servico);
    public Task<Servico?> ObterPorIdAsync(Guid id) => _ctx.Servicos.FirstOrDefaultAsync(s => s.Id == id);
    public async Task<IEnumerable<Servico>> ListarAsync() => await _ctx.Servicos.ToListAsync();

    public Task AtualizarAsync(Servico servico)
    {
        _ctx.Servicos.Update(servico);
        return Task.CompletedTask;
    }
}

public class OrdemDeServicoRepository : IOrdemDeServicoRepository
{
    private readonly OSDbContext _ctx;
    public OrdemDeServicoRepository(OSDbContext ctx) => _ctx = ctx;

    public async Task AdicionarAsync(OrdemDeServico os) => await _ctx.OrdensDeServico.AddAsync(os);
    public Task<OrdemDeServico?> ObterPorIdAsync(Guid id) => _ctx.OrdensDeServico.FirstOrDefaultAsync(o => o.Id == id);

    public Task<OrdemDeServico?> ObterPorIdCompletaAsync(Guid id) =>
        _ctx.OrdensDeServico
            .Include(o => o.Cliente)
            .Include(o => o.Veiculo)
            .Include(o => o.ItensPeca)
            .Include(o => o.ItensServico)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<OrdemDeServico>> ListarPorClienteAsync(Guid clienteId) =>
        await _ctx.OrdensDeServico
            .Include(o => o.Veiculo)
            .Include(o => o.ItensPeca)
            .Include(o => o.ItensServico)
            .Where(o => o.ClienteId == clienteId)
            .ToListAsync();

    public async Task<IEnumerable<OrdemDeServico>> ListarParaPainelAsync() =>
        await _ctx.OrdensDeServico
            .Include(o => o.Cliente)
            .Include(o => o.Veiculo)
            .Include(o => o.ItensPeca)
            .Include(o => o.ItensServico)
            .Where(o => o.Status != Domain.Enums.StatusOS.Entregue && o.Status != Domain.Enums.StatusOS.Cancelada)
            .OrderBy(o => o.Status)
            .ThenBy(o => o.DataCriacao)
            .ToListAsync();

    public Task AtualizarAsync(OrdemDeServico os)
    {
        _ctx.OrdensDeServico.Update(os);
        return Task.CompletedTask;
    }
}
