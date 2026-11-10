using SIFTC.Application.UseCases.ConfiguracionIc;

namespace SIFTC.Tests;

public sealed class ConfiguracionIcValidatorTests
{
    private static readonly IReadOnlyList<PesoIcDto> PesosValidos =
    [
        new("Rentabilidad", 0.35m),
        new("Liquidez", 0.20m),
        new("Solvencia", 0.20m),
        new("Crecimiento", 0.25m)
    ];

    [Fact]
    public void Acepta_pesos_que_suman_uno()
    {
        var result = ConfiguracionIcValidator.Validar(
            "Ajuste de prueba",
            "Mensual",
            PesosValidos,
            []);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Rechaza_pesos_que_no_suman_uno()
    {
        var pesos = new List<PesoIcDto>
        {
            new("Rentabilidad", 0.35m),
            new("Liquidez", 0.20m),
            new("Solvencia", 0.20m),
            new("Crecimiento", 0.20m)
        };

        var result = ConfiguracionIcValidator.Validar(
            "Ajuste de prueba",
            "Mensual",
            pesos,
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("configuracionIc.pesos.suma", result.Error.Code);
    }

    [Fact]
    public void Rechaza_peso_negativo()
    {
        var pesos = new List<PesoIcDto>
        {
            new("Rentabilidad", 1.10m),
            new("Liquidez", -0.10m)
        };

        var result = ConfiguracionIcValidator.Validar(
            "Ajuste de prueba",
            "Mensual",
            pesos,
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("configuracionIc.peso.invalid", result.Error.Code);
    }

    [Fact]
    public void Rechaza_dimension_duplicada()
    {
        var pesos = new List<PesoIcDto>
        {
            new("Rentabilidad", 0.50m),
            new("Rentabilidad", 0.50m)
        };

        var result = ConfiguracionIcValidator.Validar(
            "Ajuste de prueba",
            "Mensual",
            pesos,
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("configuracionIc.peso.duplicado", result.Error.Code);
    }

    [Fact]
    public void Rechaza_frecuencia_invalida()
    {
        var result = ConfiguracionIcValidator.Validar(
            "Ajuste de prueba",
            "Semanal",
            PesosValidos,
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("configuracionIc.frecuencia.invalid", result.Error.Code);
    }

    [Fact]
    public void Rechaza_motivo_vacio()
    {
        var result = ConfiguracionIcValidator.Validar(
            " ",
            "Mensual",
            PesosValidos,
            []);

        Assert.True(result.IsFailure);
        Assert.Equal("configuracionIc.motivo.invalid", result.Error.Code);
    }

    [Fact]
    public void Rechaza_severidad_invalida()
    {
        var umbrales = new List<UmbralIcDto>
        {
            new("RNT-01", 0m, 10m, "Alta")
        };

        var result = ConfiguracionIcValidator.Validar(
            "Ajuste de prueba",
            "Mensual",
            PesosValidos,
            umbrales);

        Assert.True(result.IsFailure);
        Assert.Equal("configuracionIc.umbral.severidad.invalid", result.Error.Code);
    }

    [Fact]
    public void Rechaza_rango_de_umbral_invertido()
    {
        var umbrales = new List<UmbralIcDto>
        {
            new("RNT-01", 10m, 1m, "Advertencia")
        };

        var result = ConfiguracionIcValidator.Validar(
            "Ajuste de prueba",
            "Mensual",
            PesosValidos,
            umbrales);

        Assert.True(result.IsFailure);
        Assert.Equal("configuracionIc.umbral.rango", result.Error.Code);
    }
}
