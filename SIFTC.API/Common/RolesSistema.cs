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

    public const string Parametros = AnalistaFinanciero + "," + Administrador;

    public const string ParametrosLectura = Parametros + "," + Auditor;

    // D4: bitácora de auditoría. Solo Administrador y Auditor consultan.
    public const string AuditoriaLectura = Administrador + "," + Auditor;
}
