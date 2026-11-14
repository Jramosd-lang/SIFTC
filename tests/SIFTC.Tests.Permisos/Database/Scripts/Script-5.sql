CREATE TABLE limpia.tbl_sectores (
    id_sector UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre VARCHAR(30) NOT NULL UNIQUE               -- antes VARCHAR(50); máx. en CSV 13 ("Agroindustria")
);

CREATE TABLE limpia.tbl_entidades (
    id_entidad UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticker VARCHAR(10) NOT NULL UNIQUE,              -- en CSV: 6 ("ENT001")
    nombre VARCHAR(80) NOT NULL,                     -- antes VARCHAR(100); máx. en CSV 28
    id_sector UUID NOT NULL REFERENCES limpia.tbl_sectores (id_sector)
);

-- Montos: antes NUMERIC(20,2) (18 dígitos enteros).
-- En el CSV el mayor valor es 972.847,64 (6 dígitos enteros).
-- NUMERIC(15,2) admite hasta 9.999.999.999.999,99 (13 dígitos enteros).
CREATE TABLE limpia.tbl_datos_financieros (
    id_dato UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    periodo DATE NOT NULL,
    utilidad_neta NUMERIC(15,2),
    patrimonio NUMERIC(15,2),
    activos_totales NUMERIC(15,2),
    pasivo_total NUMERIC(15,2),
    activo_corriente NUMERIC(15,2),
    pasivo_corriente NUMERIC(15,2),
    inventarios NUMERIC(15,2),
    ebitda NUMERIC(15,2),
    ebit NUMERIC(15,2),
    ingresos NUMERIC(15,2),
    gastos_financieros NUMERIC(15,2),
    fecha_carga TIMESTAMPTZ NOT NULL DEFAULT now(),
    id_lote UUID NOT NULL REFERENCES cruda.lotes_carga (id_lote),
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad),
    id_registro_origen UUID NOT NULL UNIQUE
        REFERENCES cruda.ext_matriz_financiera_entidades (id_cruda),
    UNIQUE (id_entidad, periodo)
);
