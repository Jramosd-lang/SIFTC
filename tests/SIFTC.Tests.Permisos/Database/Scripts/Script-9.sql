-- =====================================================================
-- 1. FUNCIÓN DE CÁLCULO DE COTIZACIONES 
-- =====================================================================
CREATE OR REPLACE FUNCTION analitica.calcular_cotizaciones(p_fecha DATE) 
RETURNS void 
LANGUAGE sql AS $$
    INSERT INTO analitica.fct_cotizaciones (
        id_entidad, fecha, apertura, maximo, minimo, cierre,
        variacion_pct, volumen_actualizaciones
    )
    SELECT 
        d.id_entidad, 
        p_fecha, 
        d.apertura, 
        d.maximo, 
        d.minimo, 
        d.cierre,
        ROUND(100.0 * (d.cierre - prev.cierre) / NULLIF(prev.cierre, 0), 4),
        d.volumen
    FROM (
        SELECT 
            id_entidad,
            (array_agg(ic ORDER BY fecha_carga))[1]      AS apertura,
            MAX(ic)                                      AS maximo,
            MIN(ic)                                      AS minimo,
            (array_agg(ic ORDER BY fecha_carga DESC))[1] AS cierre,
            COUNT(*)::int                                AS volumen
        FROM analitica.fct_indices_competitividad
        WHERE fecha_carga >= p_fecha::timestamp 
          AND fecha_carga < (p_fecha + INTERVAL '1 day')
        GROUP BY id_entidad
    ) d
    LEFT JOIN LATERAL (
        SELECT c.cierre 
        FROM analitica.fct_cotizaciones c
        WHERE c.id_entidad = d.id_entidad 
          AND c.fecha < p_fecha
        ORDER BY c.fecha DESC 
        LIMIT 1
    ) prev ON TRUE
    ON CONFLICT (id_entidad, fecha) DO UPDATE SET
        apertura = EXCLUDED.apertura,
        maximo = EXCLUDED.maximo,
        minimo = EXCLUDED.minimo,
        cierre = EXCLUDED.cierre,
        variacion_pct = EXCLUDED.variacion_pct,
        volumen_actualizaciones = EXCLUDED.volumen_actualizaciones,
        fecha_carga = now();
$$;

-- =====================================================================
-- 2. VISTA DE ENTIDADES
-- =====================================================================
CREATE OR REPLACE VIEW analitica.v_entidades AS 
SELECT 
    e.id_entidad, 
    e.ticker, 
    e.nombre, 
    s.nombre AS sector
FROM limpia.tbl_entidades e
JOIN limpia.tbl_sectores s ON s.id_sector = e.id_sector;

-- =====================================================================
-- 3. VISTA DE MÉTRICAS FINANCIERAS (PIVOT)
-- =====================================================================
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

-- =====================================================================
-- 4. VISTA DE RANKING
-- =====================================================================
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

-- =====================================================================
-- 5. VISTA DE COTIZACIÓN VIGENTE POR ENTIDAD (HU-BE-03: GET /entidades)
-- Última vela de cada entidad y su variación frente al cierre anterior.
-- variacion_pct se toma de fct_cotizaciones (la calcula calcular_cotizaciones)
-- y variacion usa el mismo cierre anterior, así ambas son coherentes.
-- Las entidades sin cotizaciones aparecen con fecha, ic y variaciones en NULL.
-- =====================================================================
CREATE OR REPLACE VIEW analitica.v_entidades_cotizacion AS
SELECT
    e.id_entidad,
    e.ticker,
    e.nombre,
    e.sector,
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



