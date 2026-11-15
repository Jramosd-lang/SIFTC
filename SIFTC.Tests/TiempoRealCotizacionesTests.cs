using System.Reflection;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.Common.Interfaces.Realtime;
using SIFTC.Application.UseCases.Aprobaciones.Commands.AprobarPublicacion;
using SIFTC.Application.UseCases.Aprobaciones.Events;
using SIFTC.Application.UseCases.TiempoReal;
using SIFTC.Application.UseCases.TiempoReal.Commands.DifundirCotizaciones;
using SIFTC.Application.UseCases.TiempoReal.EventHandlers;
using SIFTC.Application.UseCases.TiempoReal.Queries.ObtenerEstadoCotizaciones;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;

namespace SIFTC.Tests;

// HU-BE-04: cotizaciones en tiempo real.
public sealed class TiempoRealCotizacionesTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 15, 14, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UltimaPublicacion = new(2026, 11, 14, 22, 5, 10, 123, TimeSpan.Zero);

    private readonly Mock<IPublicacionRepository> _publicaciones = new();
    private readonly Mock<IEntidadRepository> _entidades = new();
    private readonly Mock<ICotizacionesNotificador> _notificador = new();

    public TiempoRealCotizacionesTests()
    {
        _publicaciones
            .Setup(r => r.ObtenerFechaUltimaPublicacionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UltimaPublicacion);

        _entidades
            .Setup(r => r.ListarTodasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CrearEntidad("ENT001", "Andina Energia S.A.", "Energia", 55.042m, -0.52m, -0.9555m),
                CrearEntidad("ENT002", "Hidro Norte S.A.", "Energia", null, null, null)
            ]);
    }

    private EstadoCotizacionesLector CrearLector() =>
        new(_publicaciones.Object, _entidades.Object, new TiempoFijo(Ahora));

    [Fact]
    public async Task El_estado_usa_la_ultima_publicacion_como_secuencia_y_trae_todas_las_entidades()
    {
        var estado = await CrearLector().LeerAsync(MotivoActualizacion.LotePublicado);

        Assert.Equal(UltimaPublicacion.ToUnixTimeMilliseconds(), estado.Secuencia);
        Assert.Equal(MotivoActualizacion.LotePublicado, estado.Motivo);
        Assert.Equal(Ahora, estado.GeneradoEn);
        Assert.Equal(["ENT001", "ENT002"], estado.Entidades.Select(e => e.Codigo));
        Assert.Equal(55.042m, estado.Entidades[0].Ic);
        Assert.Null(estado.Entidades[1].Ic);
    }

    [Fact]
    public async Task Sin_publicaciones_la_secuencia_es_cero()
    {
        _publicaciones
            .Setup(r => r.ObtenerFechaUltimaPublicacionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTimeOffset?)null);

        var estado = await CrearLector().LeerAsync(MotivoActualizacion.EstadoInicial);

        Assert.Equal(0, estado.Secuencia);
    }

    [Fact]
    public async Task El_mismo_estado_conserva_la_secuencia_para_que_el_cliente_descarte_duplicados()
    {
        var lector = CrearLector();

        var inicial = await lector.LeerAsync(MotivoActualizacion.EstadoInicial);
        var prueba = await lector.LeerAsync(MotivoActualizacion.Prueba);

        Assert.Equal(inicial.Secuencia, prueba.Secuencia);
    }

    [Fact]
    public async Task La_consulta_del_hub_devuelve_el_estado_inicial_sin_difundir()
    {
        var handler = new ObtenerEstadoCotizacionesQueryHandler(CrearLector());

        var result = await handler.Handle(new ObtenerEstadoCotizacionesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MotivoActualizacion.EstadoInicial, result.Value.Motivo);
        _notificador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Al_publicar_un_lote_se_difunde_el_estado_a_todos_los_clientes()
    {
        var handler = new DifundirAlPublicarLoteHandler(
            CrearLector(),
            _notificador.Object,
            NullLogger<DifundirAlPublicarLoteHandler>.Instance);

        await handler.Handle(new LotePublicadoEvent(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        _notificador.Verify(n => n.DifundirAsync(
            It.Is<ActualizacionCotizacionesDto>(a =>
                a.Motivo == MotivoActualizacion.LotePublicado &&
                a.Entidades.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Si_falla_la_difusion_no_se_propaga_el_error_a_la_aprobacion()
    {
        _notificador
            .Setup(n => n.DifundirAsync(It.IsAny<ActualizacionCotizacionesDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("hub caído"));

        var handler = new DifundirAlPublicarLoteHandler(
            CrearLector(),
            _notificador.Object,
            NullLogger<DifundirAlPublicarLoteHandler>.Instance);

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(new LotePublicadoEvent(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task La_emision_de_prueba_difunde_con_motivo_prueba()
    {
        var handler = new DifundirCotizacionesCommandHandler(CrearLector(), _notificador.Object);

        var result = await handler.Handle(new DifundirCotizacionesCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MotivoActualizacion.Prueba, result.Value.Motivo);
        _notificador.Verify(n => n.DifundirAsync(
            It.Is<ActualizacionCotizacionesDto>(a => a.Motivo == MotivoActualizacion.Prueba),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aprobar_con_exito_publica_el_evento_del_lote()
    {
        var idLote = Guid.NewGuid();
        var idAprobador = Guid.NewGuid();
        var publisher = new Mock<IPublisher>();
        _publicaciones
            .Setup(r => r.AprobarAsync(idLote, idAprobador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var handler = new AprobarPublicacionCommandHandler(_publicaciones.Object, publisher.Object);
        var result = await handler.Handle(new AprobarPublicacionCommand(idLote, idAprobador), CancellationToken.None);

        Assert.True(result.IsSuccess);
        publisher.Verify(p => p.Publish(
            new LotePublicadoEvent(idLote, idAprobador),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aprobar_con_error_no_publica_el_evento()
    {
        var publisher = new Mock<IPublisher>();
        _publicaciones
            .Setup(r => r.AprobarAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("aprobacion.conflict", "Ya decidido.", ErrorType.Conflict)));

        var handler = new AprobarPublicacionCommandHandler(_publicaciones.Object, publisher.Object);
        var result = await handler.Handle(new AprobarPublicacionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Aprobar_sin_lote_no_llama_al_repositorio_ni_publica()
    {
        var publisher = new Mock<IPublisher>();

        var handler = new AprobarPublicacionCommandHandler(_publicaciones.Object, publisher.Object);
        var result = await handler.Handle(new AprobarPublicacionCommand(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        _publicaciones.Verify(r => r.AprobarAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.VerifyNoOtherCalls();
    }

    // EntidadCotizacion es de solo lectura (la llena EF desde la vista): se arma por reflexión solo en pruebas.
    private static EntidadCotizacion CrearEntidad(
        string ticker,
        string nombre,
        string sector,
        decimal? ic,
        decimal? variacion,
        decimal? variacionPct)
    {
        var entidad = (EntidadCotizacion)Activator.CreateInstance(typeof(EntidadCotizacion), nonPublic: true)!;
        Asignar(entidad, nameof(EntidadCotizacion.IdEntidad), Guid.NewGuid());
        Asignar(entidad, nameof(EntidadCotizacion.Ticker), ticker);
        Asignar(entidad, nameof(EntidadCotizacion.Nombre), nombre);
        Asignar(entidad, nameof(EntidadCotizacion.Sector), sector);
        Asignar(entidad, nameof(EntidadCotizacion.Ic), ic);
        Asignar(entidad, nameof(EntidadCotizacion.Variacion), variacion);
        Asignar(entidad, nameof(EntidadCotizacion.VariacionPct), variacionPct);
        return entidad;
    }

    private static void Asignar(object objeto, string propiedad, object? valor) =>
        objeto.GetType()
            .GetProperty(propiedad, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(objeto, valor);

    private sealed class TiempoFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
