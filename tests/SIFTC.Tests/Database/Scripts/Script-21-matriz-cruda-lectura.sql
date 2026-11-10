-- =====================================================================
-- SIFTC - Matriz cruda: lectura para la API (rol_backend).
-- Solo se da SELECT sobre la tabla de la matriz. lotes_carga y el resto de cruda siguen bloqueados.
-- Ejecutar como postgres, después de Script-20.
-- =====================================================================
BEGIN;

GRANT USAGE ON SCHEMA cruda TO rol_backend;
GRANT SELECT ON cruda.ext_matriz_financiera_entidades TO rol_backend;

COMMIT;
