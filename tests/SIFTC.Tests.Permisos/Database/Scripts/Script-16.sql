-- =====================================================================
-- SIFTC - MIGRACIÓN: AJUSTE DE TAMAÑOS EN UNA BASE YA CREADA
-- Úsalo solo si siftc1_db ya existe con los scripts anteriores.
-- En una instalación nueva basta con correr Script-3 a Script-6 actualizados.
-- Todo va en una transacción: si algún dato no cabe, no se cambia nada.
-- =====================================================================
BEGIN;

-- 1. Las vistas dependen de columnas que cambian de tipo: se quitan y se recrean
--    v_entidades_cotizacion depende de v_entidades, por eso se quita primero
DROP VIEW IF EXISTS analitica.v_entidades_cotizacion;
DROP VIEW IF EXISTS analitica.v_metricas_financieras;
DROP VIEW IF EXISTS analitica.v_entidades;

-- 2. SEGURIDAD
ALTER TABLE seguridad.roles
    ALTER COLUMN nombre         TYPE VARCHAR(30),
    ALTER COLUMN descripcion    TYPE VARCHAR(150),
    ALTER COLUMN permisos_clave TYPE VARCHAR(60);

-- 3. CRUDA
ALTER TABLE cruda.lotes_carga
    ALTER COLUMN nombre_archivo TYPE VARCHAR(150);

-- 4. LIMPIA
ALTER TABLE limpia.tbl_sectores
    ALTER COLUMN nombre TYPE VARCHAR(30);

ALTER TABLE limpia.tbl_entidades
    ALTER COLUMN nombre TYPE VARCHAR(80);

ALTER TABLE limpia.tbl_datos_financieros
    ALTER COLUMN utilidad_neta      TYPE NUMERIC(15,2),
    ALTER COLUMN patrimonio         TYPE NUMERIC(15,2),
    ALTER COLUMN activos_totales    TYPE NUMERIC(15,2),
    ALTER COLUMN pasivo_total       TYPE NUMERIC(15,2),
    ALTER COLUMN activo_corriente   TYPE NUMERIC(15,2),
    ALTER COLUMN pasivo_corriente   TYPE NUMERIC(15,2),
    ALTER COLUMN inventarios        TYPE NUMERIC(15,2),
    ALTER COLUMN ebitda             TYPE NUMERIC(15,2),
    ALTER COLUMN ebit               TYPE NUMERIC(15,2),
    ALTER COLUMN ingresos           TYPE NUMERIC(15,2),
    ALTER COLUMN gastos_financieros TYPE NUMERIC(15,2);

-- 5. ANALÍTICA
ALTER TABLE analitica.dim_dimension
    ALTER COLUMN nombre TYPE VARCHAR(30);

ALTER TABLE analitica.dim_metrica
    ALTER COLUMN nombre  TYPE VARCHAR(50),
    ALTER COLUMN formula TYPE VARCHAR(120);

ALTER TABLE analitica.fct_valores_metrica
    ALTER COLUMN valor TYPE NUMERIC(14,6);

-- 6. Recrear las vistas (mismo código de Script-9)
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
    MAX(v.fecha_carga) AS fecha_carga
FROM analitica.fct_valores_metrica v
JOIN analitica.dim_metrica m ON m.id_metrica = v.id_metrica
JOIN limpia.tbl_entidades e ON e.id_entidad = v.id_entidad
JOIN limpia.tbl_sectores s ON s.id_sector = e.id_sector
WHERE v.id_registro_origen IS NOT NULL
GROUP BY v.id_registro_origen, v.id_lote, e.ticker, e.nombre, s.nombre, v.periodo;

CREATE OR REPLACE VIEW analitica.v_entidades_cotizacion AS
SELECT
    e.id_entidad,
    e.ticker,
    e.nombre,
    e.sector,
    act.fecha,
    act.cierre AS ic,
    (act.cierre - ant.cierre)::NUMERIC(6,3) AS variacion,
    act.variacion_pct
FROM analitica.v_entidades e
LEFT JOIN LATERAL (
    SELECT c.fecha, c.cierre, c.variacion_pct
    FROM analitica.fct_cotizaciones c
    WHERE c.id_entidad = e.id_entidad
    ORDER BY c.fecha DESC
    LIMIT 1
) act ON TRUE
LEFT JOIN LATERAL (
    SELECT c.cierre
    FROM analitica.fct_cotizaciones c
    WHERE c.id_entidad = e.id_entidad
      AND c.fecha < act.fecha
    ORDER BY c.fecha DESC
    LIMIT 1
) ant ON TRUE;

-- 7. Al recrear las vistas se pierden sus permisos: se vuelven a dar (CA-03)
GRANT ALL PRIVILEGES ON analitica.v_entidades, analitica.v_metricas_financieras, analitica.v_entidades_cotizacion TO rol_etl;
GRANT SELECT ON analitica.v_entidades, analitica.v_metricas_financieras, analitica.v_entidades_cotizacion TO rol_powerbi, rol_backend;

COMMIT;
