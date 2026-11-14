-- =====================================================================
-- 1. POBLAR DATOS SEMILLA (ROLES, DIMENSIONES Y MÉTRICAS)
-- =====================================================================

INSERT INTO seguridad.roles (nombre, descripcion, permisos_clave) VALUES
 ('Administrador', 'Gestiona usuarios, roles y parámetros del sistema', 'Control total del sistema'),
 ('Data Steward', 'Mantiene el diccionario y las reglas de calidad del software', 'Editar catálogo y reglas'),
 ('Analista financiero', 'Calcula, valida y publica métricas', 'Ejecutar cálculos y aprobar publicación'),
 ('Gerente / Directivo', 'Consulta rankings y toma de decisiones', 'Lectura de tableros y exportes'),
 ('Auditor', 'Revisa linaje y bitácora', 'Lectura de auditoría, sin edición'),
 ('Usuario móvil', 'Consulta cotizaciones y recibe alertas', 'Lectura y watchlist personal')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO analitica.dim_dimension (nombre, peso_ic) VALUES
 ('Rentabilidad', 0.35), 
 ('Liquidez', 0.20), 
 ('Solvencia', 0.20),
 ('Crecimiento', 0.25), 
 ('Mercado', 0), 
 ('Trazabilidad', 0)
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO analitica.dim_metrica (id_dimension, codigo, nombre, formula, frecuencia)
SELECT d.id_dimension, v.codigo, v.nombre, v.formula, v.frecuencia
FROM (VALUES
 ('Rentabilidad', 'RNT-01', 'ROE', 'utilidad_neta / patrimonio', 'Mensual'),
 ('Rentabilidad', 'RNT-02', 'ROA', 'utilidad_neta / activos_totales', 'Mensual'),
 ('Rentabilidad', 'RNT-03', 'Margen EBITDA', 'ebitda / ingresos', 'Mensual'),
 ('Liquidez', 'LIQ-01', 'Razón corriente', 'activo_corriente / pasivo_corriente', 'Mensual'),
 ('Liquidez', 'LIQ-02', 'Prueba ácida', '(activo_corriente - inventarios) / pasivo_corriente', 'Mensual'),
 ('Solvencia', 'END-01', 'Nivel de endeudamiento', 'pasivo_total / activos_totales', 'Mensual'),
 ('Solvencia', 'END-02', 'Cobertura de intereses', 'ebit / gastos_financieros', 'Trimestral'),
 ('Crecimiento', 'CRE-01', 'Crecimiento de ingresos', '(ingresos_t - ingresos_t1) / ingresos_t1', 'Mensual'),
 ('Mercado', 'CRE-02', 'Variación de cotización', '(ic_t - ic_t1) / ic_t1', 'Diaria'),
 ('Trazabilidad', 'CAL-01', 'Índice de calidad del dato', 'registros_validos / registros_totales', 'Diaria')
) v(dim, codigo, nombre, formula, frecuencia)
JOIN analitica.dim_dimension d ON d.nombre = v.dim
ON CONFLICT (codigo) DO NOTHING;