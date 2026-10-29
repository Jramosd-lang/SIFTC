using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Integrations;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Infrastructure.Authentication;
using SIFTC.Infrastructure.Integrations;
using SIFTC.Infrastructure.Persistence;
using SIFTC.Infrastructure.Persistence.Repositories;

namespace SIFTC.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configuración de Base de Datos (PostgreSQL)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // 2. Registro de Repositorios (Scoped = uno por cada petición HTTP)
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IEntidadRepository, EntidadRepository>();
        services.AddScoped<IMetricaRepository, MetricaRepository>();
        services.AddScoped<IMetricaQueryRepository, MetricaQueryRepository>();

        // 3. Microservicio Python (ranking y validación de matriz)
        var pythonBaseUrl = configuration["PythonBackend:BaseUrl"] ?? "https://backendfastapi-b3mt.onrender.com/";
        var pythonTimeout = configuration.GetValue<int?>("PythonBackend:TimeoutSeconds") ?? 90;

        services.AddHttpClient<IPythonBackendClient, PythonBackendClient>(client =>
        {
            client.BaseAddress = new Uri(pythonBaseUrl.EndsWith('/') ? pythonBaseUrl : $"{pythonBaseUrl}/");
            client.Timeout = TimeSpan.FromSeconds(pythonTimeout);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // 4. Configuración de Autenticación
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Secret), "JwtSettings:Secret es obligatorio.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer), "JwtSettings:Issuer es obligatorio.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), "JwtSettings:Audience es obligatorio.")
            .Validate(settings => settings.ExpiryMinutes > 0, "JwtSettings:ExpiryMinutes debe ser mayor que cero.")
            .ValidateOnStart();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
