namespace SIFTC.LoadTests;

// Parámetros de la prueba. Cada uno se puede pasar como argumento (--clientes 1000)
// o como variable de entorno (SIFTC_CLIENTES=1000); el argumento tiene prioridad.
public sealed record OpcionesPrueba(
    Uri UrlBase,
    string? Token,
    string? Usuario,
    string? Password,
    int Clientes,
    int UmbralMs,
    int LoteConexion,
    int EsperaSegundos)
{
    public const string Ayuda = """
        Uso: dotnet run --project SIFTC.LoadTests -c Release -- [opciones]

          --url <url>            URL base de la API (SIFTC_URL). Por defecto http://localhost:5057
          --token <jwt>          JWT de un Administrador (SIFTC_TOKEN). Alternativa a usuario/password.
          --usuario <correo>     Usuario Administrador para iniciar sesión (SIFTC_USUARIO).
          --password <clave>     Contraseña de ese usuario (SIFTC_PASSWORD).
          --clientes <n>         Conexiones simultáneas (SIFTC_CLIENTES). Por defecto 1000.
          --umbral-ms <ms>       Latencia máxima aceptada (SIFTC_UMBRAL_MS). Por defecto 2000.
          --lote-conexion <n>    Conexiones que se abren a la vez (SIFTC_LOTE_CONEXION). Por defecto 50.
          --espera-s <s>         Tiempo máximo esperando la emisión (SIFTC_ESPERA_S). Por defecto 15.

        Código de salida: 0 si todos los clientes reciben la emisión bajo el umbral, 1 si no, 2 si hay un error de configuración.
        """;

    public static OpcionesPrueba Leer(string[] args)
    {
        var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Argumento inesperado: {args[i]}");
            }

            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Falta el valor de {args[i]}.");
            }

            valores[args[i][2..]] = args[++i];
        }

        string? Valor(string nombre) =>
            valores.TryGetValue(nombre, out var valor)
                ? valor
                : Environment.GetEnvironmentVariable("SIFTC_" + nombre.Replace('-', '_').ToUpperInvariant());

        int Entero(string nombre, int porDefecto)
        {
            var texto = Valor(nombre);
            if (texto is null)
            {
                return porDefecto;
            }

            return int.TryParse(texto, out var numero) && numero > 0
                ? numero
                : throw new ArgumentException($"--{nombre} debe ser un entero positivo.");
        }

        var opciones = new OpcionesPrueba(
            new Uri(Valor("url") ?? "http://localhost:5057"),
            Valor("token"),
            Valor("usuario"),
            Valor("password"),
            Entero("clientes", 1000),
            Entero("umbral-ms", 2000),
            Entero("lote-conexion", 50),
            Entero("espera-s", 15));

        if (opciones.Token is null && (opciones.Usuario is null || opciones.Password is null))
        {
            throw new ArgumentException("Indique --token o bien --usuario y --password de un Administrador.");
        }

        return opciones;
    }
}
