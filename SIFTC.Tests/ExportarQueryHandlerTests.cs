using System.Text;
using MediatR;
using Moq;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Application.UseCases.Entidades.Queries.ObtenerOhlc;
using SIFTC.Application.UseCases.Exportar;
using SIFTC.Application.UseCases.Exportar.Queries.ExportarDatos;
using SIFTC.Application.UseCases.Ranking;
using SIFTC.Application.UseCases.Ranking.Queries.ObtenerRanking;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;

namespace SIFTC.Tests;

public sealed class ExportarQueryHandlerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IAuditoriaRepository> _bitacora = new();
    private readonly Guid _usuarioId = Guid.Parse("98c097d9-a661-4c29-ae77-b7a41945548a");

    private static readonly IReadOnlyList<RankingItemDto> ItemsRanking =
    [
        new(1, 2, Guid.Parse("4ebbf629-0d2d-46ef-b233-93421e13eaee"), "Andina Energia S.A.", "Energia", 55.042m, -0.9555m),
        new(2, -1, Guid.Parse("4ebbf629-0d2d-46ef-b233-93421e13eaef"), "Hidro Norte S.A.", "Energia", 48.1m, 1.5m)
    ];

    private ExportarQueryHandler CrearHandler() => new(_mediator.Object, _bitacora.Object);

    private ExportarQuery Consulta(
        string? tipo = "ranking",
        string? formato = "csv",
        string? rol = "Administrador",
        Guid? idEntidad = null,
        string? sector = null,
        string? periodo = null) =>
        new(
            tipo,
            formato,
            sector,
            null,
            periodo,
            null,
            null,
            null,
            null,
            idEntidad,
            rol,
            _usuarioId,
            "Bearer token-de-prueba",
            "200.14.100.25");

    private void PrepararRanking()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<ObtenerRankingQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new RankingResponseDto("Mensual", ItemsRanking.Count, ItemsRanking)));
    }

    private static string[] LeerLineas(ExportFileDto archivo)
    {
        var texto = Encoding.UTF8.GetString(archivo.Contenido);
        Assert.StartsWith("\uFEFF", texto);
        return texto
            .TrimStart('\uFEFF')
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }

    // ------------------------------------------------------------------
    // CA 1: el archivo trae exactamente los datos filtrados
    // ------------------------------------------------------------------

    [Fact]
    public async Task CAC1_el_archivo_trae_exactamente_los_datos_filtrados()
    {
        PrepararRanking();

        var result = await CrearHandler().Handle(
            Consulta(sector: "Energia"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("text/csv; charset=utf-8", result.Value.ContentType);
        Assert.Equal(2, result.Value.TotalFilas);
        Assert.StartsWith("ranking_", result.Value.FileName);
        Assert.EndsWith(".csv", result.Value.FileName);

        var lineas = LeerLineas(result.Value);

        Assert.Equal(3, lineas.Length);
        Assert.Equal("posicion;cambioPosicion;entidadId;entidad;sector;ic;variacionPct", lineas[0]);
        Assert.Equal("1;2;4ebbf629-0d2d-46ef-b233-93421e13eaee;Andina Energia S.A.;Energia;55,042;-0,9555", lineas[1]);
        Assert.Equal("2;-1;4ebbf629-0d2d-46ef-b233-93421e13eaef;Hidro Norte S.A.;Energia;48,1;1,5", lineas[2]);
    }

    [Fact]
    public async Task CAC1_los_filtros_se_propagan_a_la_consulta_de_ranking()
    {
        PrepararRanking();

        var consulta = Consulta() with
        {
            Sector = "Energia",
            Dimension = "Rentabilidad",
            Periodo = "Mensual",
            Orden = "ic",
            Dir = "asc",
            Limit = 10,
            Offset = 5
        };

        var result = await CrearHandler().Handle(consulta, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mediator.Verify(
            m => m.Send(
                It.Is<ObtenerRankingQuery>(q =>
                    q.Sector == "Energia" &&
                    q.Dimension == "Rentabilidad" &&
                    q.Periodo == "Mensual" &&
                    q.Orden == "ic" &&
                    q.Dir == "asc" &&
                    q.Limit == 10 &&
                    q.Offset == 5 &&
                    q.Authorization == "Bearer token-de-prueba"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CAC1_la_serie_exporta_los_mismos_puntos_de_la_consulta_ohlc()
    {
        var idEntidad = Guid.Parse("4ebbf629-0d2d-46ef-b233-93421e13eaee");
        _mediator
            .Setup(m => m.Send(It.IsAny<ObtenerOhlcQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new OhlcResponseDto(
                idEntidad,
                "Andina Energia S.A.",
                "Mensual",
                [new OhlcPuntoDto(new DateOnly(2026, 9, 1), 55.042m, 55.042m, 55.042m, 55.042m, -0.531m, -0.9555m)])));

        var result = await CrearHandler().Handle(
            Consulta(tipo: "serie", periodo: "Mensual", idEntidad: idEntidad),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var lineas = LeerLineas(result.Value);

        Assert.Equal(2, lineas.Length);
        Assert.Equal("fecha;apertura;maximo;minimo;cierre;variacion;variacionPct", lineas[0]);
        Assert.Equal("2026-09-01;55,042;55,042;55,042;55,042;-0,531;-0,9555", lineas[1]);

        _mediator.Verify(
            m => m.Send(
                It.Is<ObtenerOhlcQuery>(q => q.IdEntidad == idEntidad && q.Periodo == "Mensual"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ------------------------------------------------------------------
    // CA 2: no se exportan datos sin permiso según el rol
    // ------------------------------------------------------------------

    [Fact]
    public async Task CAC2_el_rol_sin_permiso_no_exporta_ranking()
    {
        var result = await CrearHandler().Handle(
            Consulta(rol: "Usuario móvil"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("exportar.rol.forbidden", result.Error.Code);

        _mediator.Verify(
            m => m.Send(It.IsAny<ObtenerRankingQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _bitacora.Verify(
            b => b.RegistrarAsync(It.IsAny<Auditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CAC2_el_gerente_exporta_el_ranking_sin_posicion()
    {
        PrepararRanking();

        var result = await CrearHandler().Handle(
            Consulta(rol: "Gerente / Directivo"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var lineas = LeerLineas(result.Value);

        // posicion y cambioPosicion vacios para Gerente (HU-BE-02 / D1).
        Assert.StartsWith(";;", lineas[1]);
        Assert.StartsWith(";;", lineas[2]);
        Assert.Contains(";Andina Energia S.A.;", lineas[1]);
    }

    [Fact]
    public async Task CAC2_los_cinco_roles_de_ranking_pueden_exportar()
    {
        string[] roles = ["Administrador", "Data Steward", "Analista financiero", "Gerente / Directivo", "Auditor"];

        foreach (var rol in roles)
        {
            PrepararRanking();
            var result = await CrearHandler().Handle(Consulta(rol: rol), CancellationToken.None);
            Assert.True(result.IsSuccess, $"El rol {rol} debe poder exportar el ranking.");
        }
    }

    [Fact]
    public async Task CAC2_el_usuario_movil_exporta_series_porque_son_publicas()
    {
        var idEntidad = Guid.Parse("4ebbf629-0d2d-46ef-b233-93421e13eaee");
        _mediator
            .Setup(m => m.Send(It.IsAny<ObtenerOhlcQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new OhlcResponseDto(idEntidad, "Andina Energia S.A.", "Mensual", [])));

        var result = await CrearHandler().Handle(
            Consulta(tipo: "serie", rol: "Usuario móvil", idEntidad: idEntidad),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ------------------------------------------------------------------
    // CA 3: cada exportación queda registrada en la bitácora
    // ------------------------------------------------------------------

    [Fact]
    public async Task CAC3_cada_exportacion_queda_registrada_en_la_bitacora()
    {
        PrepararRanking();

        var result = await CrearHandler().Handle(
            Consulta(sector: "Energia"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _bitacora.Verify(
            b => b.RegistrarAsync(
                It.Is<Auditoria>(a =>
                    a.Accion == "exportacion" &&
                    a.Detalle!.StartsWith("recurso=/api/v1/exportar") &&
                    a.Detalle.Contains("tipo=ranking") &&
                    a.Detalle.Contains("formato=csv") &&
                    a.Detalle.Contains("filas=2") &&
                    a.Detalle.Contains("ip=200.14.100.25") &&
                    a.Detalle.Contains("filtros=sector=Energia") &&
                    a.Detalle.Length <= 250 &&
                    a.IdUsuario == _usuarioId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CAC3_si_la_bitacora_falla_no_se_entrega_el_archivo()
    {
        PrepararRanking();
        _bitacora
            .Setup(b => b.RegistrarAsync(It.IsAny<Auditoria>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("bitacora no disponible"));

        var result = await CrearHandler().Handle(Consulta(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Dependency, result.Error.Type);
        Assert.Equal("exportar.bitacora.error", result.Error.Code);
    }

    [Fact]
    public async Task CAC3_las_exportaciones_fallidas_no_se_registran()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<ObtenerRankingQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<RankingResponseDto>(
                new Error("ranking.python.no_disponible", "Microservicio no disponible.", ErrorType.Dependency)));

        var result = await CrearHandler().Handle(Consulta(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ranking.python.no_disponible", result.Error.Code);
        _bitacora.Verify(
            b => b.RegistrarAsync(It.IsAny<Auditoria>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // Validación de parámetros
    // ------------------------------------------------------------------

    [Fact]
    public async Task Rechaza_formato_distinto_de_csv()
    {
        var result = await CrearHandler().Handle(Consulta(formato: "excel"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("exportar.formato.invalid", result.Error.Code);
        _mediator.Verify(
            m => m.Send(It.IsAny<ObtenerRankingQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Formato_vacio_asume_csv()
    {
        PrepararRanking();

        var result = await CrearHandler().Handle(Consulta(formato: null), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Rechaza_tipo_invalido()
    {
        var result = await CrearHandler().Handle(Consulta(tipo: "pdf"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("exportar.tipo.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Serie_requiere_idEntidad()
    {
        var result = await CrearHandler().Handle(Consulta(tipo: "serie"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("exportar.entidad.required", result.Error.Code);
        _mediator.Verify(
            m => m.Send(It.IsAny<ObtenerOhlcQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
