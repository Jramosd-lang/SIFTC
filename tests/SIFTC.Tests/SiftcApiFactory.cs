using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SIFTC.Tests;

// API real apuntando a la base de prueba.
// El microservicio de Python se redirige a un puerto cerrado: las pruebas nunca salen a internet
// y, si una prueba llega a la llamada, falla rápido con 503.
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
        builder.UseSetting("PythonBackend:BaseUrl", "http://127.0.0.1:9/");
        builder.UseSetting("PythonBackend:TimeoutSeconds", "5");
    }
}
