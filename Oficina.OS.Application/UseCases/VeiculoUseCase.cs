using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Application.UseCases;

public class VeiculoUseCase : IVeiculoUseCase
{
    private readonly IVeiculoRepository _repository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VeiculoUseCase(IVeiculoRepository repository, IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<VeiculoViewModel> CadastrarAsync(CadastrarVeiculoInput input)
    {
        var placa = Placa.Criar(input.Placa) ?? throw new DomainException("Placa inválida.");

        if (await _repository.ObterPorPlacaAsync(placa) != null)
            throw new DomainException("Já existe um veículo com esta placa.");

        if (await _clienteRepository.ObterPorIdAsync(input.ClienteId) == null)
            throw new DomainException("Cliente não encontrado.");

        var veiculo = new Veiculo(placa, input.Marca, input.Modelo, input.Ano, input.ClienteId);
        await _repository.AdicionarAsync(veiculo);
        await _unitOfWork.SaveChangesAsync();

        return Map(veiculo);
    }

    public async Task<VeiculoViewModel?> ObterPorPlacaAsync(string placa)
    {
        var placaVo = Placa.Criar(placa) ?? throw new DomainException("Placa inválida.");
        var v = await _repository.ObterPorPlacaAsync(placaVo);
        return v == null ? null : Map(v);
    }

    public async Task<IEnumerable<VeiculoViewModel>> ListarPorClienteAsync(Guid clienteId)
    {
        var lista = await _repository.ListarPorClienteAsync(clienteId);
        return lista.Select(Map);
    }

    private static VeiculoViewModel Map(Veiculo v)
        => new(v.Id, v.Placa.ToString(), v.Marca, v.Modelo, v.Ano, v.ClienteId);
}
