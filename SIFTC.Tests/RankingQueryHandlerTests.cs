using Moq;
using SIFTC.Application.Common.Interfaces.Integrations;
using SIFTC.Application.UseCases.Ranking;
using SIFTC.Application.UseCases.Ranking.Queries.ObtenerRanking;
using SIFTC.Domain.Common;
using Xunit;

namespace SIFTC.Tests;

public sealed class RankingQueryHandlerTests
{
    private readonly Mock<IPythonBackendClient> _client = new();

    private ObtenerRankingQueryHandler CrearHandler() => new(_client.Object);

    [Fact]
    public async Task Handle_periodo_valido_normaliza_y_llama_microservicio()
    {
        var expectedDto = new RankingResponseDto("Mensual", 1, [
            new(1, 0, Guid.NewGuid(), "Entidad Prueba", "Financiero", 75.5m, 1.2m)
        ]);

        _client
            .Setup(c => c.GetRankingAsync(It.IsAny<RankingQuery>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var query = new ObtenerRankingQuery(
            Sector: "Financiero",
            Dimension: "Rentabilidad",
            Periodo: "Mensual",
            Orden: "ic",
            Dir: "desc",
            Limit: 50,
            Offset: 0,
            Authorization: "Bearer token-test");

        var result = await CrearHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDto, result.Value);
        _client.Verify(c => c.GetRankingAsync(
            It.Is<RankingQuery>(q =>
                q.Sector == "Financiero" &&
                q.Dimension == "Rentabilidad" &&
                q.Periodo == "Mensual" &&
                q.Orden == "ic" &&
                q.Dir == "desc" &&
                q.Limit == 50 &&
                q.Offset == 0),
            "Bearer token-test",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("diaria")]
    [InlineData("diario")]
    [InlineData("Diaria")]
    [InlineData("mensual")]
    [InlineData("Mensual")]
    [InlineData("trimestral")]
    [InlineData("Trimestral")]
    public async Task Handle_acepta_variantes_de_periodo(string periodo)
    {
        _client
            .Setup(c => c.GetRankingAsync(It.IsAny<RankingQuery>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new RankingResponseDto(periodo, 0, [])));

        var query = new ObtenerRankingQuery(null, null, periodo, null, null, null, null, null);
        var result = await CrearHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_periodo_invalido_devuelve_error()
    {
        var query = new ObtenerRankingQuery(null, null, "anual", null, null, null, null, null);
        var result = await CrearHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ranking.periodo.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_dimension_invalida_devuelve_error()
    {
        var query = new ObtenerRankingQuery(null, "Inexistente", null, null, null, null, null, null);
        var result = await CrearHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ranking.dimension.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_limit_invalido_devuelve_error()
    {
        var query = new ObtenerRankingQuery(null, null, null, null, null, 250, null, null);
        var result = await CrearHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ranking.limit.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_propaga_error_del_microservicio()
    {
        _client
            .Setup(c => c.GetRankingAsync(It.IsAny<RankingQuery>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<RankingResponseDto>(
                new Error("ranking.upstream_error", "El microservicio de ranking no está disponible.", ErrorType.Dependency)));

        var query = new ObtenerRankingQuery(null, null, null, null, null, null, null, null);
        var result = await CrearHandler().Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ranking.upstream_error", result.Error.Code);
        Assert.Equal(ErrorType.Dependency, result.Error.Type);
    }
}
