-- =====================================================================
-- SIFTC - D4: lectura de la bitácora de auditoría para la API (rol_backend).
-- La API registra eventos (INSERT, ya existente en Script-12) y los consulta
-- para Administrador y Auditor (SELECT). Solo lectura sobre esta tabla.
-- Ejecutar como postgres, después de Script-19.
-- =====================================================================
BEGIN;

GRANT SELECT ON seguridad.auditorias TO rol_backend;

COMMIT;
