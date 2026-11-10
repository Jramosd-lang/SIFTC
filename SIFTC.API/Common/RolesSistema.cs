namespace SIFTC.Api.Common;

public static class RolesSistema
{
    public const string Administrador = "Administrador";
    public const string DataSteward = "Data Steward";
    public const string AnalistaFinanciero = "Analista financiero";
    public const string Gerente = "Gerente / Directivo";
    public const string Auditor = "Auditor";
    public const string UsuarioMovil = "Usuario móvil";

    public const string Consulta =
        Administrador + "," +
        DataSteward + "," +
        AnalistaFinanciero + "," +
        Gerente + "," +
        Auditor + "," +
        UsuarioMovil;

    public const string Ranking =
        Administrador + "," +
        DataSteward + "," +
        AnalistaFinanciero + "," +
        Gerente + "," +
        Auditor;

    public const string CargaMatriz = AnalistaFinanciero + "," + Administrador;

    public const string AprobacionesLectura =
        Administrador + "," +
        DataSteward + "," +
        AnalistaFinanciero + "," +
        Auditor;

    public const string AprobacionesDecision = AnalistaFinanciero;

    // Escritura de parámetros (actualizar umbral).
    public const string Parametros = AnalistaFinanciero + "," + Administrador;

    // Lectura de parámetros (pesos, fórmulas y frecuencias). D3: el Auditor solo lee.
    public const string ParametrosLectura = Parametros + "," + Auditor;
}
