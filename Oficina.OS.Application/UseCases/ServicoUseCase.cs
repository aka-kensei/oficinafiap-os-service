using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Repositories;

namespace Oficina.OS.Application.UseCases;

public class ServicoUseCase : IServicoUseCase
{
    private readonly IServicoRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ServicoUseCase(IServicoRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServicoViewModel> CadastrarAsync(CadastrarServicoInput input)
    {
        var s = new Servico(input.Descricao, input.Preco, input.TempoEstimadoHoras);
        await _repository.AdicionarAsync(s);
        await _unitOfWork.SaveChangesAsync();
        return Map(s);
    }

    public async Task<IEnumerable<ServicoViewModel>> ListarAsync()
        => (await _repository.ListarAsync()).Select(Map);

    private static ServicoViewModel Map(Servico s)
        => new(s.Id, s.Descricao, s.Preco, s.TempoEstimadoHoras);
}
