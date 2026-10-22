using Microsoft.AspNetCore.Mvc;

namespace SIFTC.Api.Common;

internal static class ProblemDetailsResponse
{
    public static ProblemDetails Create(HttpContext httpContext, int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://siftc.local/problems/{code}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;
        return problem;
    }

    public static ObjectResult ToResult(HttpContext httpContext, int status, string code, string title, string detail) =>
        new(Create(httpContext, status, code, title, detail)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    public static async Task WriteAsync(HttpContext httpContext, int status, string code, string title, string detail)
    {
        var problem = Create(httpContext, status, code, title, detail);
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: httpContext.RequestAborted);
    }
}
