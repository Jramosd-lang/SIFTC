using System.Text.Json;

namespace SIFTC.Application.UseCases.Matriz;

// El microservicio Python devuelve un JSON variable en el caso exitoso.
// Se conserva como JsonElement para no acoplar el backend a un esquema no documentado.
public sealed record ValidacionMatrizResponse(
    bool EsValida,
    string? Mensaje,
    IReadOnlyList<JsonElement> Errores,
    JsonElement? Resultado
);
