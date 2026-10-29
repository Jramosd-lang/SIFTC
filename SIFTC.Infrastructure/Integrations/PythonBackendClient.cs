using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SIFTC.Application.Common.Interfaces.Integrations;
using SIFTC.Application.UseCases.Matriz;
using SIFTC.Application.UseCases.Ranking;
using SIFTC.Domain.Common;

namespace SIFTC.Infrastructure.Integrations;

public sealed class PythonBackendClient(HttpClient httpClient) : IPythonBackendClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<RankingResponseDto>> GetRankingAsync(
        RankingQuery query,
        CancellationToken cancellationToken = default)
    {
        var url = ConstruirUrlRanking(query);

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return FalloDependencia<RankingResponseDto>(
                    "ranking.upstream_error",
                    "El microservicio de ranking no está disponible.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var ranking = JsonSerializer.Deserialize<RankingResponseDto>(body, JsonOptions);

            return ranking is null
                ? FalloDependencia<RankingResponseDto>("ranking.invalid_response", "El microservicio de ranking devolvió una respuesta inválida.")
                : Result.Success(ranking);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return FalloDependencia<RankingResponseDto>("ranking.timeout", "El microservicio de ranking no respondió a tiempo.");
        }
        catch (HttpRequestException)
        {
            return FalloDependencia<RankingResponseDto>("ranking.unreachable", "No fue posible comunicarse con el microservicio de ranking.");
        }
        catch (JsonException)
        {
            return FalloDependencia<RankingResponseDto>("ranking.invalid_response", "El microservicio de ranking devolvió una respuesta inválida.");
        }
    }

    public async Task<Result<ValidacionMatrizResponse>> ValidarMatrizAsync(
        Stream archivo,
        string nombreArchivo,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(archivo);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "text/csv" : contentType);
        form.Add(fileContent, "file", string.IsNullOrWhiteSpace(nombreArchivo) ? "matriz.csv" : nombreArchivo);

        try
        {
            using var response = await httpClient.PostAsync("v1/validar-matriz", form, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                using var documento = JsonDocument.Parse(body);
                var (mensaje, errores) = ParsearErrores(documento.RootElement);
                return Result.Success(new ValidacionMatrizResponse(false, mensaje, errores, null));
            }

            if (!response.IsSuccessStatusCode)
            {
                return FalloDependencia<ValidacionMatrizResponse>(
                    "matriz.upstream_error",
                    "El microservicio de validación no está disponible.");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return Result.Success(new ValidacionMatrizResponse(true, null, [], null));
            }

            using var documentoExitoso = JsonDocument.Parse(body);
            return Result.Success(new ValidacionMatrizResponse(
                true,
                null,
                [],
                documentoExitoso.RootElement.Clone()));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return FalloDependencia<ValidacionMatrizResponse>("matriz.timeout", "El microservicio de validación no respondió a tiempo.");
        }
        catch (HttpRequestException)
        {
            return FalloDependencia<ValidacionMatrizResponse>("matriz.unreachable", "No fue posible comunicarse con el microservicio de validación.");
        }
        catch (JsonException)
        {
            return FalloDependencia<ValidacionMatrizResponse>("matriz.invalid_response", "El microservicio de validación devolvió una respuesta inválida.");
        }
    }

    private static string ConstruirUrlRanking(RankingQuery query)
    {
        var parametros = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Sector))
        {
            parametros.Add($"sector={Uri.EscapeDataString(query.Sector)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Dimension))
        {
            parametros.Add($"dimension={Uri.EscapeDataString(query.Dimension)}");
        }

        parametros.Add($"periodo={Uri.EscapeDataString(query.Periodo)}");
        parametros.Add($"orden={Uri.EscapeDataString(query.Orden)}");
        parametros.Add($"dir={Uri.EscapeDataString(query.Dir)}");
        parametros.Add($"limit={query.Limit}");
        parametros.Add($"offset={query.Offset}");

        return "api/v1/ranking?" + string.Join("&", parametros);
    }

    private static (string? Mensaje, IReadOnlyList<JsonElement> Errores) ParsearErrores(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("detail", out var detail))
        {
            return ("El CSV contiene errores de validación.", []);
        }

        if (detail.ValueKind == JsonValueKind.Object)
        {
            var mensaje = detail.TryGetProperty("mensaje", out var mensajeElement) && mensajeElement.ValueKind == JsonValueKind.String
                ? mensajeElement.GetString()
                : null;

            var errores = new List<JsonElement>();
            if (detail.TryGetProperty("errores", out var erroresElement) && erroresElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in erroresElement.EnumerateArray())
                {
                    errores.Add(error.Clone());
                }
            }

            return (mensaje, errores);
        }

        if (detail.ValueKind == JsonValueKind.Array)
        {
            var errores = detail.EnumerateArray().Select(error => error.Clone()).ToList();
            return ("Error de validación del microservicio.", errores);
        }

        return ("El CSV contiene errores de validación.", []);
    }

    private static Result<T> FalloDependencia<T>(string codigo, string mensaje) =>
        Result.Failure<T>(new Error(codigo, mensaje, ErrorType.Dependency));
}
