using FluentAssertions;
using Oficina.OS.Domain.ValueObjects;
using Xunit;

namespace Oficina.OS.Domain.UnitTests.ValueObjects;

public class PlacaTests
{
    [Theory]
    [InlineData("ABC-1234", "ABC1234")]   // formato antigo com traço
    [InlineData("ABC1234", "ABC1234")]    // formato antigo sem traço
    [InlineData("abc1234", "ABC1234")]    // minúscula → normaliza
    [InlineData("ABC1D23", "ABC1D23")]    // Mercosul
    [InlineData("abc1d23", "ABC1D23")]    // Mercosul minúscula
    public void Criar_ComPlacaValida_NormalizaERetornaPlaca(string entrada, string esperado)
    {
        var placa = Placa.Criar(entrada);

        placa.Should().NotBeNull();
        placa!.Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("AB-1234")]    // só 2 letras
    [InlineData("ABCD-1234")]  // 4 letras
    [InlineData("ABC-12345")]  // 5 dígitos
    [InlineData("ABC1234X")]   // muito longa
    [InlineData("")]
    [InlineData("XYZ")]
    public void Criar_ComPlacaInvalida_RetornaNull(string entrada)
    {
        Placa.Criar(entrada).Should().BeNull();
    }

    [Fact]
    public void ToString_RetornaValorNormalizado()
    {
        var placa = Placa.Criar("abc-1234")!;
        placa.ToString().Should().Be("ABC1234");
    }
}
