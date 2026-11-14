CREATE TABLE cruda.lotes_carga (
    id_lote UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre_archivo VARCHAR(150) NOT NULL,            -- antes VARCHAR(200)
    fecha_carga TIMESTAMPTZ NOT NULL DEFAULT now(),
    registros_totales INT NOT NULL DEFAULT 0,
    registros_validos INT NOT NULL DEFAULT 0,
    indice_calidad NUMERIC(5,2) GENERATED ALWAYS AS
        (ROUND(100.0 * registros_validos / NULLIF(registros_totales, 0), 2)) STORED,
    id_usuario UUID NOT NULL REFERENCES seguridad.usuarios (id_usuario),
    CHECK (registros_validos <= registros_totales)
);

-- Capa cruda: se mantiene TODO en TEXT a propósito.
-- Si aquí se limita el tamaño, una fila mala hace fallar la carga completa
-- en vez de quedar registrada y marcarse como inválida al pasar a "limpia".
-- (En PostgreSQL TEXT y VARCHAR(n) ocupan exactamente lo mismo.)
CREATE TABLE cruda.ext_matriz_financiera_entidades (
    id_cruda UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    id_lote UUID NOT NULL REFERENCES cruda.lotes_carga (id_lote),
    codigo_entidad TEXT,
    nombre_entidad TEXT,
    sector TEXT,
    periodo TEXT,
    utilidad_neta TEXT,
    patrimonio TEXT,
    activos_totales TEXT,
    ebitda TEXT,
    ingresos TEXT,
    activo_corriente TEXT,
    pasivo_corriente TEXT,
    inventarios TEXT,
    pasivo_total TEXT,
    ebit TEXT,
    gastos_financieros TEXT,
    fecha_carga TIMESTAMPTZ NOT NULL DEFAULT now()
);
