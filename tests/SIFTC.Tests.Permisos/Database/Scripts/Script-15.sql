-- Confirma que Power BI solo puede leer 'analitica' y tiene DENEGADO el acceso a 'cruda' y 'limpia'
SELECT 
    'cruda' AS esquema, has_schema_privilege('rol_powerbi', 'cruda', 'USAGE') AS acceso UNION ALL
SELECT 
    'limpia' AS esquema, has_schema_privilege('rol_powerbi', 'limpia', 'USAGE') AS acceso UNION ALL
SELECT 
    'analitica' AS esquema, has_schema_privilege('rol_powerbi', 'analitica', 'USAGE') AS acceso;