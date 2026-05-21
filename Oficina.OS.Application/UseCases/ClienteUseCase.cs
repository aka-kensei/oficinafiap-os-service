using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Application.UseCases;

public class ClienteUseCase : IClienteUseCase
{
    private readonly IClienteRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ClienteUseCase(IClienteRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClienteViewModel> CriarAsync(CriarClienteInput input)
    {
        var cpf = CPF.Criar(input.Cpf) ?? throw new DomainException("CPF inválido.");

        if (await _repository.ObterPorCpfAsync(cpf) != null)
            throw new DomainException("Já existe um cliente com este CPF.");

        var cliente = new Cliente(input.Nome, cpf, input.Email, input.Telefone);
        await _repository.AdicionarAsync(cliente);
        await _unitOfWork.SaveChangesAsync();

        return Map(cliente);
    }

    public async Task<ClienteViewModel?> ObterPorIdAsync(Guid id)
    {
        var c = await _repository.ObterPorIdAsync(id);
        return c == null ? null : Map(c);
    }

    public async Task<ClienteViewModel?> ObterPorCpfAsync(string cpf)
    {
        var cpfVo = CPF.Criar(cpf) ?? throw new DomainException("CPF inválido.");
        var c = await _repository.ObterPorCpfAsync(cpfVo);
        return c == null ? null : Map(c);
    }

    public async Task<ClienteViewModel?> AtualizarAsync(Guid id, AtualizarClienteInput input)
    {
        var c = await _repository.ObterPorIdAsync(id);
        if (c == null) return null;

        c.AtualizarDados(input.Nome, input.Email, input.Telefone);
        await _repository.AtualizarAsync(c);
        await _unitOfWork.SaveChangesAsync();

        return Map(c);
    }

    private static ClienteViewModel Map(Cliente c)
        => new(c.Id, c.Nome, c.Cpf.ToString(), c.Email, c.Telefone);
}
