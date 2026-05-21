namespace Oficina.OS.Application.DTOs;

public record CadastrarServicoInput(string Descricao, decimal Preco, decimal TempoEstimadoHoras);

public record ServicoViewModel(Guid Id, string Descricao, decimal Preco, decimal TempoEstimadoHoras);
