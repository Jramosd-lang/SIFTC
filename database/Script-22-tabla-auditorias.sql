-- =====================================================================
-- SIFTC - HU-BE-06: bitacora de auditoria (minima, decision D4)
-- Script-22: crea seguridad.auditorias si no existe.
-- Ejecutar como dueno de los objetos (postgres), despues de los scripts
-- de seguridad (usuarios y roles).
--
-- [POR QUE] Ningun script de este repositorio crea la tabla, pero los
--           modelos de SIFTC.API (.NET) y de backendFastApi (Python)
--           ya escriben y leen seguridad.auditorias. Ambos backends
--           comparten el esquema y el rol rol_backend.
--
-- Compatible con los modelos existentes:
--   .NET: AuditoriaConfiguration (SIFTC.Infrastructure)
--   Python: src/Models/AuditModel.py (backendFastApi)
--
-- Clasificacion (Matriz de permisos v1.1): accion y detalle = Interna.
-- Idempotente: se puede ejecutar mas de una vez.
-- =====================================================================
BEGIN;

CREATE SCHEMA IF NOT EXISTS seguridad;

CREATE TABLE IF NOT EXISTS seguridad.auditorias (
    id_bitacora UUID NOT NULL DEFAULT gen_random_uuid(),
    accion VARCHAR(40) NOT NULL,
    detalle VARCHAR(250) NOT NULL,
    fecha_evento TIMESTAMPTZ NOT NULL DEFAULT now(),
    id_usuario UUID,
    CONSTRAINT pk_auditorias PRIMARY KEY (id_bitacora),
    CONSTRAINT fk_auditorias_usuario
        FOREIGN KEY (id_usuario) REFERENCES seguridad.usuarios (id_usuario)
);

COMMENT ON TABLE seguridad.auditorias IS
    'Bitacora de eventos criticos (HU-BE-06). Sensibilidad Interna (D5).';
COMMENT ON COLUMN seguridad.auditorias.accion IS
    'Accion realizada, p. ej. exportacion. Maximo 40 caracteres.';
COMMENT ON COLUMN seguridad.auditorias.detalle IS
    'Detalle key=value; p. ej. recurso=/api/v1/exportar; tipo=ranking; formato=csv; filas=12; ip=...; estado=200; filtros=...
Maximo 250 caracteres. recurso=, ip= y estado= son filtrables desde la consulta de auditoria.';
COMMENT ON COLUMN seguridad.auditorias.id_usuario IS
    'Usuario del JWT que ejecuto la accion; NULL si no habia token valido.';

CREATE INDEX IF NOT EXISTS ix_auditorias_usuario
    ON seguridad.auditorias (id_usuario);
CREATE INDEX IF NOT EXISTS ix_auditorias_fecha
    ON seguridad.auditorias (fecha_evento);

-- El rol compartido por .NET y Python opera la bitacora en seguridad
-- (docs/INTEGRACION_BACKENDS_SPRINT2.md).
-- Matriz de permisos SIFTC v1.1: la bitacora es de solo lectura + escritura
-- de eventos. UPDATE y DELETE quedan prohibidos para que el registro sea
-- inmutable (nadie puede alterar ni borrar lo que ya se auditoria).
GRANT SELECT, INSERT ON seguridad.auditorias TO rol_backend;

COMMIT;
