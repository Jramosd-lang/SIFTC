using System.Diagnostics;

namespace SIFTC.LoadTests;

public static class Reporte
{
    public static int Imprimir(
        OpcionesPrueba opciones,
        IReadOnlyList<ClienteSimulado> clientes,
        int fallosConexion,
        long inicioEmision)
    {
        var latencias = clientes
            .Where(c => c.RecibidoEnTicks is not null)
            .Select(c => Stopwatch.GetElapsedTime(inicioEmision, c.RecibidoEnTicks!.Value).TotalMilliseconds)
            .Order()
            .ToArray();

        var sinRecibir = clientes.Count - latencias.Length;
        var duplicados = clientes.Count(c => c.Pruebas > 1);
        var sinEstadoInicial = clientes.Count(c => c.EstadosIniciales == 0);
        var estadoInicialRepetido = clientes.Count(c => c.EstadosIniciales > 1);
        var sobreUmbral = latencias.Count(l => l >= opciones.UmbralMs);

        Console.WriteLine();
        Console.WriteLine("===== Resultado HU-BE-04 =====");
        Console.WriteLine($"Clientes solicitados:        {opciones.Clientes}");
        Console.WriteLine($"Clientes conectados:         {clientes.Count} (fallos: {fallosConexion})");
        Console.WriteLine($"Sin estado inicial:          {sinEstadoInicial}");
        Console.WriteLine($"Estado inicial repetido:     {estadoInicialRepetido}");
        Console.WriteLine($"Recibieron la emisión:       {latencias.Length}");
        Console.WriteLine($"No la recibieron:            {sinRecibir}");
        Console.WriteLine($"Con mensajes duplicados:     {duplicados}");

        if (latencias.Length > 0)
        {
            Console.WriteLine($"Latencia p50 / p95 / p99:    {Percentil(latencias, 50):F0} / {Percentil(latencias, 95):F0} / {Percentil(latencias, 99):F0} ms");
            Console.WriteLine($"Latencia máxima:             {latencias[^1]:F0} ms");
            Console.WriteLine($"Sobre el umbral ({opciones.UmbralMs} ms):   {sobreUmbral}");
        }

        var aprobado = fallosConexion == 0
            && clientes.Count == opciones.Clientes
            && sinRecibir == 0
            && sobreUmbral == 0
            && duplicados == 0
            && sinEstadoInicial == 0
            && estadoInicialRepetido == 0;

        Console.WriteLine(aprobado ? "RESULTADO: APROBADO" : "RESULTADO: NO APROBADO");
        return aprobado ? 0 : 1;
    }

    private static double Percentil(double[] ordenados, int percentil)
    {
        var indice = (int)Math.Ceiling(percentil / 100.0 * ordenados.Length) - 1;
        return ordenados[Math.Clamp(indice, 0, ordenados.Length - 1)];
    }
}
