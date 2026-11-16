using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SIFTC.Tests;

// API real apuntando a la base de prueba.
// Por defecto, el microservicio de Python apunta a un puerto cerrado: las pruebas normales nunca salen a internet.
// Para las pruebas externas: SIFTC_PYTHON_URL (con barra final) y SIFTC_PYTHON_TOKEN (token de servicio).
public sealed class SiftcApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string Secret = "clave-solo-para-pruebas-0123456789-abcdef";
    public const string Issuer = "siftc-tests";
    public const string Audience = "siftc-tests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var urlPython = Environment.GetEnvironmentVariable("SIFTC_PYTHON_URL");
        var tokenPython = Environment.GetEnvironmentVariable("SIFTC_PYTHON_TOKEN");
        var esExterno = !string.IsNullOrWhiteSpace(urlPython);

        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("JwtSettings:Secret", Secret);
        builder.UseSetting("JwtSettings:Issuer", Issuer);
        builder.UseSetting("JwtSettings:Audience", Audience);
        builder.UseSetting("JwtSettings:ExpiryMinutes", "15");
        builder.UseSetting("PythonBackend:BaseUrl", esExterno ? urlPython! : "http://127.0.0.1:9/");
        builder.UseSetting("PythonBackend:TimeoutSeconds", esExterno ? "120" : "5");
        if (!string.IsNullOrWhiteSpace(tokenPython))
        {
            builder.UseSetting("PythonBackend:ServiceToken", tokenPython);
        }
    }
}
