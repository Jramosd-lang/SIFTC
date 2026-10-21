using SIFTC.Domain.Common;
namespace SIFTC.Domain.Entities;

public sealed class Usuario
{
    public Guid IdUsuario { get; private set; }
    public string Email { get; private set; } = null!;
    public string PrimerNombre { get; private set; } = null!;
    public string? SegundoNombre { get; private set; }
    public string PrimerApellido { get; private set; } = null!;
    public string? SegundoApellido { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public string Estado { get; private set; } = null!;
    public Guid IdRol { get; private set; }
    public RolSistema Rol { get; private set; } = null!;

    private Usuario() { }

    private Usuario(Guid idUsuario, string email, string passwordHash, string primerNombre, string primerApellido, Guid idRol)
    {
        IdUsuario = idUsuario;
        Email = email;
        PrimerNombre = primerNombre;
        PrimerApellido = primerApellido;
        PasswordHash = passwordHash;
        IdRol = idRol;
        Estado = "activo";
    }

    // 4. Factory Method: La única puerta de entrada válida para tu Handler
    public static Result<Usuario> Create(
        string email,
        string passwordHash,
        string primerNombre,
        string primerApellido,
        Guid idRol)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Usuario>(new Error("usuario.email.required", "El correo electrónico es obligatorio.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<Usuario>(new Error("usuario.password.required", "El hash de la contraseña es obligatorio.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(primerNombre) || string.IsNullOrWhiteSpace(primerApellido))
            return Result.Failure<Usuario>(new Error("usuario.nombre.required", "El primer nombre y el primer apellido son obligatorios.", ErrorType.Validation));
        if (idRol == Guid.Empty)
            return Result.Failure<Usuario>(new Error("usuario.rol.required", "El rol es obligatorio.", ErrorType.Validation));

        return Result.Success(new Usuario(
            Guid.CreateVersion7(),
            email.Trim().ToLowerInvariant(),
            passwordHash,
            primerNombre.Trim(),
            primerApellido.Trim(),
            idRol
        ));
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void Activate() => Estado = "activo";
    public void Deactivate() => Estado = "inactivo";
    public void Block() => Estado = "bloqueado";
}
