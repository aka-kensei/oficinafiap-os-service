using Oficina.OS.Domain.Enums;

namespace Oficina.OS.Application.DTOs;

public record ItemPecaInput(Guid PecaId, int Quantidade);

public record CriarOSInput(string CpfCliente, string PlacaVeiculo, IReadOnlyList<Guid> ServicosIds, IReadOnlyList<ItemPecaInput> Pecas);

public record ItemPecaViewModel(Guid Id, string DescricaoPeca, int Quantidade, decimal PrecoUnitarioCobrado, decimal SubTotal);

public record ItemServicoViewModel(Guid Id, string DescricaoServico, decimal PrecoCobrado);

public record OrdemDeServicoViewModel(
    Guid Id,
    StatusOS Status,
    decimal ValorOrcamento,
    DateTime DataCriacao,
    DateTime? DataInicioExecucao,
    DateTime? DataFinalizacao,
    string NomeCliente,
    string CpfCliente,
    string PlacaVeiculo,
    string ModeloVeiculo,
    IReadOnlyList<ItemServicoViewModel> ItensServico,
    IReadOnlyList<ItemPecaViewModel> ItensPeca);
