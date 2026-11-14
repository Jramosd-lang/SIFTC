-- =====================================================================
-- SIFTC - Ajuste de columnas al diccionario de datos (fase 1)
-- Ejecutar como dueño de los objetos (postgres), después de Script-17.
-- =====================================================================
BEGIN;

-- Las vistas dependen de las columnas que se van a ampliar.
DROP VIEW IF EXISTS analitica.v_entidades_cotizacion;
DROP VIEW IF EXISTS analitica.v_metricas_financieras;
DROP VIEW IF EXISTS analitica.v_entidades;
DROP VIEW IF EXISTS analitica.v_ranking;

-- ---------------------------------------------------------------------
-- CRUDA: bitácora de cargas
-- ---------------------------------------------------------------------
ALTER TABLE cruda.lotes_carga
    ALTER COLUMN nombre_archivo TYPE VARCHAR(200);

ALTER TABLE cruda.lotes_carga
    ADD COLUMN IF NOT EXISTS estado_carga VARCHAR(32) NOT NULL DEFAULT 'Exitoso';

ALTER TABLE cruda.lotes_carga
    DROP CONSTRAINT IF EXISTS ck_lotes_carga_estado;

ALTER TABLE cruda.lotes_carga
    ADD CONSTRAINT ck_lotes_carga_estado
    CHECK (estado_carga IN ('Exitoso', 'Fallido'));

-- ---------------------------------------------------------------------
-- LIMPIA
-- ---------------------------------------------------------------------
ALTER TABLE limpia.tbl_sectores
    ALTER COLUMN nombre TYPE VARCHAR(50);

ALTER TABLE limpia.tbl_entidades
    ALTER COLUMN nombre TYPE VARCHAR(100);

ALTER TABLE limpia.tbl_datos_financieros
    ALTER COLUMN utilidad_neta TYPE NUMERIC(20, 2),
    ALTER COLUMN patrimonio TYPE NUMERIC(20, 2),
    ALTER COLUMN activos_totales TYPE NUMERIC(20, 2),
    ALTER COLUMN pasivo_total TYPE NUMERIC(20, 2),
    ALTER COLUMN activo_corriente TYPE NUMERIC(20, 2),
    ALTER COLUMN pasivo_corriente TYPE NUMERIC(20, 2),
    ALTER COLUMN inventarios TYPE NUMERIC(20, 2),
    ALTER COLUMN ebitda TYPE NUMERIC(20, 2),
    ALTER COLUMN ebit TYPE NUMERIC(20, 2),
    ALTER COLUMN ingresos TYPE NUMERIC(20, 2),
    ALTER COLUMN gastos_financieros TYPE NUMERIC(20, 2);

-- ---------------------------------------------------------------------
-- ANALITICA
-- ---------------------------------------------------------------------
ALTER TABLE analitica.dim_dimension
    ALTER COLUMN nombre TYPE VARCHAR(40);

ALTER TABLE analitica.dim_metrica
    ALTER COLUMN nombre TYPE VARCHAR(80),
    ALTER COLUMN formula TYPE VARCHAR(200);

ALTER TABLE analitica.fct_valores_metrica
    ALTER COLUMN valor TYPE NUMERIC(20, 6);

-- ---------------------------------------------------------------------
-- SEGURIDAD
-- ---------------------------------------------------------------------
ALTER TABLE seguridad.roles
    ALTER COLUMN nombre TYPE VARCHAR(40),
    ALTER COLUMN descripcion TYPE TEXT,
    ALTER COLUMN permisos_clave TYPE VARCHAR(100);

-- ---------------------------------------------------------------------
-- Recrear vistas afectadas
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW analitica.v_entidades AS
SELECT
    e.id_entidad,
    e.ticker,
    e.nombre,
    s.nombre AS sector
FROM limpia.tbl_entidades e
JOIN limpia.tbl_sectores s ON s.id_sector = e.id_sector;

CREATE OR REPLACE VIEW analitica.v_metricas_financieras AS
SELECT
    v.id_registro_origen,
    v.id_lote,
    e.ticker AS codigo_entidad,
    e.nombre AS nombre_entidad,
    s.nombre AS sector,
    v.periodo,
    MAX(v.valor) FILTER (WHERE m.codigo = 'RNT-01') AS roe,
    MAX(v.valor) FILTER (WHERE m.codigo = 'RNT-02') AS roa,
    MAX(v.valor) FILTER (WHERE m.codigo = 'RNT-03') AS margen_ebitda,
    MAX(v.valor) FILTER (WHERE m.codigo = 'LIQ-01') AS razon_corriente,
    MAX(v.valor) FILTER (WHERE m.codigo = 'LIQ-02') AS prueba_acida,
    MAX(v.valor) FILTER (WHERE m.codigo = 'END-01') AS endeudamiento_total,
    MAX(v.valor) FILTER (WHERE m.codigo = 'END-02') AS cobertura_intereses,
    MAX(v.valor) FILTER (WHERE m.codigo = 'CRE-01') AS crecimiento_ingresos,
    MAX(v.fecha_carga) AS fecha_carga,
    v.id_entidad
FROM analitica.fct_valores_metrica v
JOIN analitica.dim_metrica m ON m.id_metrica = v.id_metrica
JOIN limpia.tbl_entidades e ON e.id_entidad = v.id_entidad
JOIN limpia.tbl_sectores s ON s.id_sector = e.id_sector
WHERE v.id_registro_origen IS NOT NULL
GROUP BY v.id_registro_origen, v.id_lote, e.ticker, e.nombre, s.nombre, v.periodo, v.id_entidad;

CREATE OR REPLACE VIEW analitica.v_entidades_cotizacion AS
WITH ultima AS (
    SELECT DISTINCT ON (c.id_entidad)
        c.id_entidad,
        c.fecha,
        c.cierre,
        c.variacion_pct
    FROM analitica.fct_cotizaciones c
    ORDER BY c.id_entidad, c.fecha DESC
),
anterior AS (
    SELECT
        c.id_entidad,
        c.cierre,
        ROW_NUMBER() OVER (PARTITION BY c.id_entidad ORDER BY c.fecha DESC) AS rn
    FROM analitica.fct_cotizaciones c
)
SELECT
    e.id_entidad,
    e.ticker,
    e.nombre,
    e.sector,
    u.cierre AS ic,
    CASE WHEN a.cierre IS NULL THEN NULL ELSE u.cierre - a.cierre END AS variacion,
    u.variacion_pct
FROM analitica.v_entidades e
LEFT JOIN ultima u ON u.id_entidad = e.id_entidad
LEFT JOIN anterior a ON a.id_entidad = e.id_entidad AND a.rn = 2;

CREATE OR REPLACE VIEW analitica.v_ranking AS
SELECT
    c.fecha,
    e.id_entidad,
    e.ticker,
    e.id_sector,
    c.cierre AS ic,
    RANK() OVER (PARTITION BY c.fecha ORDER BY c.cierre DESC) AS posicion_general,
    RANK() OVER (PARTITION BY c.fecha, e.id_sector ORDER BY c.cierre DESC) AS posicion_sector
FROM analitica.fct_cotizaciones c
JOIN limpia.tbl_entidades e ON e.id_entidad = c.id_entidad;

GRANT SELECT ON
    analitica.v_entidades,
    analitica.v_metricas_financieras,
    analitica.v_entidades_cotizacion,
    analitica.v_ranking
TO rol_backend, rol_powerbi;

GRANT ALL PRIVILEGES ON
    analitica.v_entidades,
    analitica.v_metricas_financieras,
    analitica.v_entidades_cotizacion,
    analitica.v_ranking
TO rol_etl;

COMMIT;
