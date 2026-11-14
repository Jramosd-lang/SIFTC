CREATE TABLE seguridad.roles (
    id_rol UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre VARCHAR(30) NOT NULL UNIQUE,              -- antes VARCHAR(40); máx. actual 19
    descripcion VARCHAR(150),                        -- antes TEXT; máx. actual 60
    permisos_clave VARCHAR(60) NOT NULL              -- antes VARCHAR(100); máx. actual 39
);

CREATE TABLE seguridad.usuarios (
    id_usuario UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(120) NOT NULL UNIQUE,
    primer_nombre VARCHAR(30) NOT NULL,
    segundo_nombre VARCHAR(30),
    primer_apellido VARCHAR(30) NOT NULL,
    segundo_apellido VARCHAR(30),
    password_hash VARCHAR(100) NOT NULL,             -- se deja: el hash de ASP.NET Identity mide 84
    estado VARCHAR(10) NOT NULL DEFAULT 'activo'
        CHECK (estado IN ('activo', 'inactivo', 'bloqueado')),
    id_rol UUID NOT NULL REFERENCES seguridad.roles (id_rol)
);
