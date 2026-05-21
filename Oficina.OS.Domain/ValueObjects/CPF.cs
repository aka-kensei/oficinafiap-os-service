using System.Text.RegularExpressions;

namespace Oficina.OS.Domain.ValueObjects;

public partial record class CPF
{
    public string Numero { get; }

    public CPF(string numero)
    {
        Numero = numero;
    }

    public static CPF? Criar(string numero)
    {
        if (!Validar(numero))
            return null;

        return new CPF(Limpar(numero));
    }

    public static bool Validar(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        var numeroLimpo = Limpar(cpf);

        if (numeroLimpo.Length != 11) return false;
        if (TodosDigitosIguais().IsMatch(numeroLimpo)) return false;

        return ValidarDigitosVerificadores(numeroLimpo);
    }

    private static bool ValidarDigitosVerificadores(string cpf)
    {
        var multiplicadores1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicadores2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = cpf[..9];
        var soma = 0;
        for (var i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicadores1[i];

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;
        tempCpf += digito1;

        soma = 0;
        for (var i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicadores2[i];

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return cpf.EndsWith($"{digito1}{digito2}");
    }

    private static string Limpar(string cpf) => ApenasNumeros().Replace(cpf, "");

    public override string ToString() => Numero;

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex ApenasNumeros();

    [GeneratedRegex(@"^(\d)\1{10}$")]
    private static partial Regex TodosDigitosIguais();
}
