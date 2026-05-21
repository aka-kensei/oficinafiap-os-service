using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Repositories;

namespace Oficina.OS.Application.UseCases;

public class PecaUseCase : IPecaUseCase
{
    private readonly IPecaRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PecaUseCase(IPecaRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PecaViewModel> CadastrarAsync(CadastrarPecaInput input)
    {
        var peca = new Peca(input.Descricao, input.PrecoVenda, input.EstoqueInicial);
        await _repository.AdicionarAsync(peca);
        await _unitOfWork.SaveChangesAsync();
        return Map(peca);
    }

    public async Task<IEnumerable<PecaViewModel>> ListarAsync()
        => (await _repository.ListarAsync()).Select(Map);

    public async Task<PecaViewModel?> AtualizarAsync(Guid id, AtualizarPecaInput input)
    {
        var peca = await _repository.ObterPorIdAsync(id);
        if (peca == null) return null;

        peca.AtualizarDados(input.Descricao, input.PrecoVenda);
        await _repository.AtualizarAsync(peca);
        await _unitOfWork.SaveChangesAsync();
        return Map(peca);
    }

    public async Task<PecaViewModel?> AjustarEstoqueAsync(Guid id, AjustarEstoqueInput input)
    {
        var peca = await _repository.ObterPorIdAsync(id);
        if (peca == null) return null;

        if (input.Quantidade > 0) peca.AdicionarEstoque(input.Quantidade);
        else peca.RemoverEstoque(-input.Quantidade);

        await _repository.AtualizarAsync(peca);
        await _unitOfWork.SaveChangesAsync();
        return Map(peca);
    }

    private static PecaViewModel Map(Peca p)
        => new(p.Id, p.Descricao, p.PrecoVenda, p.QuantidadeEstoque);
}
