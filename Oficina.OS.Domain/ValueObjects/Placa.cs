using System.Text.RegularExpressions;

namespace Oficina.OS.Domain.ValueObjects;

public partial record class Placa
{
    public string Valor { get; }

    [GeneratedRegex(@"^[A-Z]{3}-?[0-9]{4}$|^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex FormatoPlacaRegex();

    private Placa(string valor)
    {
        Valor = valor;
    }

    public static Placa? Criar(string valor)
    {
        if (!Validar(valor))
            return null;

        return new Placa(Normalizar(valor));
    }

    public static bool Validar(string placa)
    {
        if (string.IsNullOrWhiteSpace(placa)) return false;
        return FormatoPlacaRegex().IsMatch(Normalizar(placa));
    }

    private static string Normalizar(string placa) => placa.Replace("-", "").Trim().ToUpper();

    public override string ToString() => Valor;
}
