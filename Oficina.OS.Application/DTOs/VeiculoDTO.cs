namespace Oficina.OS.Application.DTOs;

public record CadastrarVeiculoInput(string Placa, string Marca, string Modelo, int Ano, Guid ClienteId);

public record VeiculoViewModel(Guid Id, string Placa, string Marca, string Modelo, int Ano, Guid ClienteId);
