using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SIFTC.Api.Common;
using SIFTC.Api.Notifications;
using SIFTC.Api.Realtime;
using SIFTC.Application;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Infrastructure;
using SIFTC.Infrastructure.Authentication;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Conectar la base de datos y repositorios de Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTiempoReal(builder.Configuration);

// HU-AD-10 / HU-MO-05: cola de notificaciones (correo + push) en background.
builder.Services.Configure<NotificacionesOptions>(
    builder.Configuration.GetSection(NotificacionesOptions.SectionName));
builder.Services.AddHostedService<NotificacionesWorker>();
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
        // AllowCredentials permite el negotiate de SignalR con su configuración por defecto en el navegador.
        policy.WithOrigins(allowedOrigins).AllowCredentials();
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
        // Los WebSockets del navegador no envían el header Authorization: el hub recibe el JWT en ?access_token=.
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments(CotizacionesHub.Ruta))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
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
        document.Info = new OpenApiInfo
        {
            Title = "SIFTC API - Sistema de Índice de Competitividad",
            Version = "v1.0",
            Description = """
            API REST oficial del Sistema de Índice de Competitividad (SIFTC) desarrollada en .NET con Clean Architecture, CQRS, MediatR y PostgreSQL.

            ### Convenciones Generales
            * **Prefijo de Rutas:** Todas las operaciones de negocio usan el prefijo `/api/v1` (salvo `/health`, `/docs` y `/openapi`).
            * **Formatos de Entrada/Salida:** JSON UTF-8 con propiedades en `camelCase`, fechas en formato ISO 8601 (`yyyy-MM-dd` o `yyyy-MM-ddTHH:mm:ssZ`) y números JSON (nunca formateados como texto).
            * **Autenticación:** JWT Bearer Token vía cabecera `Authorization: Bearer <token>`. Se obtiene en `POST /api/v1/auth/login`.
            * **Respuestas de Error:** Especificación RFC 9457 `application/problem+json` con URI de tipo, título, código HTTP, detalle descriptivo, instancia (`path`) y código de error de negocio estable (`code`).

            ### Catálogo Oficial de Roles del Sistema
            Para operaciones de registro y edición de usuarios (`idRol`), la base de datos `seguridad.roles` define los siguientes roles y sus correspondientes UUIDs:
            * **Administrador** (`179acd26-b52c-4e75-8c26-0b6b1b5737e4`): Gestión total del sistema (usuarios, roles y versiones de parámetros de configuración del IC).
            * **Analista financiero** (`bacadb8f-63d4-4032-8c43-729f597623b4`): Carga de matrices financieras CSV, ejecución de cálculos, y aprobación o rechazo de publicaciones de lotes.
            * **Auditor** (`36078d03-86e6-4075-a7b5-c82b3e414760`): Lectura de bitácora de auditoría, trazabilidad, linaje y lectura de la matriz cruda sin transformación.
            * **Data Steward** (`ea07d6fd-a849-4764-8b03-688163837303`): Carga y mantenimiento del diccionario de datos, reglas de calidad y lectura de matriz cruda.
            * **Gerente / Directivo** (`c43ddacf-b545-4190-b051-c8172e3026f0`): Consulta de tableros, series OHLC y ranking (con posición y cambio de posición en `null` por cumplimiento de políticas de visualización).
            * **Usuario móvil** (`c6152fdb-6b06-4c89-95b9-42b6222320f1`): Consulta del mercado bursátil y gestión exclusiva de su watchlist personal.

            ### Canal de Cotizaciones en Tiempo Real (SignalR)
            * **Ruta exacta del Hub:** `/hubs/cotizaciones` (la negociación de SignalR se realiza en `POST /hubs/cotizaciones/negotiate`).
            * **Autenticación en WebSockets:** El token JWT se pasa mediante el parámetro de consulta `?access_token=<token>` (automático con `accessTokenFactory` en clientes SignalR).
            * **Evento emitido:** El hub emite el evento `"cotizaciones"` con el snapshot completo del mercado al conectarse y en cada publicación aprobada.

            ### Niveles de Sensibilidad de Datos
            * **Público:** Libre para todos los roles autenticados (entidades, series OHLC, cotizaciones).
            * **Interno:** Datos analíticos de operación interna (visibles para cualquier usuario con sesión iniciada).
            * **Confidencial:** Datos financieros detallados, ranking por dimensiones y bitácora cruda (restringido a roles autorizados: Admin, Data Steward, Analista y Auditor).
            * **Restringido:** Información personal y credenciales de seguridad protegidas por la Ley 1581 / RNF05; nunca se exponen mediante la API externa.
            """
        };

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
app.MapTiempoReal();
app.Run();

static void DescribirSchemas(OpenApiDocument document)
{
    Describir(document, "EntidadDto",
        ("id", "Identificador UUID único de la entidad."),
        ("codigo", "Ticker o código bursátil único de la entidad (ej. ENT001)."),
        ("nombre", "Nombre comercial o razón social oficial de la entidad."),
        ("sector", "Sector económico al que pertenece (ej. Energía, Financiero)."),
        ("ic", "Último Índice de Competitividad calculado (escala de 0 a 100)."),
        ("variacion", "Variación absoluta del IC frente al periodo anterior."),
        ("variacionPct", "Variación porcentual del IC frente al periodo anterior."));

    Describir(document, "MetricaDto",
        ("entidadId", "Identificador UUID de la entidad evaluada."),
        ("codigo", "Ticker único de la entidad."),
        ("nombre", "Nombre de la entidad."),
        ("sector", "Sector económico de la entidad."),
        ("fecha", "Periodo del dato en formato ISO 8601 (yyyy-MM-dd). Rango histórico: 2024-10-01 al 2026-09-01."),
        ("codigoMetrica", "Código del catálogo: RNT-01 (ROE), RNT-02 (ROA), RNT-03 (Margen EBITDA), LIQ-01 (Razón corriente), LIQ-02 (Prueba ácida), END-01 (Endeudamiento), END-02 (Cobertura intereses), CRE-01 (Crecimiento ingresos), CRE-02 (Variación cotización), CAL-01 (Calidad dato)."),
        ("nombreMetrica", "Nombre legible de la métrica financiera."),
        ("dimension", "Dimensión evaluada: Rentabilidad, Liquidez, Solvencia, Crecimiento, Mercado o Trazabilidad."),
        ("formula", "Fórmula matemática aplicada en el cálculo."),
        ("frecuencia", "Frecuencia de cálculo del catálogo: Diaria, Mensual o Trimestral."),
        ("valor", "Valor calculado (en proporción para porcentajes o en veces para ratios)."),
        ("valorNormalizado", "Valor llevado a escala estándar de 0 a 1 (reemplazado en el IC vigente por VPN directo)."));

    Describir(document, "OhlcResponseDto",
        ("entidadId", "Identificador UUID de la entidad."),
        ("entidad", "Nombre comercial de la entidad."),
        ("periodo", "Frecuencia solicitada: Diaria, Mensual o Trimestral. En Diaria/Mensual los valores coinciden; en Trimestral se agrupan 3 meses formando velas con rango real."),
        ("serie", "Puntos OHLC ordenados cronológicamente de la fecha más antigua a la más reciente."));

    Describir(document, "OhlcPuntoDto",
        ("fecha", "Fecha del periodo en formato ISO 8601 (yyyy-MM-dd)."),
        ("apertura", "Primer valor del IC registrado en el periodo."),
        ("maximo", "Valor máximo del IC alcanzado en el periodo."),
        ("minimo", "Valor mínimo del IC alcanzado en el periodo."),
        ("cierre", "Último valor del IC (cierre) registrado en el periodo."),
        ("variacion", "Variación absoluta de puntos frente al cierre del periodo anterior."),
        ("variacionPct", "Variación porcentual frente al cierre del periodo anterior."));

    Describir(document, "RankingResponseDto",
        ("periodo", "Frecuencia del ranking: Diaria, Mensual o Trimestral (fuente vigente: mensual)."),
        ("total", "Cantidad total de entidades que cumplen con los filtros aplicados."),
        ("items", "Filas del ranking ordenadas según el campo y dirección solicitados."));

    Describir(document, "RankingItemDto",
        ("posicion", "Posición actual en el ranking (1 es la mejor). Por política de cumplimiento, se entrega en null para el rol Gerente / Directivo."),
        ("cambioPosicion", "Cambio de puesto frente al periodo anterior (> 0 subió, < 0 bajó, 0 se mantuvo; null para Gerente / Directivo)."),
        ("entidadId", "Identificador UUID de la entidad."),
        ("entidad", "Nombre comercial de la entidad."),
        ("sector", "Sector económico."),
        ("ic", "Puntaje del Índice de Competitividad o de la dimensión consultada."),
        ("variacionPct", "Variación porcentual del IC frente al periodo anterior."));

    Describir(document, "PublicacionPendienteDto",
        ("idLote", "Identificador UUID del lote de carga."),
        ("nombreArchivo", "Nombre original del archivo CSV procesado."),
        ("fechaCarga", "Momento exacto en que se registró el lote en la capa cruda."),
        ("registrosTotales", "Filas totales leídas del archivo CSV."),
        ("registrosValidos", "Filas válidas sin incumplimientos bloqueantes aprobadas por el ETL."),
        ("estadoCarga", "Resultado del cargue: Exitoso o Fallido."),
        ("estado", "Estado del ciclo de aprobación: pendiente, publicado o rechazado."),
        ("idUsuarioEjecutor", "UUID del analista que cargó el archivo."),
        ("idUsuarioDecision", "UUID del analista que aprobó o rechazó (null mientras esté pendiente)."),
        ("fechaDecision", "Fecha y hora de la decisión (null mientras esté pendiente)."),
        ("comentario", "Motivo de rechazo registrado por el analista (null si está pendiente o fue aprobado)."));

    Describir(document, "RechazarPublicacionRequest",
        ("comentario", "Motivo detallado del rechazo del lote (obligatorio, máximo 500 caracteres)."));

    Describir(document, "AuthResponseDto",
        ("token", "JWT que debe enviarse en la cabecera 'Authorization: Bearer <token>' en los endpoints protegidos."),
        ("expiraEn", "Vigencia del token en segundos (por defecto 28800 segundos = 8 horas)."),
        ("usuario", "Datos del usuario que inició sesión."));

    Describir(document, "AuthUsuarioDto",
        ("id", "Identificador UUID del usuario en seguridad.usuarios."),
        ("nombre", "Nombre completo del usuario."),
        ("rol", "Rol del sistema que determina los permisos operativos (ej. Administrador, Analista financiero, etc.)."));

    Describir(document, "AutenticarUsuarioCommand",
        ("usuario", "Correo electrónico registrado del usuario (identificador de login)."),
        ("password", "Contraseña en texto plano; se valida contra el hash seguro bcrypt. Los usuarios inactivos reciben 401."));

    Describir(document, "CrearUsuarioCommand",
        ("email", "Correo electrónico único del usuario (se normaliza a minúsculas)."),
        ("password", "Contraseña en texto plano; el backend genera y persiste el hash bcrypt."),
        ("primerNombre", "Primer nombre del usuario (obligatorio)."),
        ("primerApellido", "Primer apellido del usuario (obligatorio)."),
        ("idRol", "UUID del rol en seguridad.roles. Catálogo: Administrador (179acd26-b52c-4e75-8c26-0b6b1b5737e4), Analista financiero (bacadb8f-63d4-4032-8c43-729f597623b4), Auditor (36078d03-86e6-4075-a7b5-c82b3e414760), Data Steward (ea07d6fd-a849-4764-8b03-688163837303), Gerente / Directivo (c43ddacf-b545-4190-b051-c8172e3026f0), Usuario móvil (c6152fdb-6b06-4c89-95b9-42b6222320f1)."));

    Describir(document, "ActualizarUsuarioSolicitud",
        ("email", "Nuevo correo electrónico del usuario (debe ser único en el sistema)."),
        ("primerNombre", "Primer nombre (obligatorio)."),
        ("segundoNombre", "Segundo nombre (opcional; null o vacío si no tiene)."),
        ("primerApellido", "Primer apellido (obligatorio)."),
        ("segundoApellido", "Segundo apellido (opcional; null o vacío si no tiene)."),
        ("idRol", "UUID del rol asignado. Catálogo: Administrador (179acd26-b52c-4e75-8c26-0b6b1b5737e4), Analista financiero (bacadb8f-63d4-4032-8c43-729f597623b4), Auditor (36078d03-86e6-4075-a7b5-c82b3e414760), Data Steward (ea07d6fd-a849-4764-8b03-688163837303), Gerente / Directivo (c43ddacf-b545-4190-b051-c8172e3026f0), Usuario móvil (c6152fdb-6b06-4c89-95b9-42b6222320f1)."),
        ("password", "Nueva contraseña en texto plano (opcional). Si se omite, es null o llega vacía, la contraseña actual se mantiene intacta."));

    Describir(document, "UsuarioDto",
        ("idUsuario", "Identificador UUID del usuario."),
        ("email", "Correo electrónico registrado."),
        ("primerNombre", "Primer nombre."),
        ("segundoNombre", "Segundo nombre (si fue registrado)."),
        ("primerApellido", "Primer apellido."),
        ("segundoApellido", "Segundo apellido (si fue registrado)."),
        ("idRol", "Identificador UUID del rol asignado."),
        ("rol", "Nombre oficial del rol (Administrador, Analista financiero, Auditor, Data Steward, Gerente / Directivo, Usuario móvil)."),
        ("estado", "Estado del usuario: 'activo' (puede operar) o 'inactivo' (baja lógica, no puede ingresar al sistema)."));

    Describir(document, "CargaDiccionarioResponseDto",
        ("registros", "Cantidad total de filas insertadas tras reemplazar exitosamente la tabla analitica.diccionario."));

    Describir(document, "DiccionarioResultadoDto",
        ("codigo", "Código identificador de la ficha (ej. RNT-01, codigo_entidad, GLO-IC-INDICE-DE-COMPETITIVIDAD)."),
        ("tipo", "Tipo de elemento: dato (campo técnico de matriz), metrica (indicador financiero) o termino (concepto del glosario)."),
        ("nombre", "Nombre de negocio o término legible."),
        ("definicion", "Definición o descripción funcional corta."));

    Describir(document, "DiccionarioFichaDto",
        ("codigo", "Código único de la ficha."),
        ("tipo", "Tipo: dato, metrica o termino."),
        ("nombre", "Nombre oficial del concepto o métrica."),
        ("definicion", "Definición detallada."),
        ("formula", "Fórmula matemática (solo tipo=metrica; null en los demás)."),
        ("unidad", "Unidad de medida (ej. %, Millones COP, Veces; null si no aplica)."),
        ("sinonimos", "Arreglo de sinónimos o alias asociados."),
        ("clasificacion", "Nivel de sensibilidad del dato: Público, Interno, Confidencial o Restringido."));

    Describir(document, "CrearReglaRequest",
        ("canal", "Canal de alerta: correo, push o ambos (ambos encola dos filas independientes)."),
        ("idEntidad", "UUID de la entidad a monitorear. Si es null o se omite, la regla aplica a TODAS las entidades."),
        ("umbral", "Variación porcentual mínima para emitir la notificación (ej. 2.5 para 2.5%). Si es null, cualquier cambio dispara el aviso."));

    Describir(document, "NotificacionReglaDto",
        ("idRegla", "Identificador UUID de la regla."),
        ("canal", "Canal configurado: correo, push o ambos."),
        ("idEntidad", "UUID de la entidad monitoreada (null = todas las entidades)."),
        ("umbral", "Umbral porcentual mínimo (null = cualquier cambio)."),
        ("activa", "Indica si la regla está activa (las inactivas no se devuelven)."),
        ("fechaCreacion", "Momento UTC en que se creó la regla."));

    Describir(document, "RegistrarTokenRequest",
        ("plataforma", "Plataforma de origen: web (PWA), android o ios."),
        ("token", "Registration token entregado por el SDK de Firebase Cloud Messaging (FCM)."));

    Describir(document, "NotificacionTokenDto",
        ("idToken", "Identificador interno UUID del registro del token."),
        ("plataforma", "Plataforma registrada: web, android o ios."),
        ("activo", "Estado del token para recepción de mensajes push."),
        ("fechaRegistro", "Momento UTC en que se registró el token. El token en sí nunca se devuelve por seguridad."));

    Describir(document, "RegistrarEventoRequest",
        ("idEntidad", "UUID de la entidad donde se registró la variación."),
        ("variacionPct", "Porcentaje de variación de cotización registrado (ej. 3.5 o -1.8)."),
        ("nombreEntidad", "Nombre comercial de la entidad (opcional; si se omite, el backend lo resuelve)."));

    Describir(document, "RegistrarEventoResponseDto",
        ("encolados", "Cantidad de avisos generados y puestos en cola de envío tras evaluar las reglas vigentes."));

    Describir(document, "NotificacionEnvioDto",
        ("idEnvio", "Identificador UUID del envío en cola."),
        ("idRegla", "UUID de la regla que lo generó (null si fue directo por evento)."),
        ("canal", "Canal concreto: correo o push."),
        ("asunto", "Asunto del correo o título de la notificación push."),
        ("estado", "Estado del despacho: pendiente, enviado o fallido."),
        ("intentos", "Cantidad de intentos de envío realizados hasta el momento."),
        ("maxIntentos", "Tope máximo de reintentos configurado (3)."),
        ("ultimoError", "Detalle técnico del último error si hubo fallos transitorios."),
        ("fechaCreacion", "Momento UTC en que entró a la cola."),
        ("fechaUltimoIntento", "Momento UTC del intento más reciente."),
        ("fechaEnvio", "Momento UTC en que se entregó exitosamente."));

    Describir(document, "ConfiguracionIcDto",
        ("idVersion", "UUID único de la versión de configuración del IC."),
        ("numeroVersion", "Número secuencial de la versión."),
        ("fechaVigencia", "Momento UTC desde el que rige esta versión."),
        ("motivo", "Justificación del cambio de parámetros."),
        ("idUsuario", "UUID del administrador que configuró la versión."),
        ("frecuenciaIc", "Frecuencia de cálculo del IC: Diaria, Mensual o Trimestral."),
        ("pesos", "Arreglo de pesos por dimensión en proporción (suman 1.000000)."),
        ("umbrales", "Arreglo de umbrales configurados para métricas."));

    Describir(document, "CrearVersionIcRequest",
        ("motivo", "Motivo o justificación del cambio de versión (obligatorio, máximo 500 caracteres)."),
        ("frecuencia", "Frecuencia del IC: Diaria, Mensual o Trimestral."),
        ("pesos", "Arreglo de { dimension, peso }. Cada peso es una proporción (0 a 1) y deben sumar exactamente 1.000000 con tolerancia 0.000001."),
        ("umbrales", "Arreglo de { codigoMetrica, limiteInferior, limiteSuperior, severidad (Informativa, Advertencia, Critica) }."));

    Describir(document, "PesoDimensionDto",
        ("dimension", "Dimensión: Rentabilidad, Liquidez, Solvencia o Crecimiento."),
        ("peso", "Ponderación en proporción decimal (ej. 0.35 para 35%)."));

    Describir(document, "UmbralMetricaDto",
        ("codigoMetrica", "Código de la métrica (ej. RNT-01, LIQ-01)."),
        ("limiteInferior", "Límite inferior del rango de alerta."),
        ("limiteSuperior", "Límite superior del rango de alerta."),
        ("severidad", "Nivel de alerta: Informativa, Advertencia o Critica."));

    Describir(document, "AuditoriaDto",
        ("idBitacora", "Identificador UUID del registro de auditoría."),
        ("accion", "Acción auditada (ej. usuario.actualizado, usuario.desactivado, exportacion, login.fallido)."),
        ("detalle", "Detalle informativo de la acción realizada."),
        ("fechaEvento", "Fecha y hora UTC en que ocurrió el evento."),
        ("idUsuario", "UUID del usuario ejecutor (null si la acción no fue autenticada)."));

    Describir(document, "ParametrosDto",
        ("pesos", "Pesos vigentes de las dimensiones en el cálculo del IC."),
        ("metricas", "Catálogo de métricas financieras con sus fórmulas y frecuencias."));

    Describir(document, "CrearMetricaCommand",
        ("codigo", "Código único, por ejemplo RNT-01."),
        ("nombre", "Nombre visible de la métrica."),
        ("formula", "Fórmula de cálculo."),
        ("frecuencia", "Diaria, Mensual o Trimestral."),
        ("idDimension", "UUID de la dimensión existente."));

    Describir(document, "EmisionPruebaResponse",
        ("secuencia", "Número de secuencia del estado emitido a los clientes de SignalR."),
        ("generadoEn", "Momento UTC en que se ensambló el mensaje de difusión."),
        ("entidades", "Cantidad total de entidades incluidas en el mensaje del ticker."));
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

public partial class Program { }
