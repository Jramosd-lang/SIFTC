using Moq;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;
using SIFTC.Application.UseCases.Usuarios.Commands;
using SIFTC.Application.UseCases.Usuarios.Commands.ActualizarUsuario;
using SIFTC.Application.UseCases.Usuarios.Commands.EliminarUsuario;
using SIFTC.Application.UseCases.Usuarios.Queries.ListarUsuarios;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;

namespace SIFTC.Tests;

/// <summary>
/// Gestión de usuarios: listado (solo activos), edición y baja lógica.
/// </summary>
public sealed class UsuarioHandlerTests
{
    private readonly Mock<IUsuarioRepository> _repositorio = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAuditoriaRepository> _auditoria = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();

    private static readonly Guid RolGerente = Guid.Parse("c43ddacf-b545-4190-b051-c8172e3026f0");
    private static readonly Guid RolAdministrador = Guid.Parse("179acd26-b52c-4e75-8c26-0b6b1b5737e4");

    private static Usuario CrearUsuario(
        string email = "ana@siftc.local",
        string nombre = "Ana",
        string apellido = "Pérez",
        Guid? idRol = null,
        bool activo = true)
    {
        var usuario = Usuario.Create(
            email,
            "hash-actual",
            nombre,
            apellido,
            idRol ?? RolGerente).Value;

        if (!activo)
        {
            usuario.Deactivate();
        }

        return usuario;
    }

    private ActualizarUsuarioCommandHandler CrearEditor() =>
        new(_repositorio.Object, _hasher.Object, _auditoria.Object);

    private EliminarUsuarioCommandHandler CrearEliminador() =>
        new(_repositorio.Object, _auditoria.Object);

    private static ActualizarUsuarioCommand Actualizacion(
        Guid id,
        string email = "ana@siftc.local",
        string? password = null) =>
        new(id, email, "Ana", "María", "Pérez", "Gómez", RolGerente, password);

    // ------------------------------------------------------------------
    // Alta (se mantiene el contrato existente)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Crear_normaliza_el_correo_y_guarda_el_hash()
    {
        _repositorio
            .Setup(r => r.ExistsByEmailAsync("nueva@siftc.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ObtenerNombreRolAsync(RolGerente, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Gerente / Directivo");
        _hasher.Setup(h => h.Hash("Clave123*")).Returns("hash-nuevo");

        var handler = new CrearUsuarioCommandHandler(_repositorio.Object, _hasher.Object);
        var result = await handler.Handle(
            new CrearUsuarioCommand("  Nueva@SIFTC.local  ", "Clave123*", "Nueva", "Usuario", RolGerente),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repositorio.Verify(
            r => r.AddAsync(
                It.Is<Usuario>(u => u.Email == "nueva@siftc.local" && u.PasswordHash == "hash-nuevo"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Crear_devuelve_422_si_el_rol_no_existe()
    {
        _repositorio
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ObtenerNombreRolAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var handler = new CrearUsuarioCommandHandler(_repositorio.Object, _hasher.Object);
        var result = await handler.Handle(
            new CrearUsuarioCommand("nueva@siftc.local", "Clave123*", "Nueva", "Usuario", RolGerente),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("usuario.rol.invalido", result.Error.Code);
    }

    // ------------------------------------------------------------------
    // Listado: el front solo recibe activos
    // ------------------------------------------------------------------

    [Fact]
    public async Task Listar_devuelve_solo_los_usuarios_que_entrega_el_repositorio()
    {
        _repositorio
            .Setup(r => r.ListarActivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CrearUsuario("ana@siftc.local"), CrearUsuario("luis@siftc.local", "Luis", "Gómez")]);

        var result = await new ListarUsuariosQueryHandler(_repositorio.Object)
            .Handle(new ListarUsuariosQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("ana@siftc.local", result.Value[0].Email);
        Assert.Equal("luis@siftc.local", result.Value[1].Email);
    }

    // ------------------------------------------------------------------
    // Edición
    // ------------------------------------------------------------------

    [Fact]
    public async Task Actualizar_devuelve_404_si_el_usuario_no_existe()
    {
        _repositorio
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await CrearEditor().Handle(Actualizacion(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        _repositorio.Verify(
            r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Actualizar_devuelve_409_si_el_correo_es_de_otro_usuario()
    {
        var usuario = CrearUsuario();
        _repositorio
            .Setup(r => r.GetByIdAsync(usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _repositorio
            .Setup(r => r.ExistsByEmailExceptoAsync("otro@siftc.local", usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CrearEditor().Handle(
            Actualizacion(usuario.IdUsuario, "otro@siftc.local"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("usuario.duplicado", result.Error.Code);
        _repositorio.Verify(
            r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Actualizar_devuelve_422_si_el_rol_no_existe()
    {
        var usuario = CrearUsuario();
        _repositorio
            .Setup(r => r.GetByIdAsync(usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _repositorio
            .Setup(r => r.ExistsByEmailExceptoAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ObtenerNombreRolAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await CrearEditor().Handle(
            Actualizacion(usuario.IdUsuario), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("usuario.rol.invalido", result.Error.Code);
    }

    [Fact]
    public async Task Actualizar_guarda_los_datos_y_la_nueva_contrasena()
    {
        var usuario = CrearUsuario();
        _repositorio
            .Setup(r => r.GetByIdAsync(usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _repositorio
            .Setup(r => r.ExistsByEmailExceptoAsync("nueva@siftc.local", usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ObtenerNombreRolAsync(RolGerente, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Gerente / Directivo");
        _hasher
            .Setup(h => h.Hash("NuevaClave1*"))
            .Returns("hash-nuevo");

        var result = await CrearEditor().Handle(
            Actualizacion(usuario.IdUsuario, "nueva@siftc.local", "NuevaClave1*"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("nueva@siftc.local", result.Value.Email);
        Assert.Equal("Gerente / Directivo", result.Value.Rol);
        Assert.Equal("María", result.Value.SegundoNombre);
        Assert.Equal("Gómez", result.Value.SegundoApellido);
        Assert.Equal("hash-nuevo", usuario.PasswordHash);
        _repositorio.Verify(
            r => r.UpdateAsync(usuario, It.IsAny<CancellationToken>()),
            Times.Once);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                It.Is<Auditoria>(x => x.Accion == "usuario.actualizado"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Actualizar_sin_password_conserva_el_hash_actual()
    {
        var usuario = CrearUsuario();
        _repositorio
            .Setup(r => r.GetByIdAsync(usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _repositorio
            .Setup(r => r.ExistsByEmailExceptoAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ObtenerNombreRolAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Gerente / Directivo");

        var result = await CrearEditor().Handle(
            Actualizacion(usuario.IdUsuario, password: ""), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("hash-actual", usuario.PasswordHash);
        _hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    // ------------------------------------------------------------------
    // Baja lógica
    // ------------------------------------------------------------------

    [Fact]
    public async Task Eliminar_devuelve_404_si_el_usuario_no_existe()
    {
        _repositorio
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await CrearEliminador().Handle(
            new EliminarUsuarioCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Eliminar_no_permite_que_un_administrador_se_desactive_a_si_mismo()
    {
        var id = Guid.NewGuid();

        var result = await CrearEliminador().Handle(
            new EliminarUsuarioCommand(id, id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("usuario.autoeliminar", result.Error.Code);
        _repositorio.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Eliminar_cambia_el_estado_a_inactivo_sin_borrar_la_fila()
    {
        var usuario = CrearUsuario();
        _repositorio
            .Setup(r => r.GetByIdAsync(usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _repositorio
            .Setup(r => r.ObtenerNombreRolAsync(RolGerente, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Gerente / Directivo");

        var result = await CrearEliminador().Handle(
            new EliminarUsuarioCommand(usuario.IdUsuario, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("inactivo", usuario.Estado);
        _repositorio.Verify(
            r => r.UpdateAsync(usuario, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(
            r => r.DeleteAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                It.Is<Auditoria>(x => x.Accion == "usuario.desactivado"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Eliminar_es_idempotente_si_ya_estaba_inactivo()
    {
        var usuario = CrearUsuario(activo: false);
        _repositorio
            .Setup(r => r.GetByIdAsync(usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var result = await CrearEliminador().Handle(
            new EliminarUsuarioCommand(usuario.IdUsuario, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("inactivo", usuario.Estado);
        _repositorio.Verify(
            r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Eliminar_no_deja_el_sistema_sin_administradores_activos()
    {
        var usuario = CrearUsuario(idRol: RolAdministrador);
        _repositorio
            .Setup(r => r.GetByIdAsync(usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        _repositorio
            .Setup(r => r.ObtenerNombreRolAsync(RolAdministrador, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Administrador");
        _repositorio
            .Setup(r => r.ContarActivosPorRolAsync(RolAdministrador, usuario.IdUsuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await CrearEliminador().Handle(
            new EliminarUsuarioCommand(usuario.IdUsuario, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("usuario.ultimo.administrador", result.Error.Code);
        Assert.Equal("activo", usuario.Estado);
        _repositorio.Verify(
            r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // Login: un usuario inactivo ya no entra
    // ------------------------------------------------------------------

    [Fact]
    public async Task Login_rechaza_a_un_usuario_inactivo()
    {
        var usuario = CrearUsuario(activo: false);
        _repositorio
            .Setup(r => r.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var handler = new AutenticarUsuarioCommandHandler(
            _repositorio.Object,
            _hasher.Object,
            _jwt.Object,
            _auditoria.Object);

        var result = await handler.Handle(
            new AutenticarUsuarioCommand(usuario.Email, "Siftc2026*"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _jwt.Verify(j => j.GenerateToken(It.IsAny<Usuario>()), Times.Never);
        _auditoria.Verify(
            a => a.RegistrarAsync(
                It.Is<Auditoria>(x => x.Accion == "login.fallido"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
