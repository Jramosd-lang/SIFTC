-- =====================================================================
-- SIFTC - HU-BE-05: flujo de aprobación por lote
-- Script-19: versión corregida para convivir con Script-18-diccionario.
-- Ejecutar como dueño de los objetos (postgres), después de Script-17
-- y Script-18-ajuste-diccionario.
--
-- [CAMBIO] Este script reemplaza a Script-18-flujo-aprobacion.sql.
-- [POR QUÉ] Ya existe Script-18-ajuste-diccionario.sql en el repositorio;
--           duplicar el número dificulta saber el orden de ejecución.
--
-- Resumen de correcciones:
--   1. Los pendientes ya no se otorgan a rol_powerbi.
--   2. Los lotes rechazados/publicados sin la bandera de publicación
--      no pueden escribir directamente en fct_*.
--   3. publicar_lote exige aprobador no nulo, rol Analista financiero,
--      lote exitoso, registros válidos y pendientes reales.
--   4. Se refuerza el CHECK de publicaciones publicadas: exigen usuario.
--   5. Se corrige NUMERIC de pend_valores_metrica a (20,6).
--   6. Se agrega una bandeja de notificación para rechazos.
--   7. Se documenta el caso de lotes fallidos y reprocesos.
-- =====================================================================
BEGIN;

-- ---------------------------------------------------------------------
-- 1. Estado de publicación de cada lote
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS analitica.publicaciones_lote (
    id_lote UUID PRIMARY KEY REFERENCES cruda.lotes_carga (id_lote),
    estado VARCHAR(10) NOT NULL DEFAULT 'pendiente'
        CHECK (estado IN ('pendiente', 'publicado', 'rechazado')),
    id_usuario_ejecutor UUID NOT NULL REFERENCES seguridad.usuarios (id_usuario),
    id_usuario_decision UUID REFERENCES seguridad.usuarios (id_usuario),
    fecha_decision TIMESTAMPTZ,
    comentario VARCHAR(500),
    -- [CAMBIO] Un lote publicado ahora exige usuario de decisión.
    -- [POR QUÉ] Evita publicar con id_usuario_decision = NULL y perder
    --           la trazabilidad de quién aprobó.
    CONSTRAINT ck_publicaciones_decision CHECK (
        (estado = 'pendiente'
            AND id_usuario_decision IS NULL
            AND fecha_decision IS NULL
            AND comentario IS NULL)
        OR (estado = 'publicado'
            AND id_usuario_decision IS NOT NULL
            AND fecha_decision IS NOT NULL)
        OR (estado = 'rechazado'
            AND id_usuario_decision IS NOT NULL
            AND fecha_decision IS NOT NULL
            AND btrim(comentario) <> '')
    )
);

-- [CAMBIO] El índice original era redundante sobre id_lote y solo cubría
-- pendientes. Se reemplaza por un índice útil para la bandeja.
-- [POR QUÉ] La API filtrará por estado y ordenará por fecha.
CREATE INDEX IF NOT EXISTS ix_publicaciones_estado
    ON analitica.publicaciones_lote (estado, fecha_decision);

-- ---------------------------------------------------------------------
-- 2. Regularizar publicaciones existentes antes de reforzar el CHECK
-- ---------------------------------------------------------------------
-- [CAMBIO] Se completan usuarios/fechas de publicaciones históricas.
-- [POR QUÉ] El CHECK nuevo exige id_usuario_decision para 'publicado'.
--           En una migración no existe un aprobador humano; se usa el
--           ejecutor como responsable histórico y se deja comentario.
UPDATE analitica.publicaciones_lote
SET id_usuario_decision = COALESCE(id_usuario_decision, id_usuario_ejecutor),
    fecha_decision = COALESCE(fecha_decision, now()),
    comentario = COALESCE(comentario, 'Publicado por migración Script-19')
WHERE estado = 'publicado'
  AND (id_usuario_decision IS NULL OR fecha_decision IS NULL);

UPDATE analitica.publicaciones_lote
SET id_usuario_decision = COALESCE(id_usuario_decision, id_usuario_ejecutor),
    fecha_decision = COALESCE(fecha_decision, now()),
    comentario = COALESCE(NULLIF(btrim(comentario), ''), 'Rechazado por migración Script-19')
WHERE estado = 'rechazado'
  AND (id_usuario_decision IS NULL OR fecha_decision IS NULL OR comentario IS NULL OR btrim(comentario) = '');

-- [CAMBIO] Si la tabla venía del script anterior, se reemplaza el CHECK.
-- [POR QUÉ] El constraint anterior permitía estado 'publicado' sin usuario.
ALTER TABLE analitica.publicaciones_lote
    DROP CONSTRAINT IF EXISTS ck_publicaciones_decision;
ALTER TABLE analitica.publicaciones_lote
    ADD CONSTRAINT ck_publicaciones_decision CHECK (
        (estado = 'pendiente'
            AND id_usuario_decision IS NULL
            AND fecha_decision IS NULL
            AND comentario IS NULL)
        OR (estado = 'publicado'
            AND id_usuario_decision IS NOT NULL
            AND fecha_decision IS NOT NULL)
        OR (estado = 'rechazado'
            AND id_usuario_decision IS NOT NULL
            AND fecha_decision IS NOT NULL
            AND btrim(comentario) <> '')
    );

-- [CAMBIO] Los lotes históricos fallidos no se publican.
-- [POR QUÉ] Un lote fallido no tiene cálculo válido; publicarlo dejaría
--           una aprobación sin datos o con datos parciales.
INSERT INTO analitica.publicaciones_lote
    (id_lote, estado, id_usuario_ejecutor, id_usuario_decision, fecha_decision, comentario)
SELECT
    l.id_lote,
    CASE WHEN l.estado_carga = 'Fallido' THEN 'rechazado' ELSE 'publicado' END,
    l.id_usuario,
    l.id_usuario,
    COALESCE(l.fecha_carga, now()),
    CASE
        WHEN l.estado_carga = 'Fallido' THEN 'Migración Script-19: lote fallido'
        ELSE 'Migración Script-19: lote histórico'
    END
FROM cruda.lotes_carga l
ON CONFLICT (id_lote) DO NOTHING;

-- ---------------------------------------------------------------------
-- 3. Resultados de lotes sin aprobar
-- ---------------------------------------------------------------------
-- [CAMBIO] valor pasa de NUMERIC(14,6) a NUMERIC(20,6).
-- [POR QUÉ] Script-18-ajuste-diccionario amplió fct_valores_metrica.valor
--           a NUMERIC(20,6) según el diccionario. El pendiente debe tener
--           la misma capacidad para no rechazar valores válidos.
CREATE TABLE IF NOT EXISTS analitica.pend_valores_metrica (
    id_valor UUID NOT NULL,
    periodo DATE NOT NULL,
    valor NUMERIC(20,6),
    valor_normalizado NUMERIC(9,8),
    fecha_carga TIMESTAMPTZ NOT NULL,
    id_lote UUID NOT NULL REFERENCES analitica.publicaciones_lote (id_lote),
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad),
    id_metrica UUID NOT NULL REFERENCES analitica.dim_metrica (id_metrica),
    id_registro_origen UUID REFERENCES limpia.tbl_datos_financieros (id_dato),
    PRIMARY KEY (id_lote, id_entidad, id_metrica, periodo),
    -- [CAMBIO] Se replican los CHECK de la tabla publicada.
    CHECK (valor_normalizado IS NULL OR valor_normalizado BETWEEN 0 AND 1)
);

-- [CAMBIO] Si la tabla ya existía con la precisión anterior, se amplía.
ALTER TABLE analitica.pend_valores_metrica
    ALTER COLUMN valor TYPE NUMERIC(20,6);

CREATE TABLE IF NOT EXISTS analitica.pend_cotizaciones (
    id_cotizacion UUID NOT NULL,
    fecha DATE NOT NULL,
    apertura NUMERIC(6,3) NOT NULL,
    maximo NUMERIC(6,3) NOT NULL,
    minimo NUMERIC(6,3) NOT NULL,
    cierre NUMERIC(6,3) NOT NULL,
    variacion_pct NUMERIC(8,4),
    volumen_actualizaciones INT NOT NULL,
    fecha_carga TIMESTAMPTZ NOT NULL,
    id_lote UUID NOT NULL REFERENCES analitica.publicaciones_lote (id_lote),
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad),
    PRIMARY KEY (id_lote, id_entidad, fecha),
    -- [CAMBIO] Mismo CHECK que fct_cotizaciones.
    CHECK (minimo <= LEAST(apertura, cierre) AND maximo >= GREATEST(apertura, cierre))
);

CREATE TABLE IF NOT EXISTS analitica.pend_indices_competitividad (
    id_indice UUID NOT NULL,
    fecha_carga TIMESTAMPTZ NOT NULL,
    ic NUMERIC(6,3) NOT NULL,
    id_lote UUID NOT NULL REFERENCES analitica.publicaciones_lote (id_lote),
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad),
    PRIMARY KEY (id_lote, id_entidad),
    CHECK (ic BETWEEN 0 AND 100)
);

-- ---------------------------------------------------------------------
-- 4. Bandeja de notificación de rechazos
-- ---------------------------------------------------------------------
-- [CAMBIO] Se agrega un evento persistente para el rechazo.
-- [POR QUÉ] La HU exige notificar al equipo de Análisis de datos.
--           SQL no envía correos; deja el evento para que la API/worker
--           lo consuma. No se expone a Power BI.
CREATE TABLE IF NOT EXISTS analitica.notificaciones_aprobacion (
    id_notificacion UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    id_lote UUID NOT NULL REFERENCES analitica.publicaciones_lote (id_lote),
    tipo VARCHAR(20) NOT NULL CHECK (tipo IN ('rechazo', 'publicacion')),
    destinatario_rol VARCHAR(40) NOT NULL DEFAULT 'Analista financiero',
    mensaje VARCHAR(500) NOT NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT now(),
    enviada BOOLEAN NOT NULL DEFAULT false
);

CREATE INDEX IF NOT EXISTS ix_notificaciones_pendientes
    ON analitica.notificaciones_aprobacion (fecha_creacion)
    WHERE NOT enviada;

-- ---------------------------------------------------------------------
-- 5. Registro de publicación y estado del lote
-- ---------------------------------------------------------------------
-- [CAMBIO] El trigger solo crea publicación para lotes exitosos.
-- [POR QUÉ] Un lote fallido no debe aparecer como aprobable.
DROP TRIGGER IF EXISTS tr_lote_pendiente ON cruda.lotes_carga;

CREATE OR REPLACE FUNCTION analitica.registrar_publicacion_lote()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
BEGIN
    IF NEW.estado_carga IS DISTINCT FROM 'Exitoso' THEN
        RETURN NEW;
    END IF;

    INSERT INTO analitica.publicaciones_lote (id_lote, id_usuario_ejecutor)
    VALUES (NEW.id_lote, NEW.id_usuario)
    ON CONFLICT (id_lote) DO NOTHING;

    RETURN NEW;
END;
$$;

CREATE TRIGGER tr_lote_pendiente
    AFTER INSERT ON cruda.lotes_carga
    FOR EACH ROW
    WHEN (NEW.estado_carga = 'Exitoso')
    EXECUTE FUNCTION analitica.registrar_publicacion_lote();

-- [CAMBIO] Se agrega una lectura explícita de estado y se elimina el
-- comportamiento "sin publicación = permitir escritura".
-- [POR QUÉ] Si no existe publicación, la escritura debe fallar; no debe
--           asumirse que el lote está publicado.
CREATE OR REPLACE FUNCTION analitica.estado_publicacion(p_id_lote UUID)
RETURNS VARCHAR(10)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
DECLARE
    v_estado VARCHAR(10);
BEGIN
    SELECT estado INTO v_estado
    FROM analitica.publicaciones_lote
    WHERE id_lote = p_id_lote
    FOR SHARE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'El lote % no tiene publicación registrada.', p_id_lote
            USING ERRCODE = 'P0002';
    END IF;

    RETURN v_estado;
END;
$$;

-- [CAMBIO] Se conserva por compatibilidad, pero ahora falla si no existe
-- publicación y usa estado_publicacion.
CREATE OR REPLACE FUNCTION analitica.lote_pendiente(p_id_lote UUID)
RETURNS boolean
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
BEGIN
    RETURN analitica.estado_publicacion(p_id_lote) = 'pendiente';
END;
$$;

-- ---------------------------------------------------------------------
-- 6. Desvío de escrituras del ETL
-- ---------------------------------------------------------------------
-- [CAMBIO] Se distingue entre:
--   pendiente  -> se desvía a pend_*.
--   publicado  -> solo se permite durante publicar_lote (bandera).
--   rechazado  -> se bloquea.
-- [POR QUÉ] El script anterior dejaba pasar cualquier estado distinto de
--           'pendiente'. Un lote rechazado podía publicarse al reprocesarlo.
CREATE OR REPLACE FUNCTION analitica.desviar_valor_metrica()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
DECLARE
    v_estado VARCHAR(10);
BEGIN
    v_estado := analitica.estado_publicacion(NEW.id_lote);

    IF v_estado = 'pendiente' THEN
        -- Se conserva para que las cotizaciones sin id_lote sepan a qué
        -- lote pertenecen. Es una limitación conocida del esquema actual.
        PERFORM set_config('siftc.lote_en_proceso', NEW.id_lote::text, true);

        INSERT INTO analitica.pend_valores_metrica (
            id_valor, periodo, valor, valor_normalizado, fecha_carga,
            id_lote, id_entidad, id_metrica, id_registro_origen)
        VALUES (
            NEW.id_valor, NEW.periodo, NEW.valor, NEW.valor_normalizado, NEW.fecha_carga,
            NEW.id_lote, NEW.id_entidad, NEW.id_metrica, NEW.id_registro_origen)
        ON CONFLICT (id_lote, id_entidad, id_metrica, periodo) DO UPDATE SET
            valor = EXCLUDED.valor,
            valor_normalizado = EXCLUDED.valor_normalizado,
            fecha_carga = EXCLUDED.fecha_carga,
            id_registro_origen = EXCLUDED.id_registro_origen;

        RETURN NULL;
    END IF;

    IF v_estado = 'publicado'
       AND coalesce(current_setting('siftc.publicando', true), 'false') = 'true' THEN
        RETURN NEW;
    END IF;

    RAISE EXCEPTION 'El lote % está % y no admite escrituras directas.', NEW.id_lote, v_estado
        USING ERRCODE = 'SI409';
END;
$$;

CREATE OR REPLACE FUNCTION analitica.desviar_cotizacion()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
DECLARE
    v_lote UUID := nullif(current_setting('siftc.lote_en_proceso', true), '')::uuid;
    v_estado VARCHAR(10);
BEGIN
    -- [CAMBIO] Sin lote no se permite escribir; antes se publicaba directo.
    -- [POR QUÉ] fct_cotizaciones no tiene id_lote y el fallback silencioso
    --           permitía saltarse la aprobación.
    IF v_lote IS NULL THEN
        RAISE EXCEPTION 'No se pudo determinar el lote de la cotización.'
            USING ERRCODE = 'SI409';
    END IF;

    v_estado := analitica.estado_publicacion(v_lote);

    IF v_estado = 'pendiente' THEN
        INSERT INTO analitica.pend_cotizaciones (
            id_cotizacion, fecha, apertura, maximo, minimo, cierre, variacion_pct,
            volumen_actualizaciones, fecha_carga, id_lote, id_entidad)
        VALUES (
            NEW.id_cotizacion, NEW.fecha, NEW.apertura, NEW.maximo, NEW.minimo, NEW.cierre,
            NEW.variacion_pct, NEW.volumen_actualizaciones, NEW.fecha_carga, v_lote, NEW.id_entidad)
        ON CONFLICT (id_lote, id_entidad, fecha) DO UPDATE SET
            apertura = EXCLUDED.apertura,
            maximo = EXCLUDED.maximo,
            minimo = EXCLUDED.minimo,
            cierre = EXCLUDED.cierre,
            variacion_pct = EXCLUDED.variacion_pct,
            volumen_actualizaciones = EXCLUDED.volumen_actualizaciones,
            fecha_carga = EXCLUDED.fecha_carga;

        RETURN NULL;
    END IF;

    IF v_estado = 'publicado'
       AND coalesce(current_setting('siftc.publicando', true), 'false') = 'true' THEN
        RETURN NEW;
    END IF;

    RAISE EXCEPTION 'El lote % está % y no admite cotizaciones directas.', v_lote, v_estado
        USING ERRCODE = 'SI409';
END;
$$;

CREATE OR REPLACE FUNCTION analitica.desviar_indice()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
DECLARE
    v_estado VARCHAR(10);
BEGIN
    v_estado := analitica.estado_publicacion(NEW.id_lote);

    IF v_estado = 'pendiente' THEN
        INSERT INTO analitica.pend_indices_competitividad (id_indice, fecha_carga, ic, id_lote, id_entidad)
        VALUES (NEW.id_indice, NEW.fecha_carga, NEW.ic, NEW.id_lote, NEW.id_entidad)
        ON CONFLICT (id_lote, id_entidad) DO UPDATE SET
            ic = EXCLUDED.ic,
            fecha_carga = EXCLUDED.fecha_carga;

        RETURN NULL;
    END IF;

    IF v_estado = 'publicado'
       AND coalesce(current_setting('siftc.publicando', true), 'false') = 'true' THEN
        RETURN NEW;
    END IF;

    RAISE EXCEPTION 'El lote % está % y no admite índices directos.', NEW.id_lote, v_estado
        USING ERRCODE = 'SI409';
END;
$$;

DROP TRIGGER IF EXISTS tr_desviar_valor ON analitica.fct_valores_metrica;
CREATE TRIGGER tr_desviar_valor
    BEFORE INSERT ON analitica.fct_valores_metrica
    FOR EACH ROW EXECUTE FUNCTION analitica.desviar_valor_metrica();

DROP TRIGGER IF EXISTS tr_desviar_cotizacion ON analitica.fct_cotizaciones;
CREATE TRIGGER tr_desviar_cotizacion
    BEFORE INSERT ON analitica.fct_cotizaciones
    FOR EACH ROW EXECUTE FUNCTION analitica.desviar_cotizacion();

DROP TRIGGER IF EXISTS tr_desviar_indice ON analitica.fct_indices_competitividad;
CREATE TRIGGER tr_desviar_indice
    BEFORE INSERT ON analitica.fct_indices_competitividad
    FOR EACH ROW EXECUTE FUNCTION analitica.desviar_indice();

-- ---------------------------------------------------------------------
-- 7. Aprobar
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION analitica.publicar_lote(p_id_lote UUID, p_id_aprobador UUID)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
DECLARE
    v_estado VARCHAR(10);
    v_ejecutor UUID;
    v_estado_carga VARCHAR(32);
    v_registros_validos INT;
    v_tiene_pendientes BOOLEAN;
BEGIN
    -- [CAMBIO] Validaciones que faltaban.
    -- [POR QUÉ] Evitan publicar con aprobador nulo, con rol incorrecto,
    --           con lote fallido o sin datos pendientes.
    IF p_id_aprobador IS NULL THEN
        RAISE EXCEPTION 'El aprobador es obligatorio.' USING ERRCODE = 'SI422';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM seguridad.usuarios u
        JOIN seguridad.roles r ON r.id_rol = u.id_rol
        WHERE u.id_usuario = p_id_aprobador
          AND u.estado = 'activo'
          AND r.nombre = 'Analista financiero'
    ) THEN
        RAISE EXCEPTION 'Solo un Analista financiero activo puede aprobar.'
            USING ERRCODE = 'SI403';
    END IF;

    SELECT estado, id_usuario_ejecutor
    INTO v_estado, v_ejecutor
    FROM analitica.publicaciones_lote
    WHERE id_lote = p_id_lote
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'El lote % no existe.', p_id_lote USING ERRCODE = 'P0002';
    END IF;

    IF v_estado <> 'pendiente' THEN
        RAISE EXCEPTION 'El lote % ya está %.', p_id_lote, v_estado USING ERRCODE = 'SI409';
    END IF;

    IF v_ejecutor = p_id_aprobador THEN
        RAISE EXCEPTION 'Quien ejecuta un cálculo no puede aprobarlo.'
            USING ERRCODE = 'SI403';
    END IF;

    SELECT estado_carga, registros_validos
    INTO v_estado_carga, v_registros_validos
    FROM cruda.lotes_carga
    WHERE id_lote = p_id_lote;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'El lote % no existe en cruda.', p_id_lote USING ERRCODE = 'P0002';
    END IF;

    IF v_estado_carga IS DISTINCT FROM 'Exitoso' THEN
        RAISE EXCEPTION 'El lote % no está exitoso (estado: %).', p_id_lote, v_estado_carga
            USING ERRCODE = 'SI409';
    END IF;

    IF v_registros_validos <= 0 THEN
        RAISE EXCEPTION 'El lote % no tiene registros válidos.', p_id_lote
            USING ERRCODE = 'SI422';
    END IF;

    SELECT EXISTS (
        SELECT 1 FROM analitica.pend_valores_metrica WHERE id_lote = p_id_lote
        UNION ALL
        SELECT 1 FROM analitica.pend_cotizaciones WHERE id_lote = p_id_lote
        UNION ALL
        SELECT 1 FROM analitica.pend_indices_competitividad WHERE id_lote = p_id_lote
    ) INTO v_tiene_pendientes;

    IF NOT v_tiene_pendientes THEN
        RAISE EXCEPTION 'El lote % no tiene resultados pendientes.', p_id_lote
            USING ERRCODE = 'SI409';
    END IF;

    UPDATE analitica.publicaciones_lote
    SET estado = 'publicado',
        id_usuario_decision = p_id_aprobador,
        fecha_decision = now(),
        comentario = NULL
    WHERE id_lote = p_id_lote;

    -- [CAMBIO] Bandera transaccional para que los triggers permitan la
    -- copia desde pend_* a fct_* y bloqueen cualquier otra escritura.
    -- [POR QUÉ] Antes bastaba con que el estado fuera 'publicado' para
    --           permitir escrituras, incluso desde un reproceso del ETL.
    PERFORM set_config('siftc.publicando', 'true', true);
    PERFORM set_config('siftc.lote_en_proceso', p_id_lote::text, true);

    INSERT INTO analitica.fct_valores_metrica (
        id_valor, periodo, valor, valor_normalizado, fecha_carga,
        id_lote, id_entidad, id_metrica, id_registro_origen)
    SELECT id_valor, periodo, valor, valor_normalizado, fecha_carga,
           id_lote, id_entidad, id_metrica, id_registro_origen
    FROM analitica.pend_valores_metrica
    WHERE id_lote = p_id_lote
    ON CONFLICT (id_entidad, id_metrica, periodo) DO UPDATE SET
        valor = EXCLUDED.valor,
        valor_normalizado = EXCLUDED.valor_normalizado,
        fecha_carga = EXCLUDED.fecha_carga,
        id_lote = EXCLUDED.id_lote,
        id_registro_origen = EXCLUDED.id_registro_origen;

    INSERT INTO analitica.fct_cotizaciones (
        id_cotizacion, fecha, apertura, maximo, minimo, cierre, variacion_pct,
        volumen_actualizaciones, fecha_carga, id_entidad)
    SELECT id_cotizacion, fecha, apertura, maximo, minimo, cierre, variacion_pct,
           volumen_actualizaciones, fecha_carga, id_entidad
    FROM analitica.pend_cotizaciones
    WHERE id_lote = p_id_lote
    ON CONFLICT (id_entidad, fecha) DO UPDATE SET
        apertura = EXCLUDED.apertura,
        maximo = EXCLUDED.maximo,
        minimo = EXCLUDED.minimo,
        cierre = EXCLUDED.cierre,
        variacion_pct = EXCLUDED.variacion_pct,
        volumen_actualizaciones = EXCLUDED.volumen_actualizaciones,
        fecha_carga = EXCLUDED.fecha_carga;

    INSERT INTO analitica.fct_indices_competitividad (id_indice, fecha_carga, ic, id_lote, id_entidad)
    SELECT id_indice, fecha_carga, ic, id_lote, id_entidad
    FROM analitica.pend_indices_competitividad
    WHERE id_lote = p_id_lote
    ON CONFLICT (id_entidad, id_lote) DO UPDATE SET
        ic = EXCLUDED.ic,
        fecha_carga = EXCLUDED.fecha_carga;

    PERFORM set_config('siftc.publicando', 'false', true);
    PERFORM set_config('siftc.lote_en_proceso', '', true);
END;
$$;

-- ---------------------------------------------------------------------
-- 8. Rechazar
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION analitica.rechazar_lote(
    p_id_lote UUID,
    p_id_usuario UUID,
    p_comentario VARCHAR
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
DECLARE
    v_estado VARCHAR(10);
    v_mensaje VARCHAR(500);
BEGIN
    IF p_id_usuario IS NULL THEN
        RAISE EXCEPTION 'El usuario que rechaza es obligatorio.' USING ERRCODE = 'SI422';
    END IF;

    IF p_comentario IS NULL OR btrim(p_comentario) = '' THEN
        RAISE EXCEPTION 'El rechazo exige un comentario.' USING ERRCODE = 'SI422';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM seguridad.usuarios u
        JOIN seguridad.roles r ON r.id_rol = u.id_rol
        WHERE u.id_usuario = p_id_usuario
          AND u.estado = 'activo'
          AND r.nombre = 'Analista financiero'
    ) THEN
        RAISE EXCEPTION 'Solo un Analista financiero activo puede rechazar.'
            USING ERRCODE = 'SI403';
    END IF;

    SELECT estado INTO v_estado
    FROM analitica.publicaciones_lote
    WHERE id_lote = p_id_lote
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'El lote % no existe.', p_id_lote USING ERRCODE = 'P0002';
    END IF;

    IF v_estado <> 'pendiente' THEN
        RAISE EXCEPTION 'El lote % ya está %.', p_id_lote, v_estado USING ERRCODE = 'SI409';
    END IF;

    v_mensaje := btrim(p_comentario);

    UPDATE analitica.publicaciones_lote
    SET estado = 'rechazado',
        id_usuario_decision = p_id_usuario,
        fecha_decision = now(),
        comentario = v_mensaje
    WHERE id_lote = p_id_lote;

    -- [CAMBIO] Se registra el evento de rechazo para que la API/worker
    -- notifique al equipo de Análisis de datos.
    -- [POR QUÉ] La HU exige notificación; SQL solo deja el evento.
    INSERT INTO analitica.notificaciones_aprobacion (
        id_lote, tipo, destinatario_rol, mensaje)
    VALUES (
        p_id_lote, 'rechazo', 'Analista financiero', v_mensaje);
END;
$$;

-- ---------------------------------------------------------------------
-- 9. Permisos
-- ---------------------------------------------------------------------
-- [CAMBIO] rol_powerbi ya NO recibe SELECT sobre pendientes ni sobre
-- publicaciones en trámite.
-- [POR QUÉ] CA 1: lo pendiente no debe ser visible en canales.
GRANT SELECT ON
    analitica.publicaciones_lote,
    analitica.pend_valores_metrica,
    analitica.pend_cotizaciones,
    analitica.pend_indices_competitividad,
    analitica.notificaciones_aprobacion
TO rol_backend;

GRANT ALL PRIVILEGES ON
    analitica.publicaciones_lote,
    analitica.pend_valores_metrica,
    analitica.pend_cotizaciones,
    analitica.pend_indices_competitividad,
    analitica.notificaciones_aprobacion
TO rol_etl;

REVOKE SELECT ON
    analitica.publicaciones_lote,
    analitica.pend_valores_metrica,
    analitica.pend_cotizaciones,
    analitica.pend_indices_competitividad,
    analitica.notificaciones_aprobacion
FROM rol_powerbi;

REVOKE EXECUTE ON FUNCTION analitica.registrar_publicacion_lote() FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION analitica.estado_publicacion(UUID) FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION analitica.lote_pendiente(UUID) FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION analitica.desviar_valor_metrica() FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION analitica.desviar_cotizacion() FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION analitica.desviar_indice() FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION analitica.publicar_lote(UUID, UUID) FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION analitica.rechazar_lote(UUID, UUID, VARCHAR) FROM PUBLIC;

GRANT EXECUTE ON FUNCTION analitica.publicar_lote(UUID, UUID) TO rol_backend;
GRANT EXECUTE ON FUNCTION analitica.rechazar_lote(UUID, UUID, VARCHAR) TO rol_backend;

COMMIT;

-- =====================================================================
-- Notas operativas que no puede resolver este script:
--
-- 1. El ETL actual no marca un lote como Fallido si procesar_analitica
--    falla después de insertar el lote. Para cobertura completa, Python
--    debe actualizar cruda.lotes_carga.estado_carga = 'Fallido' y llamar
--    a una función de cierre/rechazo en el except. Mientras tanto, un
--    lote con ETL fallido puede quedar pendiente y luego fallar en
--    publicar_lote si no tiene registros válidos/pendientes.
--
-- 2. fct_cotizaciones no tiene id_lote. Se mantiene la variable
--    siftc.lote_en_proceso como puente. Si se procesan dos lotes en una
--    misma transacción, o si se usa calcular_cotizaciones() directamente,
--    hay riesgo de ambigüedad. La solución estructural es agregar id_lote
--    a fct_cotizaciones.
--
-- 3. La notificación al equipo de Análisis de datos queda como evento en
--    analitica.notificaciones_aprobacion. Falta un worker/API que la lea
--    y envíe el correo o mensaje real.
--
-- 4. La matriz de permisos de aplicación debe aplicarse en .NET: el rol
--    de base de datos rol_backend es compartido y no distingue entre
--    Administrador, Analista, Auditor, etc.
-- =====================================================================
