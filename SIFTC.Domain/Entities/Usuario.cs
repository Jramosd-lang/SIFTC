using SIFTC.Domain.Common;
using SIFTC.Domain.Enums;

namespace SIFTC.Domain.Entities;

public class Usuario
{
    // 1. Propiedades limpias, sin inicialización en línea para no afectar a EF Core
    public Guid IdUsuario { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string PrimerNombre { get; private set; }
    public string PrimerApellido { get; private set; }
    public Rol Rol { get; private set; }
    public bool IsActive { get; private set; }

    // 2. Constructor privado requerido por EF Core para materializar datos (ultra rápido)
    private Usuario()
    {
        // Inicializamos en null! para suprimir las advertencias del compilador,
        // ya que EF Core se encargará de llenarlos inmediatamente al leer de PostgreSQL.
        Email = null!;
        PasswordHash = null!;
        PrimerNombre = null!;
        PrimerApellido = null!;
    }

    // 3. Constructor privado principal (Ahora exige el ID y el Estado)
    private Usuario(Guid idUsuario, string email, string passwordHash, string primerNombre, string primerApellido, Rol rol, bool isActive)
    {
        IdUsuario = idUsuario;
        Email = email;
        PasswordHash = passwordHash;
        PrimerNombre = primerNombre;
        PrimerApellido = primerApellido;
        Rol = rol;
        IsActive = isActive;
    }

    // 4. Factory Method: La única puerta de entrada válida para tu Handler
    public static Result<Usuario> Create(
        string email,
        string passwordHash,
        string primerNombre,
        string primerApellido,
        Rol rol)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Usuario>(new Error("usuario.email.required", "El correo electrónico es obligatorio.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<Usuario>(new Error("usuario.password.required", "El hash de la contraseña es obligatorio.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(primerNombre))
            return Result.Failure<Usuario>(new Error("usuario.primerNombre.required", "El primer nombre es obligatorio.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(primerApellido))
            return Result.Failure<Usuario>(new Error("usuario.primerApellido.required", "El primer apellido es obligatorio.", ErrorType.Validation));

        // Aquí es donde tomas el control. Generas el ID y se lo pasas al constructor.
        return Result.Success(new Usuario(
            Guid.CreateVersion7(), // <-- Se genera solo aquí, cuando nace un usuario nuevo
            email.Trim().ToLowerInvariant(),
            passwordHash,
            primerNombre.Trim(),
            primerApellido.Trim(),
            rol,
            true // <-- Por defecto, todo usuario nuevo nace activo
        ));
    }

    public Result<Usuario> Update(string primerNombre, string primerApellido, Rol rol)
    {
        if (string.IsNullOrWhiteSpace(primerNombre))
            return Result.Failure<Usuario>(new Error("usuario.primerNombre.required", "El primer nombre es obligatorio.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(primerApellido))
            return Result.Failure<Usuario>(new Error("usuario.primerApellido.required", "El primer apellido es obligatorio.", ErrorType.Validation));

        PrimerNombre = primerNombre.Trim();
        PrimerApellido = primerApellido.Trim();
        Rol = rol;

        return Result.Success(this);
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}