-- Muestra los 3 roles de aplicación creados en el servidor PostgreSQL
SELECT rolname, rolcanlogin, rolsuper
FROM pg_roles
WHERE rolname IN ('rol_etl', 'rol_backend', 'rol_powerbi');