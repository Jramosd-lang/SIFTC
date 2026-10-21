using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Infrastructure.Authentication;
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

        // 3. Configuración de Autenticación
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
