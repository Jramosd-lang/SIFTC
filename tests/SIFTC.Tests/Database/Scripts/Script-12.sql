-- =====================================================================
-- CA-03: INDEPENDENCIA DE CAPAS Y MATRIZ DE PERMISOS POR ESQUEMA
-- =====================================================================

-- 1. CREACIÓN DE ROLES DE BASE DE DATOS
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rol_etl') THEN 
        CREATE ROLE rol_etl LOGIN; -- contraseña: definir fuera del repositorio (ALTER ROLE)     
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rol_backend') THEN 
        CREATE ROLE rol_backend LOGIN; -- contraseña: definir fuera del repositorio (ALTER ROLE) 
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rol_powerbi') THEN 
        CREATE ROLE rol_powerbi LOGIN; -- contraseña: definir fuera del repositorio (ALTER ROLE) 
    END IF;
END
$$;

-- 2. REVOCAR ACCESOS POR DEFECTO A PUBLIC Y OTROS ROLES
REVOKE ALL ON SCHEMA cruda, limpia, analitica, seguridad FROM PUBLIC, rol_backend, rol_powerbi;
REVOKE EXECUTE ON FUNCTION analitica.calcular_cotizaciones(DATE) FROM PUBLIC;

-- 3. ASIGNAR PERMISOS AL ROL ETL (Acceso completo a capas de datos)
GRANT ALL PRIVILEGES ON SCHEMA cruda, limpia, analitica TO rol_etl;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA cruda, limpia, analitica TO rol_etl;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA cruda, limpia, analitica TO rol_etl;
GRANT EXECUTE ON FUNCTION analitica.calcular_cotizaciones(DATE) TO rol_etl;

-- 4. ASIGNAR PERMISOS A POWER BI (Lectura estricta solo en capa analítica)
GRANT USAGE ON SCHEMA analitica TO rol_powerbi;
GRANT SELECT ON ALL TABLES IN SCHEMA analitica TO rol_powerbi;

-- 5. ASIGNAR PERMISOS AL BACKEND (Lectura en analítica + operaciones de usuarios y bitácora)
GRANT USAGE ON SCHEMA analitica, seguridad TO rol_backend;
GRANT SELECT ON ALL TABLES IN SCHEMA analitica TO rol_backend;
GRANT SELECT ON seguridad.roles TO rol_backend;
GRANT SELECT, INSERT, UPDATE ON seguridad.usuarios TO rol_backend;
GRANT SELECT, INSERT, DELETE ON seguridad.entidades_seguidas TO rol_backend;
GRANT INSERT ON seguridad.auditorias TO rol_backend;

-- 6. ASIGNACIÓN DE PERMISOS POR DEFECTO PARA FUTURAS TABLAS
ALTER DEFAULT PRIVILEGES IN SCHEMA cruda, limpia, analitica GRANT ALL PRIVILEGES ON TABLES TO rol_etl;
ALTER DEFAULT PRIVILEGES IN SCHEMA cruda, limpia, analitica GRANT ALL PRIVILEGES ON SEQUENCES TO rol_etl;
ALTER DEFAULT PRIVILEGES IN SCHEMA analitica GRANT SELECT ON TABLES TO rol_powerbi, rol_backend;