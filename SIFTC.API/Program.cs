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
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

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
            // Le decimos a .NET que nosotros manejaremos la respuesta, que no envíe su 401 vacío
            context.HandleResponse();

            await ProblemDetailsResponse.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "auth.unauthorized", "No autorizado", "El token es inválido, ha expirado o no fue enviado en la cabecera.");
        },
        OnForbidden = async context =>
        {
            await ProblemDetailsResponse.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "auth.forbidden", "Acceso denegado", "Tu rol no tiene los permisos necesarios para consumir este endpoint.");
        }
    };
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("Frontend");

// 3. Middlewares de seguridad (El orden es obligatorio)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
