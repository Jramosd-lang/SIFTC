-- =====================================================================
-- SIFTC - Sprint 2: vista aprobada por Análisis de Datos para /entidades
-- Ejecutar como el rol dueño de los objetos (postgres), después de
-- Script-9 / Script-16. El backend solo recibe SELECT sobre analitica.
-- =====================================================================
BEGIN;

-- Entidades con su última cotización y la variación frente al cierre anterior.
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

-- CA-03: el backend .NET y Power BI solo leen la capa analítica.
GRANT SELECT ON analitica.v_entidades_cotizacion TO rol_backend, rol_powerbi;
GRANT ALL PRIVILEGES ON analitica.v_entidades_cotizacion TO rol_etl;

COMMIT;
