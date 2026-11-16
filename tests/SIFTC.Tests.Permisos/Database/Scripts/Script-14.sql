-- Muestra la asignación de permisos de uso (USAGE) por esquema para cada rol
SELECT 
    nspname AS esquema,
    r.rolname AS usuario_o_rol,
    has_schema_privilege(r.rolname, nspname, 'USAGE') AS tiene_acceso_uso
FROM pg_namespace
CROSS JOIN (
    SELECT 'rol_etl' AS rolname UNION ALL
    SELECT 'rol_backend' UNION ALL
    SELECT 'rol_powerbi'
) r
WHERE nspname IN ('cruda', 'limpia', 'analitica', 'seguridad')
ORDER BY r.rolname, nspname;