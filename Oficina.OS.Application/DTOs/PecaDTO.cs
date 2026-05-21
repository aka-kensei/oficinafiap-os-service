namespace Oficina.OS.Application.DTOs;

public record CadastrarPecaInput(string Descricao, decimal PrecoVenda, int EstoqueInicial);

public record AtualizarPecaInput(string Descricao, decimal PrecoVenda);

public record AjustarEstoqueInput(int Quantidade);

public record PecaViewModel(Guid Id, string Descricao, decimal PrecoVenda, int QuantidadeEstoque);
