using Npgsql;
using SIFTC.Api.Common;

namespace SIFTC.Tests.Infrastructure;

// Datos mínimos y fijos para las pruebas. Se insertan una vez, después de aplicar los scripts.
public static class DatosDePrueba
{
    public static readonly Guid IdSector = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    public static readonly Guid IdEntidad = Guid.Parse("00000000-0000-0000-0000-0000000000e1");

    public static readonly IReadOnlyDictionary<string, Guid> UsuarioPorRol = new Dictionary<string, Guid>
    {
        [RolesSistema.Administrador] = Guid.Parse("00000000-0000-0000-0000-0000000000b1"),
        [RolesSistema.DataSteward] = Guid.Parse("00000000-0000-0000-0000-0000000000b2"),
        [RolesSistema.AnalistaFinanciero] = Guid.Parse("00000000-0000-0000-0000-0000000000b3"),
        [RolesSistema.Gerente] = Guid.Parse("00000000-0000-0000-0000-0000000000b4"),
        [RolesSistema.Auditor] = Guid.Parse("00000000-0000-0000-0000-0000000000b5"),
        [RolesSistema.UsuarioMovil] = Guid.Parse("00000000-0000-0000-0000-0000000000b6")
    };

    public static async Task SembrarAsync(string connectionString)
    {
        await using var conexion = new NpgsqlConnection(connectionString);
        await conexion.OpenAsync();

        await EjecutarAsync(conexion,
            """
            INSERT INTO limpia.tbl_sectores (id_sector, nombre)
            VALUES (@id, 'Energía')
            ON CONFLICT (nombre) DO NOTHING;
            """,
            ("id", IdSector));

        await EjecutarAsync(conexion,
            """
            INSERT INTO limpia.tbl_entidades (id_entidad, ticker, nombre, id_sector)
            VALUES (@id, 'ENT001', 'Andina Energía S.A.', @sector)
            ON CONFLICT (ticker) DO NOTHING;
            """,
            ("id", IdEntidad),
            ("sector", IdSector));

        foreach (var (rol, idUsuario) in UsuarioPorRol)
        {
            await EjecutarAsync(conexion,
                """
                INSERT INTO seguridad.usuarios
                    (id_usuario, email, primer_nombre, primer_apellido, password_hash, estado, id_rol)
                SELECT @id, @email, 'Usuario', 'Prueba', 'hash-de-prueba', 'activo', r.id_rol
                FROM seguridad.roles r
                WHERE r.nombre = @rol
                ON CONFLICT (id_usuario) DO NOTHING;
                """,
                ("id", idUsuario),
                ("email", $"{idUsuario}@prueba.local"),
                ("rol", rol));
        }
    }

    private static async Task EjecutarAsync(
        NpgsqlConnection conexion,
        string sql,
        params (string Nombre, object Valor)[] parametros)
    {
        await using var comando = new NpgsqlCommand(sql, conexion);
        foreach (var (nombre, valor) in parametros)
        {
            comando.Parameters.AddWithValue(nombre, valor);
        }

        await comando.ExecuteNonQueryAsync();
    }
}
