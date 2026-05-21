using FluentAssertions;
using Oficina.OS.Domain.ValueObjects;
using Xunit;

namespace Oficina.OS.Domain.UnitTests.ValueObjects;

public class CPFTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("529 982 247 25")]
    public void Criar_ComCPFValido_RetornaCPF(string entrada)
    {
        var cpf = CPF.Criar(entrada);

        cpf.Should().NotBeNull();
        cpf!.Numero.Should().Be("52998224725");
    }

    [Theory]
    [InlineData("111.111.111-11")] // todos iguais
    [InlineData("123.456.789-00")] // dígito verificador errado
    [InlineData("123")]             // tamanho errado
    [InlineData("abc.def.ghi-jk")]  // não numérico
    [InlineData("")]
    [InlineData(" ")]
    public void Criar_ComCPFInvalido_RetornaNull(string entrada)
    {
        CPF.Criar(entrada).Should().BeNull();
    }

    [Fact]
    public void ToString_RetornaNumeroLimpo()
    {
        var cpf = CPF.Criar("529.982.247-25")!;
        cpf.ToString().Should().Be("52998224725");
    }

    [Fact]
    public void Validar_NumeroLimpoComDigitosVerificadoresCorretos_RetornaTrue()
    {
        CPF.Validar("52998224725").Should().BeTrue();
    }

    [Fact]
    public void Validar_NumeroLimpoComDigitosVerificadoresIncorretos_RetornaFalse()
    {
        CPF.Validar("52998224726").Should().BeFalse();
    }
}
