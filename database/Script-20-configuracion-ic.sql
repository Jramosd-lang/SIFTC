-- =====================================================================
-- SIFTC - HU-BE-09: configuracion versionada del Indice de Competitividad
-- Ejecutar como dueno de los objetos (postgres), despues de Script-19.
--
-- CA 1: los pesos deben sumar 100 %.
-- CA 2: cada cambio crea una version con fecha de vigencia y motivo.
-- CA 3: la version nueva aplica solo a calculos posteriores.
--
-- Solo el Administrador escribe. El ETL lee la version vigente al iniciar
-- el calculo y guarda su id_version_ic en cruda.lotes_carga.
-- =====================================================================
BEGIN;

-- ---------------------------------------------------------------------
-- 1. Version de configuracion
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS analitica.configuracion_ic_version (
    id_version UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    numero_version INTEGER NOT NULL UNIQUE,
    fecha_vigencia TIMESTAMPTZ NOT NULL DEFAULT now(),
    motivo VARCHAR(500) NOT NULL,
    id_usuario UUID NOT NULL REFERENCES seguridad.usuarios (id_usuario),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT now(),
    frecuencia_ic VARCHAR(12) NOT NULL DEFAULT 'Mensual'
        CHECK (frecuencia_ic IN ('Diaria', 'Mensual', 'Trimestral')),
    CONSTRAINT ck_configuracion_ic_motivo CHECK (btrim(motivo) <> '')
);

CREATE INDEX IF NOT EXISTS ix_configuracion_ic_vigencia
    ON analitica.configuracion_ic_version (fecha_vigencia DESC, numero_version DESC);

-- ---------------------------------------------------------------------
-- 2. Pesos por dimension
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS analitica.configuracion_ic_peso (
    id_version UUID NOT NULL REFERENCES analitica.configuracion_ic_version (id_version) ON DELETE CASCADE,
    id_dimension UUID NOT NULL REFERENCES analitica.dim_dimension (id_dimension),
    peso NUMERIC(9,6) NOT NULL CHECK (peso BETWEEN 0 AND 1),
    PRIMARY KEY (id_version, id_dimension)
);

-- ---------------------------------------------------------------------
-- 3. Umbrales por metrica
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS analitica.configuracion_ic_umbral (
    id_version UUID NOT NULL REFERENCES analitica.configuracion_ic_version (id_version) ON DELETE CASCADE,
    id_metrica UUID NOT NULL REFERENCES analitica.dim_metrica (id_metrica),
    limite_inferior NUMERIC(20,6),
    limite_superior NUMERIC(20,6),
    severidad VARCHAR(20) NOT NULL
        CHECK (severidad IN ('Informativa', 'Advertencia', 'Critica')),
    PRIMARY KEY (id_version, id_metrica),
    CONSTRAINT ck_configuracion_ic_umbral_rango CHECK (
        limite_inferior IS NULL OR limite_superior IS NULL OR limite_inferior <= limite_superior
    )
);

-- ---------------------------------------------------------------------
-- 4. Version inicial a partir de los pesos actuales de dim_dimension
-- ---------------------------------------------------------------------
DO $$
DECLARE
    v_id_version UUID;
    v_id_usuario UUID;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM analitica.configuracion_ic_version) THEN
        SELECT u.id_usuario
        INTO v_id_usuario
        FROM seguridad.usuarios u
        JOIN seguridad.roles r ON r.id_rol = u.id_rol
        WHERE r.nombre = 'Administrador'
          AND u.estado = 'activo'
        ORDER BY u.email
        LIMIT 1;

        IF v_id_usuario IS NOT NULL THEN
            INSERT INTO analitica.configuracion_ic_version
                (numero_version, motivo, id_usuario, frecuencia_ic)
            VALUES
                (1, 'Version inicial creada por Script-20', v_id_usuario, 'Mensual')
            RETURNING id_version INTO v_id_version;

            INSERT INTO analitica.configuracion_ic_peso (id_version, id_dimension, peso)
            SELECT v_id_version, d.id_dimension, d.peso_ic
            FROM analitica.dim_dimension d;
        END IF;
    END IF;
END;
$$;

-- ---------------------------------------------------------------------
-- 5. Crear una version nueva
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION analitica.crear_version_ic(
    p_id_usuario UUID,
    p_motivo VARCHAR,
    p_frecuencia VARCHAR,
    p_pesos JSONB,
    p_umbrales JSONB
)
RETURNS UUID
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, pg_temp
AS $$
DECLARE
    v_id_version UUID;
    v_numero_version INTEGER;
    v_suma NUMERIC(20,6);
    v_umbrales JSONB := COALESCE(p_umbrales, '[]'::jsonb);
BEGIN
    IF p_id_usuario IS NULL THEN
        RAISE EXCEPTION 'El usuario es obligatorio.' USING ERRCODE = 'SI422';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM seguridad.usuarios u
        JOIN seguridad.roles r ON r.id_rol = u.id_rol
        WHERE u.id_usuario = p_id_usuario
          AND u.estado = 'activo'
          AND r.nombre = 'Administrador'
    ) THEN
        RAISE EXCEPTION 'Solo un Administrador activo puede configurar el IC.'
            USING ERRCODE = 'SI403';
    END IF;

    IF p_motivo IS NULL OR btrim(p_motivo) = '' OR length(btrim(p_motivo)) > 500 THEN
        RAISE EXCEPTION 'El motivo es obligatorio y no puede superar 500 caracteres.'
            USING ERRCODE = 'SI422';
    END IF;

    IF p_frecuencia IS NULL OR p_frecuencia NOT IN ('Diaria', 'Mensual', 'Trimestral') THEN
        RAISE EXCEPTION 'La frecuencia admitida es Diaria, Mensual o Trimestral.'
            USING ERRCODE = 'SI422';
    END IF;

    IF p_pesos IS NULL OR jsonb_typeof(p_pesos) <> 'array' OR jsonb_array_length(p_pesos) = 0 THEN
        RAISE EXCEPTION 'Debe enviar al menos un peso.' USING ERRCODE = 'SI422';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM (
            SELECT item->>'dimension' AS dimension, count(*) AS total
            FROM jsonb_array_elements(p_pesos) item
            GROUP BY item->>'dimension'
            HAVING count(*) > 1
        ) duplicados
    ) THEN
        RAISE EXCEPTION 'No se puede repetir una dimension.' USING ERRCODE = 'SI422';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM jsonb_to_recordset(p_pesos) AS item(dimension TEXT, peso NUMERIC)
        LEFT JOIN analitica.dim_dimension d ON d.nombre = item.dimension
        WHERE d.id_dimension IS NULL
           OR item.dimension IS NULL
           OR item.peso IS NULL
           OR item.peso < 0
           OR item.peso > 1
    ) THEN
        RAISE EXCEPTION 'Los pesos deben usar dimensiones existentes y valores entre 0 y 1.'
            USING ERRCODE = 'SI422';
    END IF;

    SELECT coalesce(sum(item.peso), 0)
    INTO v_suma
    FROM jsonb_to_recordset(p_pesos) AS item(dimension TEXT, peso NUMERIC);

    IF abs(v_suma - 1) > 0.000001 THEN
        RAISE EXCEPTION 'Los pesos deben sumar 1.000000 (100 por ciento). Suma actual: %.', v_suma
            USING ERRCODE = 'SI422';
    END IF;

    IF jsonb_typeof(v_umbrales) <> 'array' THEN
        RAISE EXCEPTION 'Los umbrales deben ser un arreglo.' USING ERRCODE = 'SI422';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM jsonb_to_recordset(v_umbrales) AS item(
            codigo_metrica TEXT,
            limite_inferior NUMERIC,
            limite_superior NUMERIC,
            severidad TEXT
        )
        LEFT JOIN analitica.dim_metrica m ON m.codigo = item.codigo_metrica
        WHERE m.id_metrica IS NULL
           OR item.severidad NOT IN ('Informativa', 'Advertencia', 'Critica')
           OR (item.limite_inferior IS NOT NULL
               AND item.limite_superior IS NOT NULL
               AND item.limite_inferior > item.limite_superior)
    ) THEN
        RAISE EXCEPTION 'Los umbrales deben usar metricas existentes, severidad valida y rango coherente.'
            USING ERRCODE = 'SI422';
    END IF;

    LOCK TABLE analitica.configuracion_ic_version IN EXCLUSIVE MODE;

    SELECT coalesce(max(numero_version), 0) + 1
    INTO v_numero_version
    FROM analitica.configuracion_ic_version;

    INSERT INTO analitica.configuracion_ic_version
        (numero_version, motivo, id_usuario, frecuencia_ic)
    VALUES
        (v_numero_version, btrim(p_motivo), p_id_usuario, p_frecuencia)
    RETURNING id_version INTO v_id_version;

    INSERT INTO analitica.configuracion_ic_peso (id_version, id_dimension, peso)
    SELECT v_id_version, d.id_dimension, item.peso
    FROM jsonb_to_recordset(p_pesos) AS item(dimension TEXT, peso NUMERIC)
    JOIN analitica.dim_dimension d ON d.nombre = item.dimension;

    INSERT INTO analitica.configuracion_ic_umbral
        (id_version, id_metrica, limite_inferior, limite_superior, severidad)
    SELECT v_id_version, m.id_metrica, item.limite_inferior, item.limite_superior, item.severidad
    FROM jsonb_to_recordset(v_umbrales) AS item(
        codigo_metrica TEXT,
        limite_inferior NUMERIC,
        limite_superior NUMERIC,
        severidad TEXT
    )
    JOIN analitica.dim_metrica m ON m.codigo = item.codigo_metrica;

    RETURN v_id_version;
END;
$$;

-- ---------------------------------------------------------------------
-- 6. Trazabilidad de la version usada por cada calculo
-- ---------------------------------------------------------------------
ALTER TABLE cruda.lotes_carga
    ADD COLUMN IF NOT EXISTS id_version_ic UUID
    REFERENCES analitica.configuracion_ic_version (id_version);

-- ---------------------------------------------------------------------
-- 7. Permisos
-- ---------------------------------------------------------------------
GRANT SELECT ON
    analitica.configuracion_ic_version,
    analitica.configuracion_ic_peso,
    analitica.configuracion_ic_umbral
TO rol_backend, rol_etl;

REVOKE SELECT ON
    analitica.configuracion_ic_version,
    analitica.configuracion_ic_peso,
    analitica.configuracion_ic_umbral
FROM rol_powerbi;

REVOKE EXECUTE ON FUNCTION
    analitica.crear_version_ic(UUID, VARCHAR, VARCHAR, JSONB, JSONB)
FROM PUBLIC;

GRANT EXECUTE ON FUNCTION
    analitica.crear_version_ic(UUID, VARCHAR, VARCHAR, JSONB, JSONB)
TO rol_backend;

COMMIT;

