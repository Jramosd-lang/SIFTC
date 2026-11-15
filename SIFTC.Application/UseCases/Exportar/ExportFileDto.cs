namespace SIFTC.Application.UseCases.Exportar;

/// <summary>Archivo generado por una exportación.</summary>
/// <param name="Contenido">Bytes del archivo.</param>
/// <param name="ContentType">Tipo MIME del archivo.</param>
/// <param name="FileName">Nombre sugerido de descarga.</param>
/// <param name="TotalFilas">Filas de datos exportadas, sin contar la cabecera.</param>
public sealed record ExportFileDto(
    byte[] Contenido,
    string ContentType,
    string FileName,
    int TotalFilas
);
