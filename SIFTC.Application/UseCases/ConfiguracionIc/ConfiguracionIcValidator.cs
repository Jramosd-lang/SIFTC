using SIFTC.Application.Common;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.ConfiguracionIc;

public static class ConfiguracionIcValidator
{
    private const decimal ToleranciaSuma = 0.000001m;
    private static readonly string[] Severidades = ["Informativa", "Advertencia", "Critica"];

    public static Result Validar(
        string? motivo,
        string? frecuencia,
        IReadOnlyList<PesoIcDto>? pesos,
        IReadOnlyList<UmbralIcDto>? umbrales)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500)
        {
            return Result.Failure(new Error(
                "configuracionIc.motivo.invalid",
                "El motivo es obligatorio y no puede superar 500 caracteres.",
                ErrorType.Validation));
        }

        if (frecuencia is null || !FrecuenciaParser.TryParse(frecuencia, out _))
        {
            return Result.Failure(new Error(
                "configuracionIc.frecuencia.invalid",
                "La frecuencia admitida es Diaria, Mensual o Trimestral.",
                ErrorType.Validation));
        }

        if (pesos is null || pesos.Count == 0)
        {
            return Result.Failure(new Error(
                "configuracionIc.pesos.required",
                "Debe enviar al menos un peso.",
                ErrorType.Validation));
        }

        if (pesos.Any(p => string.IsNullOrWhiteSpace(p.Dimension)))
        {
            return Result.Failure(new Error(
                "configuracionIc.peso.dimension.required",
                "Todas las dimensiones son obligatorias.",
                ErrorType.Validation));
        }

        if (pesos.GroupBy(p => p.Dimension, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
        {
            return Result.Failure(new Error(
                "configuracionIc.peso.duplicado",
                "No se puede repetir una dimension.",
                ErrorType.Validation));
        }

        if (pesos.Any(p => p.Peso is < 0 or > 1))
        {
            return Result.Failure(new Error(
                "configuracionIc.peso.invalid",
                "Cada peso debe estar entre 0 y 1.",
                ErrorType.Validation));
        }

        if (Math.Abs(pesos.Sum(p => p.Peso) - 1m) > ToleranciaSuma)
        {
            return Result.Failure(new Error(
                "configuracionIc.pesos.suma",
                "Los pesos deben sumar 1.000000 (100 %).",
                ErrorType.Validation));
        }

        if (umbrales is null)
        {
            return Result.Success();
        }

        foreach (var umbral in umbrales)
        {
            if (string.IsNullOrWhiteSpace(umbral.CodigoMetrica))
            {
                return Result.Failure(new Error(
                    "configuracionIc.umbral.metrica.required",
                    "Cada umbral debe indicar el codigo de la metrica.",
                    ErrorType.Validation));
            }

            if (!Severidades.Contains(umbral.Severidad, StringComparer.Ordinal))
            {
                return Result.Failure(new Error(
                    "configuracionIc.umbral.severidad.invalid",
                    "La severidad admitida es Informativa, Advertencia o Critica.",
                    ErrorType.Validation));
            }

            if (umbral.LimiteInferior is not null &&
                umbral.LimiteSuperior is not null &&
                umbral.LimiteInferior > umbral.LimiteSuperior)
            {
                return Result.Failure(new Error(
                    "configuracionIc.umbral.rango",
                    "El limite inferior no puede ser mayor que el limite superior.",
                    ErrorType.Validation));
            }
        }

        return Result.Success();
    }
}

