using Microsoft.AspNetCore.Mvc;

namespace SIFTC.Api.Common;

internal static class ProblemDetailsResponse
{
    public static ProblemDetails Create(HttpContext httpContext, int status, string code, string title, string detail) => new()
    {
        Type = $"https://siftc.local/problems/{code}",
        Title = title,
        Status = status,
        Detail = detail,
        Instance = httpContext.Request.Path
    };

    public static async Task WriteAsync(HttpContext httpContext, int status, string code, string title, string detail)
    {
        var problem = Create(httpContext, status, code, title, detail);
        problem.Extensions["code"] = code;
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: httpContext.RequestAborted);
    }
}
