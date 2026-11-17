using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SIFTC.Application.Common.Interfaces.Notificaciones;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Application.UseCases.Notificaciones.Commands.CrearRegla;
using SIFTC.Application.UseCases.Notificaciones.Commands.EliminarRegla;
using SIFTC.Application.UseCases.Notificaciones.Commands.RegistrarToken;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;

namespace SIFTC.Tests;

/// <summary>
/// HU-AD-10 / HU-MO-05: notificaciones (reglas CA2, envío CA1, reintentos CA3).
/// </summary>
public sealed class NotificacionHandlerTests
{
    private readonly Mock<INotificacionRepository> _repositorio = new();
    private readonly Mock<ICorreoNotificador> _correo = new();
    private readonly Mock<IPushNotificador> _push = new();
    private readonly Mock<IAuditoriaRepository> _auditoria = new();

    private static readonly Guid Usuario = Guid.NewGuid();
    private static readonly Guid Entidad = Guid.NewGuid();

    private static NotificacionRegla Regla(
        string canal = "push",
        Guid? idEntidad = null,
        decimal? umbral = null,
        Guid? idUsuario = null) =>
        NotificacionRegla.Crear(idUsuario ?? Usuario, canal, idEntidad, umbral).Value;

    private static NotificacionEnvio Envio(
        string canal = "correo",
        int maxIntentos = 3) =>
        NotificacionEnvio.Crear(
            Usuario,
            null,
            canal,
            "Cambio relevante",
            "La entidad cambió.",
            maxIntentos).Value;

    private ProcesadorEnviosNotificacion CrearProcesador() =>
        new(
            _repositorio.Object,
            _correo.Object,
            _push.Object,
            _auditoria.Object,
            Options.Create(new NotificacionesOptions { ReintentoSegundos = 0 }),
            TimeProvider.System,
            NullLogger<ProcesadorEnviosNotificacion>.Instance);

    // ------------------------------------------------------------------
    // CA2: reglas
    // ------------------------------------------------------------------

    [Fact]
    public async Task CA2_crear_regla_valida_guarda_canal_entidad_y_umbral()
    {
        _repositorio
            .Setup(r => r.ObtenerNombreEntidadAsync(Entidad, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ACME");
        _repositorio
            .Setup(r => r.ObtenerReglaPorClaveAsync(Usuario, "push", Entidad, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificacionRegla?)null);

        var result = await new CrearReglaCommandHandler(_repositorio.Object).Handle(
            new CrearReglaCommand(Usuario, "push", Entidad, 2.5m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("push", result.Value.Canal);
        Assert.Equal(Entidad, result.Value.IdEntidad);
        Assert.Equal(2.5m, result.Value.Umbral);
        _repositorio.Verify(
            r => r.CrearReglaAsync(
                It.Is<NotificacionRegla>(x => x.IdUsuario == Usuario && x.Umbral == 2.5m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("sms")]
    [InlineData("")]
    [InlineData(null)]
    public async Task CA2_canal_invalido_devuelve_422(string? canal)
    {
        var result = await new CrearReglaCommandHandler(_repositorio.Object).Handle(
            new CrearReglaCommand(Usuario, canal, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notificacion.canal.invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        _repositorio.Verify(
            r => r.CrearReglaAsync(It.IsAny<NotificacionRegla>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CA2_umbral_negativo_devuelve_422()
    {
        var result = await new CrearReglaCommandHandler(_repositorio.Object).Handle(
            new CrearReglaCommand(Usuario, "correo", null, -1m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notificacion.umbral.invalid", result.Error.Code);
    }

    [Fact]
    public async Task CA2_regla_duplicada_devuelve_409()
    {
        var existente = Regla("push", Entidad, 1m);
        _repositorio
            .Setup(r => r.ObtenerNombreEntidadAsync(Entidad, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ACME");
        _repositorio
            .Setup(r => r.ObtenerReglaPorClaveAsync(Usuario, "push", Entidad, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var result = await new CrearReglaCommandHandler(_repositorio.Object).Handle(
            new CrearReglaCommand(Usuario, "push", Entidad, 3m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notificacion.regla.duplicada", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        _repositorio.Verify(
            r => r.CrearReglaAsync(It.IsAny<NotificacionRegla>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CA2_regla_desactivada_se_reactiva_en_vez_de_duplicarse()
    {
        var existente = Regla("push", Entidad, 1m);
        existente.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerNombreEntidadAsync(Entidad, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ACME");
        _repositorio
            .Setup(r => r.ObtenerReglaPorClaveAsync(Usuario, "push", Entidad, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var result = await new CrearReglaCommandHandler(_repositorio.Object).Handle(
            new CrearReglaCommand(Usuario, "push", Entidad, 4m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existente.IdRegla, result.Value.IdRegla);
        Assert.True(result.Value.Activa);
        Assert.Equal(4m, result.Value.Umbral);
        _repositorio.Verify(
            r => r.ActualizarReglaAsync(existente, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(
            r => r.CrearReglaAsync(It.IsAny<NotificacionRegla>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CA2_eliminar_regla_ajena_devuelve_404()
    {
        var ajena = Regla(idUsuario: Guid.NewGuid());
        _repositorio
            .Setup(r => r.ObtenerReglaAsync(ajena.IdRegla, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ajena);

        var result = await new EliminarReglaCommandHandler(_repositorio.Object).Handle(
            new EliminarReglaCommand(Usuario, ajena.IdRegla),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notificacion.regla.no.encontrada", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        _repositorio.Verify(
            r => r.ActualizarReglaAsync(It.IsAny<NotificacionRegla>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CA2_eliminar_regla_propia_la_desactiva()
    {
        var propia = Regla();
        _repositorio
            .Setup(r => r.ObtenerReglaAsync(propia.IdRegla, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propia);

        var result = await new EliminarReglaCommandHandler(_repositorio.Object).Handle(
            new EliminarReglaCommand(Usuario, propia.IdRegla),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(propia.Activa);
        _repositorio.Verify(
            r => r.ActualizarReglaAsync(propia, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ------------------------------------------------------------------
    // Tokens FCM
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("desktop")]
    [InlineData("")]
    public async Task Token_con_plataforma_invalida_devuelve_422(string plataforma)
    {
        var result = await new RegistrarTokenCommandHandler(_repositorio.Object).Handle(
            new RegistrarTokenCommand(Usuario, plataforma, "token-123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notificacion.token.plataforma.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Token_vacio_devuelve_422()
    {
        var result = await new RegistrarTokenCommandHandler(_repositorio.Object).Handle(
            new RegistrarTokenCommand(Usuario, "web", "  "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("notificacion.token.required", result.Error.Code);
    }

    [Fact]
    public async Task Token_valido_se_registra_sin_devolver_el_valor()
    {
        var result = await new RegistrarTokenCommandHandler(_repositorio.Object).Handle(
            new RegistrarTokenCommand(Usuario, "web", "token-123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("web", result.Value.Plataforma);
        Assert.True(result.Value.Activo);
        _repositorio.Verify(
            r => r.RegistrarTokenAsync(
                It.Is<NotificacionToken>(t => t.TokenFcm == "token-123" && t.IdUsuario == Usuario),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ------------------------------------------------------------------
    // CA2: el generador evalúa canal, entidad y umbral
    // ------------------------------------------------------------------

    [Fact]
    public async Task CA2_generador_encola_solo_las_reglas_que_cumplen_entidad_y_umbral()
    {
        var reglas = new List<NotificacionRegla>
        {
            Regla("push", Entidad, 2m),               // cumple (3.2 >= 2)
            Regla("correo", Guid.NewGuid(), null),    // otra entidad: no
            Regla("ambos", null, null)                // todas y cualquier cambio: 2 filas
        };

        _repositorio
            .Setup(r => r.ListarReglasActivasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reglas);

        List<NotificacionEnvio>? encolados = null;
        _repositorio
            .Setup(r => r.EncolarAsync(It.IsAny<IReadOnlyList<NotificacionEnvio>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<NotificacionEnvio>, CancellationToken>((e, _) => encolados = e.ToList())
            .Returns(Task.CompletedTask);

        var total = await new GeneradorNotificaciones(_repositorio.Object).EncolarPorEventoAsync(
            [new EventoNotificacionDto(Entidad, "ACME", 3.2m)],
            CancellationToken.None);

        Assert.Equal(3, total);
        Assert.NotNull(encolados);
        Assert.Equal(2, encolados!.Count(e => e.Canal == "push"));
        Assert.Single(encolados!, e => e.Canal == "correo");
        Assert.All(encolados!, e => Assert.Contains("ACME", e.Cuerpo));
    }

    [Fact]
    public async Task CA2_generador_no_encola_si_no_se_supera_el_umbral()
    {
        _repositorio
            .Setup(r => r.ListarReglasActivasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Regla("push", Entidad, 5m)]);

        var total = await new GeneradorNotificaciones(_repositorio.Object).EncolarPorEventoAsync(
            [new EventoNotificacionDto(Entidad, "ACME", 2m)],
            CancellationToken.None);

        Assert.Equal(0, total);
        _repositorio.Verify(
            r => r.EncolarAsync(It.IsAny<IReadOnlyList<NotificacionEnvio>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // CA1: envío correcto
    // ------------------------------------------------------------------

    [Fact]
    public async Task CA1_correo_exitoso_marca_enviado_y_audita()
    {
        var envio = Envio();
        _repositorio
            .Setup(r => r.TomarPendientesAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([envio]);
        _repositorio
            .Setup(r => r.ObtenerCorreoAsync(Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync("usuario@prueba.local");

        var procesados = await CrearProcesador().ProcesarPendientesAsync();

        Assert.Equal(1, procesados);
        Assert.Equal("enviado", envio.Estado);
        Assert.NotNull(envio.FechaEnvio);
        Assert.Equal(0, envio.Intentos);
        _correo.Verify(
            c => c.EnviarAsync("usuario@prueba.local", envio.Asunto, envio.Cuerpo, It.IsAny<CancellationToken>()),
            Times.Once);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                It.Is<Auditoria>(x => x.Accion == "notificacion.enviada"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CA1_push_exitoso_marca_token_en_uso()
    {
        var envio = Envio("push");
        var token = NotificacionToken.Crear(Usuario, "web", "token-123").Value;

        _repositorio
            .Setup(r => r.TomarPendientesAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([envio]);
        _repositorio
            .Setup(r => r.ObtenerTokensActivosAsync(Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);

        await CrearProcesador().ProcesarPendientesAsync();

        Assert.Equal("enviado", envio.Estado);
        _push.Verify(
            p => p.EnviarAsync("token-123", envio.Asunto, envio.Cuerpo, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(
            r => r.MarcarTokenUsoAsync(token.IdToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ------------------------------------------------------------------
    // CA3: reintentos y fallos
    // ------------------------------------------------------------------

    [Fact]
    public async Task CA3_fallo_transitorio_suma_intento_y_sigue_pendiente()
    {
        var envio = Envio(maxIntentos: 3);
        _repositorio
            .Setup(r => r.TomarPendientesAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([envio]);
        _repositorio
            .Setup(r => r.ObtenerCorreoAsync(Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync("usuario@prueba.local");
        _correo
            .Setup(c => c.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP no disponible"));

        await CrearProcesador().ProcesarPendientesAsync();

        Assert.Equal("pendiente", envio.Estado);
        Assert.Equal(1, envio.Intentos);
        Assert.Contains("SMTP no disponible", envio.UltimoError);
        Assert.NotNull(envio.FechaUltimoIntento);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                It.Is<Auditoria>(x => x.Accion == "notificacion.reintento"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CA3_al_agotar_max_intentos_queda_fallido()
    {
        var envio = Envio(maxIntentos: 1);
        _repositorio
            .Setup(r => r.TomarPendientesAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([envio]);
        _repositorio
            .Setup(r => r.ObtenerCorreoAsync(Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync("usuario@prueba.local");
        _correo
            .Setup(c => c.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP rechazó el correo"));

        await CrearProcesador().ProcesarPendientesAsync();

        Assert.Equal("fallido", envio.Estado);
        Assert.Equal(1, envio.Intentos);
        Assert.Equal(1, envio.MaxIntentos);
        Assert.Contains("SMTP rechazó el correo", envio.UltimoError);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                It.Is<Auditoria>(x => x.Accion == "notificacion.fallida"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CA3_token_unregistered_se_desactiva_y_el_envio_falla()
    {
        var envio = Envio("push");
        var token = NotificacionToken.Crear(Usuario, "android", "token-viejo").Value;

        _repositorio
            .Setup(r => r.TomarPendientesAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([envio]);
        _repositorio
            .Setup(r => r.ObtenerTokensActivosAsync(Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync([token]);
        _push
            .Setup(p => p.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TokenPushInvalidoException("UNREGISTERED"));

        await CrearProcesador().ProcesarPendientesAsync();

        Assert.Equal("pendiente", envio.Estado);
        Assert.Equal(1, envio.Intentos);
        _repositorio.Verify(
            r => r.DesactivarTokenAsync("token-viejo", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CA3_sin_tokens_activos_registra_el_fallo()
    {
        var envio = Envio("push");
        _repositorio
            .Setup(r => r.TomarPendientesAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([envio]);
        _repositorio
            .Setup(r => r.ObtenerTokensActivosAsync(Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await CrearProcesador().ProcesarPendientesAsync();

        Assert.Equal(1, envio.Intentos);
        Assert.Contains("tokens push", envio.UltimoError);
    }
}
