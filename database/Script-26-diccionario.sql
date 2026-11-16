-- =====================================================================
-- SIFTC - HU-DD-08: endpoint de diccionario
-- Script-26: tabla analitica.diccionario (busqueda y ficha del portal).
-- Ejecutar como dueno de los objetos (postgres), despues de Script-25.
--
-- [POR QUE] La HU-DD-08 pide que cualquier usuario con sesion busque un
--           termino o metrica del diccionario de datos y abra su ficha.
--           El diccionario se mantiene en el Excel de Frell; el backend
--           solo lo carga desde diccionario.csv y lo sirve por API con
--           filtro de rol por clasificacion de sensibilidad.
--
-- Formato del CSV (seccion 2.1 del documento de la HU):
--   - separador punto y coma (;), UTF-8, fila de encabezado.
--   - 8 columnas: codigo, tipo, nombre, definicion, sinonimos,
--     formula, unidad, clasificacion.
--
-- Notas:
--   - El cargue POST /api/v1/diccionario/cargar VACIA la tabla y
--     reinserta el CSV: siempre queda igual al Excel.
--   - Restringido: solo se almacena; nadie lo ve via API (el filtro es
--     del backend, no de la BD).
--   - Idempotente: se puede ejecutar mas de una vez.
-- =====================================================================
BEGIN;

-- ---------------------------------------------------------------------
-- Tabla del diccionario (las 8 columnas del CSV, codigo como PK)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS analitica.diccionario (
    codigo TEXT NOT NULL,
    tipo TEXT NOT NULL,
    nombre TEXT NOT NULL,
    definicion TEXT NOT NULL,
    sinonimos TEXT,
    formula TEXT,
    unidad TEXT,
    clasificacion TEXT NOT NULL,
    CONSTRAINT pk_diccionario PRIMARY KEY (codigo),
    -- Seccion 2.2: dato, metrica o termino (glosario, prefijo GLO-).
    CONSTRAINT chk_diccionario_tipo
        CHECK (tipo IN ('dato', 'metrica', 'termino')),
    -- Seccion 2.4: clasificacion de sensibilidad (HU-DD-06).
    CONSTRAINT chk_diccionario_clasificacion
        CHECK (clasificacion IN ('Público', 'Interno', 'Confidencial', 'Restringido'))
);

-- La HU exige un indice sobre nombre (busqueda ILIKE de GET /diccionario).
CREATE INDEX IF NOT EXISTS ix_diccionario_nombre
    ON analitica.diccionario (nombre);

-- La busqueda tambien recorre sinonimos (contiene, distingue mayusculas = no).
CREATE INDEX IF NOT EXISTS ix_diccionario_sinonimos
    ON analitica.diccionario (sinonimos);

COMMENT ON TABLE analitica.diccionario IS
    'Diccionario de datos cargado desde diccionario.csv (HU-DD-08). El cargue reemplaza todo.';
COMMENT ON COLUMN analitica.diccionario.clasificacion IS
    'Sensibilidad: Publico/Interno ven todos; Confidencial solo Admin, Data Steward, Analista financiero y Auditor; Restringido nadie via API.';
COMMENT ON COLUMN analitica.diccionario.sinonimos IS
    'Sinonimos separados por coma; la ficha los devuelve como arreglo.';

-- El rol compartido por .NET opera la tabla (carga y lectura).
GRANT SELECT, INSERT, UPDATE, DELETE ON analitica.diccionario TO rol_backend;

COMMIT;
