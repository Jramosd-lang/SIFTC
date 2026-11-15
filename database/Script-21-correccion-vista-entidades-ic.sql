-- =====================================================================
-- SIFTC - Correccion de la vista de entidades para usar el ultimo IC
-- HU-BE-09 / soporte a ranking
--
-- IMPORTANTE:
--   Esta version NO modifica v_ranking.
--   v_ranking se mantiene con fct_cotizaciones.cierre porque esa tabla
--   guarda la serie OHLC del IC y su cierre es el IC del periodo.
--   Cambiar v_ranking para usar fct_indices_competitividad con
--   i.fecha_carga <= c.fecha devuelve 0 filas con los datos actuales.
--
-- Ejecutar como dueno de los objetos, despues de Script-18.
-- =====================================================================
BEGIN;

DROP VIEW IF EXISTS analitica.v_entidades_cotizacion;

CREATE OR REPLACE VIEW analitica.v_entidades_cotizacion AS
WITH ultima_cotizacion AS (
    SELECT DISTINCT ON (c.id_entidad)
        c.id_entidad,
        c.cierre,
        c.variacion_pct
    FROM analitica.fct_cotizaciones c
    ORDER BY c.id_entidad, c.fecha DESC
),
anterior_cotizacion AS (
    SELECT
        c.id_entidad,
        c.cierre,
        ROW_NUMBER() OVER (PARTITION BY c.id_entidad ORDER BY c.fecha DESC) AS rn
    FROM analitica.fct_cotizaciones c
),
ultimo_ic AS (
    SELECT DISTINCT ON (i.id_entidad)
        i.id_entidad,
        i.ic
    FROM analitica.fct_indices_competitividad i
    ORDER BY i.id_entidad, i.fecha_carga DESC
)
SELECT
    e.id_entidad,
    e.ticker,
    e.nombre,
    e.sector,
    ui.ic,
    CASE
        WHEN ac.cierre IS NULL THEN NULL
        ELSE uc.cierre - ac.cierre
    END AS variacion,
    uc.variacion_pct
FROM analitica.v_entidades e
LEFT JOIN ultima_cotizacion uc ON uc.id_entidad = e.id_entidad
LEFT JOIN anterior_cotizacion ac ON ac.id_entidad = e.id_entidad AND ac.rn = 2
LEFT JOIN ultimo_ic ui ON ui.id_entidad = e.id_entidad;

GRANT SELECT ON analitica.v_entidades_cotizacion TO rol_backend, rol_powerbi;
GRANT ALL PRIVILEGES ON analitica.v_entidades_cotizacion TO rol_etl;

COMMIT;
