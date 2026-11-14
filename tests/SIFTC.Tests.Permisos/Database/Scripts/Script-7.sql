CREATE TABLE seguridad.entidades_seguidas (
    id_usuario UUID NOT NULL REFERENCES seguridad.usuarios (id_usuario) ON DELETE CASCADE,
    id_entidad UUID NOT NULL REFERENCES limpia.tbl_entidades (id_entidad) ON DELETE CASCADE,
    PRIMARY KEY (id_usuario, id_entidad)
);

CREATE TABLE seguridad.auditorias (
    id_bitacora UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    accion VARCHAR(40) NOT NULL,
    detalle VARCHAR(250),
    fecha_evento TIMESTAMPTZ NOT NULL DEFAULT now(),
    id_usuario UUID REFERENCES seguridad.usuarios (id_usuario)
);