using SIFTC.Domain.Enums;

namespace SIFTC.Application.Common;

public static class FrecuenciaParser
{
    public static bool TryParse(string? valor, out Frecuencia frecuencia)
    {
        frecuencia = default;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        switch (valor.Trim().ToLowerInvariant())
        {
            case "diaria":
            case "diario":
                frecuencia = Frecuencia.Diaria;
                return true;

            case "mensual":
                frecuencia = Frecuencia.Mensual;
                return true;

            case "trimestral":
                frecuencia = Frecuencia.Trimestral;
                return true;

            default:
                return false;
        }
    }
}
