CREATE TABLE analitica.dim_dimension (
    id_dimension UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre VARCHAR(30) NOT NULL UNIQUE,              -- antes VARCHAR(40); máx. actual 12
    peso_ic NUMERIC(4,3) NOT NULL DEFAULT 0 CHECK (peso_ic BETWEEN 0 AND 1)
);

CREATE TABLE analitica.dim_metrica (
    id_metrica UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    codigo VARCHAR(10) NOT NULL UNIQUE,
    nombre VARCHAR(50) NOT NULL,                     -- antes VARCHAR(80); máx. actual 26
    formula VARCHAR(120) NOT NULL,                   -- antes VARCHAR(200); máx. actual 51
    frecuencia VARCHAR(12) NOT NULL
        CHECK (frecuencia IN ('Diaria', 'Mensual', 'Trimestral')),
    id_dimension UUID NOT NULL REFERENCES analitica.dim_dimension (id_dimension)
);

-- valor: antes NUMERIC(20,6) (14 dígitos enteros).
-- Con el CSV las razones van de -0,16 a 6,26. NUMERIC(14,6) deja 8 dígitos
-- enteros, suficiente incluso si una cobertura de intereses se dispara
-- por gastos financieros casi en cero.
CREATE TABLE analitica.fct_valores_metrica (
    id_valor UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    periodo DATE NOT NULL,
    valor NUMERIC(14,6),
    valor_normalizado NUMERIC(9,8) CHECK (valor_normalizado BETWEEN 0 AND 1),
    fecha_carga TIMESTAMPTZ NOT NULL DEFAULT now(),
    id_lote UUID NOT NULL REFERENCES cruda.lotes_carga (id_lote),
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad),
    id_metrica UUID NOT NULL REFERENCES analitica.dim_metrica (id_metrica),
    id_registro_origen UUID REFERENCES limpia.tbl_datos_financieros (id_dato),
    UNIQUE (id_entidad, id_metrica, periodo)
);

CREATE TABLE analitica.fct_indices_competitividad (
    id_indice UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fecha_carga TIMESTAMPTZ NOT NULL DEFAULT now(),
    ic NUMERIC(6,3) NOT NULL CHECK (ic BETWEEN 0 AND 100),
    id_lote UUID NOT NULL REFERENCES cruda.lotes_carga (id_lote),
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad),
    UNIQUE (id_entidad, id_lote)
);

CREATE TABLE analitica.fct_cotizaciones (
    id_cotizacion UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fecha DATE NOT NULL,
    apertura NUMERIC(6,3) NOT NULL,
    maximo NUMERIC(6,3) NOT NULL,
    minimo NUMERIC(6,3) NOT NULL,
    cierre NUMERIC(6,3) NOT NULL,
    variacion_pct NUMERIC(8,4),
    volumen_actualizaciones INT NOT NULL DEFAULT 0,
    fecha_carga TIMESTAMPTZ NOT NULL DEFAULT now(),
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad),
    UNIQUE (id_entidad, fecha),
    CHECK (minimo <= LEAST(apertura, cierre) AND maximo >= GREATEST(apertura, cierre))
);
