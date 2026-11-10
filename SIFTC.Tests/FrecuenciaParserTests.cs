using SIFTC.Application.Common;
using SIFTC.Domain.Enums;

namespace SIFTC.Tests;

public sealed class FrecuenciaParserTests
{
    [Theory]
    [InlineData("Diaria", Frecuencia.Diaria)]
    [InlineData("diaria", Frecuencia.Diaria)]
    [InlineData("diario", Frecuencia.Diaria)]
    [InlineData("Mensual", Frecuencia.Mensual)]
    [InlineData("mensual", Frecuencia.Mensual)]
    [InlineData("Trimestral", Frecuencia.Trimestral)]
    [InlineData("trimestral", Frecuencia.Trimestral)]
    public void Acepta_valores_canonicos_y_alias(string valor, Frecuencia esperada)
    {
        var valido = FrecuenciaParser.TryParse(valor, out var frecuencia);

        Assert.True(valido);
        Assert.Equal(esperada, frecuencia);
    }

    [Theory]
    [InlineData("semanal")]
    [InlineData("")]
    [InlineData(null)]
    public void Rechaza_valores_invalidos(string? valor)
    {
        Assert.False(FrecuenciaParser.TryParse(valor, out _));
    }
}
