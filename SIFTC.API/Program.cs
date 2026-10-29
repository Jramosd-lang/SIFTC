using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SIFTC.Api.Common;
using SIFTC.Application;
using SIFTC.Infrastructure;
using SIFTC.Infrastructure.Authentication;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Conectar la base de datos y repositorios de Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    };
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var allowAnyOrigin = builder.Configuration.GetValue<bool?>("Cors:AllowAnyOrigin") ?? true;
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowAnyOrigin)
    {
        policy.AllowAnyOrigin();
    }
    else
    {
        policy.WithOrigins(allowedOrigins);
    }

    policy.AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(PaginacionHeaders.Total);
}));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("La configuración JWT es obligatoria.");

if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException("JwtSettings debe configurarse mediante secretos o variables de entorno.");
}

// 2. Configuración de Seguridad: Autenticación JWT (Unificado)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };

    // Las respuestas de autenticación usan Problem Details conforme a RFC 9457.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();

            await ProblemDetailsResponse.WriteAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "auth.unauthorized",
                "Unauthorized",
                "Token inválido o vencido",
                ProblemDetailsResponse.AboutBlank);
        },
        OnForbidden = async context =>
        {
            await ProblemDetailsResponse.WriteAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "auth.forbidden",
                "Forbidden",
                "No tiene permiso",
                ProblemDetailsResponse.AboutBlank);
        }
    };
});

// Los errores de binding (ej. page=abc) usan el mismo formato Problem Details que el resto de la API.
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var campos = string.Join(", ", context.ModelState.Where(e => e.Value?.Errors.Count > 0).Select(e => e.Key));
        return ProblemDetailsResponse.ToResult(
            context.HttpContext,
            StatusCodes.Status400BadRequest,
            "request.invalid",
            "Solicitud incorrecta",
            $"Parámetros inválidos: {campos}.");
    };
});
builder.Services.AddOpenApi();

var app = builder.Build();

var documentationEnabled = builder.Configuration.GetValue<bool?>("Documentation:Enabled") ?? true;
if (documentationEnabled)
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options.WithTitle("SIFTC API");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("Frontend");

// 3. Middlewares de seguridad (El orden es obligatorio)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
