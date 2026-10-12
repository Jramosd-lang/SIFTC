using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SIFTC.Application;
using SIFTC.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Conectar la base de datos y repositorios de Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
        ValidIssuer = "SIFTC.Backend",
        ValidAudience = "SIFTC.Clients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
    };

    // AQUÍ OBLIGAS AL JWT A USAR EL FORMATO RFC 7807 (Problem Details)
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Le decimos a .NET que nosotros manejaremos la respuesta, que no envíe su 401 vacío
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                title = "No autorizado",
                status = 401,
                detail = "El token es inválido, ha expirado o no fue enviado en la cabecera."
            };
            await context.Response.WriteAsJsonAsync(problem);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Acceso denegado (Forbidden)",
                status = 403,
                detail = "Tu rol no tiene los permisos necesarios para consumir este endpoint."
            };
            await context.Response.WriteAsJsonAsync(problem);
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

// 3. Middlewares de seguridad (El orden es obligatorio)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();