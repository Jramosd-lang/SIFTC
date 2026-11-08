using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Token JWT devuelto por /api/v1/auth/login"
        };

        DescribirSchemas(document);
        return Task.CompletedTask;
    });

    // Corrige el requestBody de la carga de matriz para que Scalar muestre un selector de archivo.
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        if (context.Description.ActionDescriptor is ControllerActionDescriptor action &&
            action.ActionName is "Validar" or "Cargar")
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.String,
                                    Format = "binary"
                                }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };
        }

        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = new List<string>()
            });
        }

        return Task.CompletedTask;
    });
});

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
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is not null)
        {
            app.Logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);
        }

        await ProblemDetailsResponse.WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            "server.error",
            "Error interno",
            "Ocurrió un error interno al procesar la solicitud.");
    });
});
app.UseCors("Frontend");

// 3. Middlewares de seguridad (El orden es obligatorio)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

static void DescribirSchemas(OpenApiDocument document)
{
    Describir(document, "EntidadDto",
        ("id", "Identificador UUID de la entidad."),
        ("codigo", "Ticker o código único de la entidad, por ejemplo ENT001."),
        ("nombre", "Nombre comercial o razón social."),
        ("sector", "Sector económico al que pertenece."),
        ("ic", "Último Índice de Competitividad (0 a 100)."),
        ("variacion", "Variación absoluta frente al cierre anterior."),
        ("variacionPct", "Variación porcentual frente al cierre anterior."));

    Describir(document, "MetricaDto",
        ("entidadId", "Identificador UUID de la entidad."),
        ("codigo", "Ticker de la entidad."),
        ("nombre", "Nombre de la entidad."),
        ("sector", "Sector económico."),
        ("fecha", "Periodo del dato en formato ISO 8601 (yyyy-MM-dd)."),
        ("codigoMetrica", "Código del catálogo: RNT-01, LIQ-01, END-01, CRE-01, etc."),
        ("nombreMetrica", "Nombre legible de la métrica."),
        ("dimension", "Dimensión del catálogo: Rentabilidad, Liquidez, Solvencia, Crecimiento, Mercado o Trazabilidad."),
        ("formula", "Fórmula usada para el cálculo."),
        ("frecuencia", "Frecuencia de la métrica: Diaria, Mensual o Trimestral."),
        ("valor", "Valor calculado como proporción o veces, según la métrica."),
        ("valorNormalizado", "Valor llevado a escala 0 a 1 para el cálculo del IC."));

    Describir(document, "OhlcResponseDto",
        ("entidadId", "Identificador UUID de la entidad."),
        ("entidad", "Nombre de la entidad."),
        ("periodo", "Frecuencia usada: Diaria, Mensual o Trimestral."),
        ("serie", "Puntos OHLC ordenados de la fecha más antigua a la más reciente."));

    Describir(document, "OhlcPuntoDto",
        ("fecha", "Fecha del periodo en formato ISO 8601."),
        ("apertura", "Primer valor del IC en el periodo."),
        ("maximo", "Valor máximo del IC en el periodo."),
        ("minimo", "Valor mínimo del IC en el periodo."),
        ("cierre", "Último valor del IC en el periodo."),
        ("variacion", "Variación absoluta frente al cierre anterior."),
        ("variacionPct", "Variación porcentual frente al cierre anterior."));

    Describir(document, "RankingResponseDto",
        ("periodo", "Frecuencia solicitada: diario, mensual o trimestral."),
        ("total", "Cantidad total de entidades que cumplen los filtros."),
        ("items", "Filas ordenadas según orden y dir."));

    Describir(document, "RankingItemDto",
        ("posicion", "Posición actual (1 es la mejor); null para Gerente / Directivo."),
        ("cambioPosicion", "Positivo si subió, negativo si bajó, 0 si mantuvo; null para Gerente / Directivo."),
        ("entidadId", "Identificador UUID de la entidad."),
        ("entidad", "Nombre de la entidad."),
        ("sector", "Sector económico."),
        ("ic", "IC general o puntaje de la dimensión (0 a 100)."),
        ("variacionPct", "Variación porcentual frente al periodo anterior."));

    Describir(document, "AuthResponseDto",
        ("token", "JWT que debe enviarse como Bearer en los demás endpoints."),
        ("expiraEn", "Vigencia del token en segundos."),
        ("usuario", "Datos del usuario autenticado."));

    Describir(document, "AuthUsuarioDto",
        ("id", "Identificador UUID del usuario."),
        ("nombre", "Nombre completo."),
        ("rol", "Rol del sistema; determina los permisos."));

    Describir(document, "AutenticarUsuarioCommand",
        ("usuario", "Correo o usuario registrado. Es el identificador de login."),
        ("password", "Contraseña en texto plano; se valida contra el hash bcrypt."));

    Describir(document, "CrearUsuarioCommand",
        ("email", "Correo único del usuario."),
        ("password", "Contraseña en texto plano; el backend la guarda con bcrypt."),
        ("primerNombre", "Primer nombre."),
        ("primerApellido", "Primer apellido."),
        ("idRol", "UUID del rol existente en seguridad.roles."));

    Describir(document, "CrearMetricaCommand",
        ("codigo", "Código único, por ejemplo RNT-01."),
        ("nombre", "Nombre visible de la métrica."),
        ("formula", "Fórmula de cálculo."),
        ("frecuencia", "Diaria, Mensual o Trimestral."),
        ("idDimension", "UUID de la dimensión existente."));
}

static void Describir(
    OpenApiDocument document,
    string nombre,
    params (string Campo, string Descripcion)[] campos)
{
    if (document.Components?.Schemas is null)
    {
        return;
    }

    if (!document.Components.Schemas.TryGetValue(nombre, out var schema) ||
        schema is not OpenApiSchema concreto ||
        concreto.Properties is null)
    {
        return;
    }

    foreach (var (campo, descripcion) in campos)
    {
        if (concreto.Properties.TryGetValue(campo, out var propiedad) &&
            propiedad is OpenApiSchema propiedadConcreta)
        {
            propiedadConcreta.Description = descripcion;
        }
    }
}
