using Microsoft.AspNetCore.Mvc;

namespace SIFTC.Api.Common;

internal static class ProblemDetailsResponse
{
    public const string AboutBlank = "about:blank";

    public static ProblemDetails Create(HttpContext httpContext, int status, string code, string title, string detail, string? type = null)
    {
        var problem = new ProblemDetails
        {
            Type = type ?? $"https://siftc.local/problems/{code}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["code"] = code;
        return problem;
    }

    public static ObjectResult ToResult(HttpContext httpContext, int status, string code, string title, string detail, string? type = null) =>
        new(Create(httpContext, status, code, title, detail, type))
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };

    public static async Task WriteAsync(HttpContext httpContext, int status, string code, string title, string detail, string? type = null)
    {
        var problem = Create(httpContext, status, code, title, detail, type);
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: httpContext.RequestAborted);
    }
}
