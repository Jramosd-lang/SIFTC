using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SIFTC.Tests;

// API real apuntando a la base de prueba. Sin repositorios falsos.
public sealed class SiftcApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string Secret = "clave-solo-para-pruebas-0123456789-abcdef";
    public const string Issuer = "siftc-tests";
    public const string Audience = "siftc-tests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("JwtSettings:Secret", Secret);
        builder.UseSetting("JwtSettings:Issuer", Issuer);
        builder.UseSetting("JwtSettings:Audience", Audience);
        builder.UseSetting("JwtSettings:ExpiryMinutes", "15");
    }
}
