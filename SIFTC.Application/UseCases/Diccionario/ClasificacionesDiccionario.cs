namespace SIFTC.Application.UseCases.Diccionario;

/// <summary>
/// Filtro por rol de la HU-DD-08 (sección 2.4), según la clasificación de
/// sensibilidad (HU-DD-06) y la matriz de permisos (HU-BE-02).
///
/// | Clasificación | Quién la ve                                        |
/// |---------------|----------------------------------------------------|
/// | Público       | Todos                                              |
/// | Interno       | Todos los usuarios con sesión                      |
/// | Confidencial  | Administrador, Data Steward, Analista y Auditor    |
/// | Restringido   | Nadie desde la API (nunca está en la lista)        |
///
/// Los nombres de rol coinciden con la semilla de seguridad.roles y con
/// API/Common/RolesSistema.cs. Cualquier rol desconocido ve solo la base.
/// </summary>
public static class ClasificacionesDiccionario
{
    public const string Publico = "Público";
    public const string Interno = "Interno";
    public const string Confidencial = "Confidencial";
    public const string Restringido = "Restringido";

    private static readonly string[] VisiblesPorDefecto = [Publico, Interno];

    private static readonly string[] VisiblesConConfidencial =
        [Publico, Interno, Confidencial];

    // "Analista" del documento = "Analista financiero" en SIFTC.
    private static readonly string[] RolesQueVenConfidencial =
    [
        "Administrador",
        "Data Steward",
        "Analista financiero",
        "Auditor"
    ];

    /// <summary>Clasificaciones que el rol puede consultar vía API.</summary>
    public static IReadOnlyList<string> VisiblesPara(string? rol) =>
        rol is not null && RolesQueVenConfidencial.Contains(rol, StringComparer.Ordinal)
            ? VisiblesConConfidencial
            : VisiblesPorDefecto;
}
