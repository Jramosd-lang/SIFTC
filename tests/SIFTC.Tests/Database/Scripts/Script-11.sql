-- =====================================================================
-- SIFTC - SCRIPT DE VERIFICACIÓN Y AUDITORÍA DE DATOS
-- =====================================================================

-- 1. VERIFICAR REGISTRO DE LOTES DE CARGA (CAPA CRUDA)
SELECT 
    id_lote,
    nombre_archivo,
    fecha_carga,
    registros_totales,
    registros_validos,
    indice_calidad || '%' AS calidad
FROM cruda.lotes_carga
ORDER BY fecha_carga DESC;

-- 2. VERIFICAR DATOS DE LA MATRIZ BRUTA EN TEXTO (CAPA CRUDA)
SELECT 
    id_cruda,
    codigo_entidad,
    nombre_entidad,
    sector,
    periodo,
    utilidad_neta,
    activos_totales,
    fecha_carga
FROM cruda.ext_matriz_financiera_entidades
ORDER BY fecha_carga DESC
LIMIT 5;

-- 3. VERIFICAR SECTORES Y ENTIDADES ESTANDARIZADAS (CAPA LIMPIA)
SELECT 
    e.ticker,
    e.nombre AS empresa,
    s.nombre AS sector
FROM limpia.tbl_entidades e
JOIN limpia.tbl_sectores s ON s.id_sector = e.id_sector;

-- 4. VERIFICAR DATOS FINANCIEROS NUMÉRICOS Y SUS LLAVES (CAPA LIMPIA)
SELECT 
    f.id_dato,
    e.ticker,
    f.periodo,
    f.utilidad_neta,
    f.activos_totales,
    f.id_registro_origen AS id_cruda_vinculado
FROM limpia.tbl_datos_financieros f
JOIN limpia.tbl_entidades e ON e.id_entidad = f.id_entidad
ORDER BY f.periodo DESC
LIMIT 5;

-- 5. VERIFICAR MÉTRICAS CALCULADAS (CAPA ANALÍTICA)
SELECT 
    v.id_valor,
    e.ticker,
    m.codigo AS cod_metrica,
    m.nombre AS metrica,
    v.periodo,
    v.valor AS resultado_calculado,
    v.id_registro_origen AS id_limpia_vinculado
FROM analitica.fct_valores_metrica v
JOIN analitica.dim_metrica m ON m.id_metrica = v.id_metrica
JOIN limpia.tbl_entidades e ON e.id_entidad = v.id_entidad
ORDER BY v.periodo DESC, m.codigo
LIMIT 10;

-- 6. VERIFICAR VISTA PIVOTE DE MÉTRICAS FINANCIERAS POR ENTIDAD Y PERIODO
SELECT * 
FROM analitica.v_metricas_financieras
LIMIT 5;

-- 7. CONSULTA CLAVE DE TRAZABILIDAD (MUESTRA DE 10 REGISTROS - CA-02)
-- Muestra el camino completo desde Analítica -> Limpia -> Cruda
SELECT 
    a.id_valor AS id_analitica,
    m.codigo AS metrica,
    a.periodo,
    a.valor AS valor_calculado,
    l.id_dato AS id_limpia,
    l.utilidad_neta AS utilidad_num_limpia,
    c.id_cruda,
    c.utilidad_neta AS utilidad_texto_cruda,
    a.id_lote
FROM analitica.fct_valores_metrica a
JOIN analitica.dim_metrica m ON m.id_metrica = a.id_metrica
JOIN limpia.tbl_datos_financieros l ON l.id_dato = a.id_registro_origen
JOIN cruda.ext_matriz_financiera_entidades c ON c.id_cruda = l.id_registro_origen
ORDER BY a.fecha_carga DESC
LIMIT 10;