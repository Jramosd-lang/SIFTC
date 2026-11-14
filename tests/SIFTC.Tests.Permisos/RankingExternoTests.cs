using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using SIFTC.Api.Common;
using SIFTC.Tests.Infrastructure;
using Xunit;

namespace SIFTC.Tests;

// Ranking contra el microservicio de Python real (Render). Pruebas EXTERNAS:
//   $env:SIFTC_PYTHON_URL = "https://backendfastapi-b3mt.onrender.com/"
//   $env:SIFTC_PYTHON_TOKEN = <token de servicio>
//   dotnet test tests\SIFTC.Tests\SIFTC.Tests.csproj --filter "Categoria=Externa"
// La API envía un único token de servicio a Python, así que Python ve siempre el mismo rol.
// Por eso aquí solo se verifica la regla del Gerente, no un contraste entre roles.
[Collection("Base de datos")]
[Trait("Categoria", "Externa")]
public sealed class RankingExternoTests(SiftcDatabaseFixture db)
{
    [Fact]
    public async Task Gerente_recibe_posicion_nula_en_el_ranking_real()
    {
        using var client = CrearClienteConRol(RolesSistema.Gerente);

        var response = await client.GetAsync("/api/v1/ranking");
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Esperado 200, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");

        using var doc = JsonDocument.Parse(cuerpo);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // Si no hay datos, la prueba no verifica nada: falla a propósito.
        Assert.NotEmpty(items);

        Assert.All(items, item =>
        {
            Assert.Equal(JsonValueKind.Null, item.GetProperty("posicion").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("cambioPosicion").ValueKind);
        });
    }

    private HttpClient CrearClienteConRol(string rol)
    {
        var client = db.ApiPostgres.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerarToken(rol));
        return client;
    }

    private static string GenerarToken(string rol)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SiftcApiFactory.Secret));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, DatosDePrueba.UsuarioPorRol[rol].ToString()),
            new Claim(ClaimTypes.Role, rol)
        };

        var token = new JwtSecurityToken(
            issuer: SiftcApiFactory.Issuer,
            audience: SiftcApiFactory.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
