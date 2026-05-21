using MassTransit;
using Oficina.Contracts.Events;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Application.UseCases;

/// <summary>
/// Use case da OS — atua APENAS sobre transições disparadas pelo cliente (criar, aprovar, reprovar, entregar).
/// Transições "operacionais" (diagnóstico, execução, finalização) são feitas pela Saga
/// em resposta a eventos publicados pelo Execução Service.
/// </summary>
public class OrdemDeServicoUseCase : IOrdemDeServicoUseCase
{
    private readonly IOrdemDeServicoRepository _osRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IVeiculoRepository _veiculoRepository;
    private readonly IPecaRepository _pecaRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrdemDeServicoUseCase(
        IOrdemDeServicoRepository osRepository,
        IClienteRepository clienteRepository,
        IVeiculoRepository veiculoRepository,
        IPecaRepository pecaRepository,
        IServicoRepository servicoRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _osRepository = osRepository;
        _clienteRepository = clienteRepository;
        _veiculoRepository = veiculoRepository;
        _pecaRepository = pecaRepository;
        _servicoRepository = servicoRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OrdemDeServicoViewModel> CriarAsync(CriarOSInput input)
    {
        var cpf = CPF.Criar(input.CpfCliente) ?? throw new DomainException("CPF inválido.");
        var cliente = await _clienteRepository.ObterPorCpfAsync(cpf)
            ?? throw new DomainException("Cliente não cadastrado.");

        var placa = Placa.Criar(input.PlacaVeiculo) ?? throw new DomainException("Placa inválida.");
        var veiculo = await _veiculoRepository.ObterPorPlacaAsync(placa)
            ?? throw new DomainException("Veículo não cadastrado.");

        if (veiculo.ClienteId != cliente.Id)
            throw new DomainException("Este veículo não pertence ao cliente informado.");

        var os = new OrdemDeServico(cliente.Id, veiculo.Id);

        foreach (var servicoId in input.ServicosIds)
        {
            var servico = await _servicoRepository.ObterPorIdAsync(servicoId)
                ?? throw new DomainException($"Serviço {servicoId} não encontrado.");
            os.AdicionarServico(servico);
        }

        foreach (var itemPeca in input.Pecas)
        {
            var peca = await _pecaRepository.ObterPorIdAsync(itemPeca.PecaId)
                ?? throw new DomainException($"Peça {itemPeca.PecaId} não encontrada.");
            peca.RemoverEstoque(itemPeca.Quantidade);
            os.AdicionarPeca(peca, itemPeca.Quantidade);
            await _pecaRepository.AtualizarAsync(peca);
        }

        await _osRepository.AdicionarAsync(os);
        await _unitOfWork.SaveChangesAsync();

        await _publishEndpoint.Publish(new OSCriada(
            CorrelationId: os.Id,
            OrdemDeServicoId: os.Id,
            ClienteId: cliente.Id,
            ClienteNome: cliente.Nome,
            ClienteCpf: cliente.Cpf.ToString(),
            ClienteEmail: cliente.Email,
            ClienteTelefone: cliente.Telefone,
            VeiculoId: veiculo.Id,
            VeiculoPlaca: veiculo.Placa.ToString(),
            VeiculoMarca: veiculo.Marca,
            VeiculoModelo: veiculo.Modelo,
            VeiculoAno: veiculo.Ano,
            DataCriacao: os.DataCriacao));

        return Map(os, cliente, veiculo);
    }

    public async Task<OrdemDeServicoViewModel?> ObterAsync(Guid id)
    {
        var os = await _osRepository.ObterPorIdCompletaAsync(id);
        return os == null ? null : Map(os);
    }

    public async Task<IEnumerable<OrdemDeServicoViewModel>> ListarPainelAsync()
        => (await _osRepository.ListarParaPainelAsync()).Select(os => Map(os));

    public async Task<OrdemDeServicoViewModel?> AprovarAsync(Guid id)
    {
        var os = await _osRepository.ObterPorIdCompletaAsync(id);
        if (os == null) return null;

        os.Aprovar();
        await _osRepository.AtualizarAsync(os);

        await _publishEndpoint.Publish(new OSAprovadaPeloCliente(
            CorrelationId: os.Id,
            OrdemDeServicoId: os.Id,
            OrcamentoId: Guid.Empty,
            AprovadaEm: DateTime.UtcNow));

        // SaveChanges persiste a atualização da OS + a mensagem no outbox EF
        // dentro da mesma transação (transactional outbox).
        await _unitOfWork.SaveChangesAsync();

        return Map(os);
    }

    public async Task<OrdemDeServicoViewModel?> ReprovarAsync(Guid id, string? motivo)
    {
        var os = await _osRepository.ObterPorIdCompletaAsync(id);
        if (os == null) return null;

        await _publishEndpoint.Publish(new OSReprovadaPeloCliente(
            CorrelationId: os.Id,
            OrdemDeServicoId: os.Id,
            Motivo: motivo,
            ReprovadaEm: DateTime.UtcNow));

        await _unitOfWork.SaveChangesAsync();

        return Map(os);
    }

    public async Task<OrdemDeServicoViewModel?> EntregarAsync(Guid id)
    {
        var os = await _osRepository.ObterPorIdCompletaAsync(id);
        if (os == null) return null;

        os.Entregar();
        await _osRepository.AtualizarAsync(os);
        await _unitOfWork.SaveChangesAsync();
        return Map(os);
    }

    private static OrdemDeServicoViewModel Map(OrdemDeServico os, Cliente? cliente = null, Veiculo? veiculo = null)
    {
        cliente ??= os.Cliente;
        veiculo ??= os.Veiculo;

        return new OrdemDeServicoViewModel(
            Id: os.Id,
            Status: os.Status,
            ValorOrcamento: os.ValorOrcamento,
            DataCriacao: os.DataCriacao,
            DataInicioExecucao: os.DataInicioExecucao,
            DataFinalizacao: os.DataFinalizacao,
            NomeCliente: cliente?.Nome ?? "N/A",
            CpfCliente: cliente?.Cpf.ToString() ?? "N/A",
            PlacaVeiculo: veiculo?.Placa.ToString() ?? "N/A",
            ModeloVeiculo: veiculo != null ? $"{veiculo.Marca} {veiculo.Modelo}" : "N/A",
            ItensServico: os.ItensServico.Select(i => new ItemServicoViewModel(i.Id, i.DescricaoServico, i.PrecoCobrado)).ToList(),
            ItensPeca: os.ItensPeca.Select(i => new ItemPecaViewModel(i.Id, i.DescricaoPeca, i.Quantidade, i.PrecoUnitarioCobrado, i.SubTotal)).ToList());
    }
}
