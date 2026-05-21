using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oficina.OS.Domain.ValueObjects;

namespace Oficina.OS.Infrastructure.Database.Converters;

public class CpfConverter : ValueConverter<CPF, string>
{
    public CpfConverter() : base(
        cpf => cpf.Numero,
        numero => new CPF(numero))
    { }
}

public class PlacaConverter : ValueConverter<Placa, string>
{
    public PlacaConverter() : base(
        placa => placa.Valor,
        valor => Placa.Criar(valor)!)
    { }
}
