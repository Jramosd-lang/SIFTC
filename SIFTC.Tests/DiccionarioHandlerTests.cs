using System.Text;
using Moq;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Diccionario.Commands.CargarDiccionario;
using SIFTC.Application.UseCases.Diccionario.Queries.BuscarDiccionario;
using SIFTC.Application.UseCases.Diccionario.Queries.ObtenerFicha;
using SIFTC.Domain.Common;
using DiccionarioEntidad = SIFTC.Domain.Entities.Diccionario;

namespace SIFTC.Tests;

/// <summary>
/// HU-DD-08: endpoint de diccionario (carga, búsqueda, ficha y filtro por rol).
/// </summary>
public sealed class DiccionarioHandlerTests
{
    private readonly Mock<IDiccionarioRepository> _repositorio = new();

    private const string Encabezado =
        "codigo;tipo;nombre;definicion;sinonimos;formula;unidad;clasificacion";

    private const string FilaRoe =
        "RNT-01;metrica;ROE;Rentabilidad sobre el patrimonio;rentabilidad del patrimonio;utilidad_neta / patrimonio;%;Interno";

    private const string FilaSensible =
        "RNT-99;metrica;Margen bruto interno;Margen confidencial de la gerencia;;margen / ventas;%;Confidencial";

    private static Stream Csv(string contenido) =>
        new MemoryStream(Encoding.UTF8.GetBytes(contenido));

    private CargarDiccionarioCommandHandler CrearCargador() => new(_repositorio.Object);

    private static DiccionarioEntidad Fila(string clasificacion = "Interno") =>
        DiccionarioEntidad.Crear(
            "RNT-01", "metrica", "ROE", "Rentabilidad sobre el patrimonio",
            "rentabilidad del patrimonio", "utilidad_neta / patrimonio", "%",
            clasificacion).Value;

    // ------------------------------------------------------------------
    // CA 1: carga exitosa; archivo con columnas faltantes se rechaza
    // ------------------------------------------------------------------

    [Fact]
    public async Task CAC1_un_csv_valido_reemplaza_la_tabla_y_devuelve_los_registros()
    {
        _repositorio
            .Setup(r => r.ReemplazarTodoAsync(It.IsAny<IReadOnlyList<DiccionarioEntidad>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var csv = $"{Encabezado}\n{FilaRoe}\n{FilaSensible}";
        var result = await CrearCargador().Handle(
            new CargarDiccionarioCommand(Csv(csv)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Registros);
        _repositorio.Verify(
            r => r.ReemplazarTodoAsync(
                It.Is<IReadOnlyList<DiccionarioEntidad>>(l => l.Count == 2 && l[0].Codigo == "RNT-01"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CAC1_un_csv_con_columnas_en_distinto_orden_y_extras_se_procesa_correctamente()
    {
        _repositorio
            .Setup(r => r.ReemplazarTodoAsync(It.IsAny<IReadOnlyList<DiccionarioEntidad>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var encabezadoDesordenado = "columna_extra;clasificacion;unidad;formula;sinonimos;definicion;nombre;tipo;codigo";
        var filaDesordenada = "ignorado;Interno;%;utilidad_neta / patrimonio;rentabilidad del patrimonio;Rentabilidad sobre el patrimonio;ROE;metrica;RNT-01";
        var csv = $"{encabezadoDesordenado}\n{filaDesordenada}";

        var result = await CrearCargador().Handle(
            new CargarDiccionarioCommand(Csv(csv)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Registros);
        _repositorio.Verify(
            r => r.ReemplazarTodoAsync(
                It.Is<IReadOnlyList<DiccionarioEntidad>>(l =>
                    l.Count == 1 &&
                    l[0].Codigo == "RNT-01" &&
                    l[0].Tipo == "metrica" &&
                    l[0].Nombre == "ROE" &&
                    l[0].Clasificacion == "Interno"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CAC1_un_csv_sin_columnas_se_rechaza_sin_tocar_la_tabla()
    {
        // Falta la columna 'clasificacion' (y la de fórmula viene mal nombrada).
        var csv = "codigo;tipo;nombre;definicion;sinonimos;formula;unidad\nRNT-01;metrica;ROE;Def;;;%";

        var result = await CrearCargador().Handle(
            new CargarDiccionarioCommand(Csv(csv)), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("diccionario.columnas.faltan", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        _repositorio.Verify(
            r => r.ReemplazarTodoAsync(It.IsAny<IReadOnlyList<DiccionarioEntidad>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CAC1_una_fila_con_tipo_invalido_se_rechaza_con_el_numero_de_fila()
    {
        var csv = $"{Encabezado}\nRNT-01;indeterminado;ROE;Definicion;;;%";

        var result = await CrearCargador().Handle(
            new CargarDiccionarioCommand(Csv(csv)), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("diccionario.fila.invalida", result.Error.Code);
        Assert.Contains("Fila 2", result.Error.Message);
        _repositorio.Verify(
            r => r.ReemplazarTodoAsync(It.IsAny<IReadOnlyList<DiccionarioEntidad>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CAC1_un_codigo_repetido_se_rechaza_antes_de_escribir()
    {
        var csv = $"{Encabezado}\n{FilaRoe}\n{FilaRoe}";

        var result = await CrearCargador().Handle(
            new CargarDiccionarioCommand(Csv(csv)), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("diccionario.codigo.duplicado", result.Error.Code);
        _repositorio.Verify(
            r => r.ReemplazarTodoAsync(It.IsAny<IReadOnlyList<DiccionarioEntidad>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // CA 2: la búsqueda pasa los filtros y el límite al repositorio
    // ------------------------------------------------------------------

    [Fact]
    public async Task CAC2_la_busqueda_pasa_el_texto_el_tipo_y_el_limite_de_20()
    {
        _repositorio
            .Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Fila()]);

        var result = await new BuscarDiccionarioQueryHandler(_repositorio.Object).Handle(
            new BuscarDiccionarioQuery("roe", "METRICA", "Administrador"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("RNT-01", result.Value[0].Codigo);
        _repositorio.Verify(
            r => r.BuscarAsync("roe", "metrica", It.IsAny<IReadOnlyList<string>>(), 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CAC2_sin_texto_o_con_tipo_invalido_responde_422()
    {
        var handler = new BuscarDiccionarioQueryHandler(_repositorio.Object);

        var sinTexto = await handler.Handle(new BuscarDiccionarioQuery("  ", null, "Administrador"), CancellationToken.None);
        var tipoMalo = await handler.Handle(new BuscarDiccionarioQuery("roe", "otra", "Administrador"), CancellationToken.None);

        Assert.Equal("diccionario.buscar.required", sinTexto.Error.Code);
        Assert.Equal("diccionario.tipo.invalid", tipoMalo.Error.Code);
        _repositorio.Verify(
            r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // CA 3: ficha completa por RNT-01 o 404 si no existe
    // ------------------------------------------------------------------

    [Fact]
    public async Task CAC3_la_ficha_devuelve_los_sinonimos_como_arreglo()
    {
        _repositorio
            .Setup(r => r.ObtenerPorCodigoAsync("RNT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Fila());

        var result = await new ObtenerFichaQueryHandler(_repositorio.Object).Handle(
            new ObtenerFichaQuery("RNT-01", "Administrador"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ROE", result.Value.Nombre);
        Assert.Equal("utilidad_neta / patrimonio", result.Value.Formula);
        Assert.Equal(["rentabilidad del patrimonio"], result.Value.Sinonimos);
        Assert.Equal("Interno", result.Value.Clasificacion);
    }

    [Fact]
    public async Task CAC3_un_codigo_inexistente_responde_404()
    {
        _repositorio
            .Setup(r => r.ObtenerPorCodigoAsync("NO-EXISTE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiccionarioEntidad?)null);

        var result = await new ObtenerFichaQueryHandler(_repositorio.Object).Handle(
            new ObtenerFichaQuery("NO-EXISTE", "Administrador"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    // ------------------------------------------------------------------
    // CA 4: rol sin permiso no ve confidencial (mismo 404 que no existe)
    // ------------------------------------------------------------------

    [Fact]
    public async Task CAC4_el_gerente_no_recibe_lo_confidencial_y_el_analista_si()
    {
        var handler = new ObtenerFichaQueryHandler(_repositorio.Object);

        _repositorio
            .Setup(r => r.ObtenerPorCodigoAsync("RNT-99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Fila("Confidencial"));

        var gerente = await handler.Handle(new ObtenerFichaQuery("RNT-99", "Gerente / Directivo"), CancellationToken.None);
        var analista = await handler.Handle(new ObtenerFichaQuery("RNT-99", "Analista financiero"), CancellationToken.None);
        var sinSesion = await handler.Handle(new ObtenerFichaQuery("RNT-99", null), CancellationToken.None);

        Assert.True(gerente.IsFailure);
        Assert.Equal(ErrorType.NotFound, gerente.Error.Type);
        Assert.True(analista.IsSuccess);
        Assert.Equal("Confidencial", analista.Value.Clasificacion);
        Assert.True(sinSesion.IsFailure);
    }

    [Fact]
    public async Task CAC4_el_404_por_sin_permiso_es_identico_al_de_un_codigo_inexistente()
    {
        _repositorio
            .Setup(r => r.ObtenerPorCodigoAsync("RNT-99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Fila("Confidencial"));
        _repositorio
            .Setup(r => r.ObtenerPorCodigoAsync("NO-EXISTE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiccionarioEntidad?)null);

        var handler = new ObtenerFichaQueryHandler(_repositorio.Object);
        var sinPermiso = await handler.Handle(new ObtenerFichaQuery("RNT-99", "Gerente / Directivo"), CancellationToken.None);
        var inexistente = await handler.Handle(new ObtenerFichaQuery("NO-EXISTE", "Gerente / Directivo"), CancellationToken.None);

        // Anti-enumeración: mismo código y mismo mensaje en ambos casos.
        Assert.Equal(inexistente.Error.Code, sinPermiso.Error.Code);
        Assert.Equal(inexistente.Error.Message, sinPermiso.Error.Message);
    }

    [Fact]
    public async Task CAC4_la_busqueda_filtra_las_clasificaciones_segun_el_rol()
    {
        var handler = new BuscarDiccionarioQueryHandler(_repositorio.Object);
        IReadOnlyList<string>? visiblesGerente = null;
        IReadOnlyList<string>? visiblesAnalista = null;

        _repositorio
            .Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, IReadOnlyList<string>, int, CancellationToken>((_, _, v, _, _) => visiblesGerente ??= v)
            .ReturnsAsync([]);

        await handler.Handle(new BuscarDiccionarioQuery("algo", null, "Gerente / Directivo"), CancellationToken.None);

        _repositorio
            .Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, IReadOnlyList<string>, int, CancellationToken>((_, _, v, _, _) => visiblesAnalista = v)
            .ReturnsAsync([]);

        await handler.Handle(new BuscarDiccionarioQuery("algo", null, "Analista financiero"), CancellationToken.None);

        // Gerente: solo Público e Interno. Analista: además Confidencial.
        // Restringido nunca está, para ningún rol.
        Assert.Equal(["Público", "Interno"], visiblesGerente);
        Assert.Equal(["Público", "Interno", "Confidencial"], visiblesAnalista);
        Assert.DoesNotContain("Restringido", visiblesAnalista!);
    }
}
