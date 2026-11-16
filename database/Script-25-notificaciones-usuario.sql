-- =====================================================================
-- SIFTC - HU-AD-10 / HU-MO-05: notificaciones a usuario
-- Script-25: reglas por usuario, tokens FCM y cola de envios.
-- Ejecutar como dueno de los objetos (postgres), despues de Script-22.
--
-- [POR QUE] La HU exige (a) reglas configurables por usuario con canal,
--           entidades y umbral (CA2) y (b) reintentos y fallos
--           registrados (CA3). El envio es correo SMTP + push Firebase
--           Cloud Messaging, en menos de 1 minuto (CA1).
--
-- Relacion con analitica.notificaciones_aprobacion (Script-19):
--   esa bandeja guarda eventos de rechazo de lotes; el worker de esta HU
--   puede consumirla y traducirla en filas de notificaciones_envios.
--
-- Notas:
--   - notificaciones_envios es a la vez cola y log (estado + intentos +
--     ultimo_error), asi CA3 no necesita una tabla extra.
--   - Los tokens FCM son sensibles: no se incluyen en bitacoras ni logs.
--   - Idempotente: se puede ejecutar mas de una vez.
-- =====================================================================
BEGIN;

-- ---------------------------------------------------------------------
-- CA2: reglas configurables por usuario (canal, entidades y umbral)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS seguridad.notificaciones_reglas (
    id_regla UUID NOT NULL DEFAULT gen_random_uuid(),
    id_usuario UUID NOT NULL,
    canal VARCHAR(10) NOT NULL,
    id_entidad UUID,
    umbral NUMERIC(8, 4),
    activa BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_actualizacion TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT pk_notificaciones_reglas PRIMARY KEY (id_regla),
    CONSTRAINT chk_notificaciones_reglas_canal
        CHECK (canal IN ('correo', 'push', 'ambos')),
    -- Umbral en porcentaje de variacion; NULL = avisar de cualquier cambio.
    CONSTRAINT chk_notificaciones_reglas_umbral
        CHECK (umbral IS NULL OR umbral >= 0),
    CONSTRAINT fk_notificaciones_reglas_usuario
        FOREIGN KEY (id_usuario) REFERENCES seguridad.usuarios (id_usuario)
        ON DELETE CASCADE,
    CONSTRAINT fk_notificaciones_reglas_entidad
        FOREIGN KEY (id_entidad) REFERENCES limpia.tbl_entidades (id_entidad)
        ON DELETE CASCADE
);

-- id_entidad NULL = todas las entidades; sin esto el UNIQUE no cubre NULL.
CREATE UNIQUE INDEX IF NOT EXISTS uq_notificaciones_reglas
    ON seguridad.notificaciones_reglas (
        id_usuario,
        canal,
        COALESCE(id_entidad, '00000000-0000-0000-0000-000000000000'::uuid)
    );

CREATE INDEX IF NOT EXISTS ix_notificaciones_reglas_usuario
    ON seguridad.notificaciones_reglas (id_usuario)
    WHERE activa;

COMMENT ON TABLE seguridad.notificaciones_reglas IS
    'Reglas de aviso por usuario: canal (correo/push/ambos), entidad (NULL = todas) y umbral. CA2.';
COMMENT ON COLUMN seguridad.notificaciones_reglas.umbral IS
    'Variacion minima en porcentaje que dispara el aviso; NULL = cualquier cambio.';

-- ---------------------------------------------------------------------
-- Tokens de dispositivos para FCM
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS seguridad.notificaciones_tokens (
    id_token UUID NOT NULL DEFAULT gen_random_uuid(),
    id_usuario UUID NOT NULL,
    plataforma VARCHAR(10) NOT NULL,
    token_fcm TEXT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_registro TIMESTAMPTZ NOT NULL DEFAULT now(),
    ultimo_uso TIMESTAMPTZ,
    CONSTRAINT pk_notificaciones_tokens PRIMARY KEY (id_token),
    CONSTRAINT chk_notificaciones_tokens_plataforma
        CHECK (plataforma IN ('web', 'android', 'ios')),
    CONSTRAINT uq_notificaciones_tokens UNIQUE (token_fcm),
    CONSTRAINT fk_notificaciones_tokens_usuario
        FOREIGN KEY (id_usuario) REFERENCES seguridad.usuarios (id_usuario)
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_notificaciones_tokens_activos
    ON seguridad.notificaciones_tokens (id_usuario)
    WHERE activo;

COMMENT ON TABLE seguridad.notificaciones_tokens IS
    'Tokens FCM de los dispositivos del usuario (web/android). No registrar en bitacoras.';

-- ---------------------------------------------------------------------
-- CA3: cola de envios con reintentos y fallos registrados
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS seguridad.notificaciones_envios (
    id_envio UUID NOT NULL DEFAULT gen_random_uuid(),
    id_usuario UUID NOT NULL,
    id_regla UUID,
    canal VARCHAR(10) NOT NULL,
    asunto VARCHAR(200) NOT NULL,
    cuerpo TEXT NOT NULL,
    estado VARCHAR(12) NOT NULL DEFAULT 'pendiente',
    intentos INTEGER NOT NULL DEFAULT 0,
    max_intentos INTEGER NOT NULL DEFAULT 3,
    ultimo_error VARCHAR(500),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_ultimo_intento TIMESTAMPTZ,
    fecha_envio TIMESTAMPTZ,
    CONSTRAINT pk_notificaciones_envios PRIMARY KEY (id_envio),
    CONSTRAINT chk_notificaciones_envios_estado
        CHECK (estado IN ('pendiente', 'enviado', 'fallido')),
    -- Un envio es correo o push; si la regla dice 'ambos' se crean dos filas.
    CONSTRAINT chk_notificaciones_envios_canal
        CHECK (canal IN ('correo', 'push')),
    CONSTRAINT chk_notificaciones_envios_intentos
        CHECK (intentos >= 0 AND intentos <= max_intentos),
    CONSTRAINT fk_notificaciones_envios_usuario
        FOREIGN KEY (id_usuario) REFERENCES seguridad.usuarios (id_usuario),
    CONSTRAINT fk_notificaciones_envios_regla
        FOREIGN KEY (id_regla) REFERENCES seguridad.notificaciones_reglas (id_regla)
        ON DELETE SET NULL
);

-- El worker toma los pendientes cuyo proximo reintento ya vencio.
CREATE INDEX IF NOT EXISTS ix_notificaciones_envios_cola
    ON seguridad.notificaciones_envios (fecha_ultimo_intento)
    WHERE estado = 'pendiente';
CREATE INDEX IF NOT EXISTS ix_notificaciones_envios_usuario
    ON seguridad.notificaciones_envios (id_usuario);
CREATE INDEX IF NOT EXISTS ix_notificaciones_envios_fecha
    ON seguridad.notificaciones_envios (fecha_creacion);

COMMENT ON TABLE seguridad.notificaciones_envios IS
    'Cola y log de notificaciones: estado, intentos y ultimo_error. CA3.';
COMMENT ON COLUMN seguridad.notificaciones_envios.estado IS
    'pendiente (en cola), enviado (exito) o fallido (agoto max_intentos).';
COMMENT ON COLUMN seguridad.notificaciones_envios.ultimo_error IS
    'Ultimo error SMTP/FCM; trazabilidad de reintentos (maximo 500 caracteres).';

-- El rol compartido por .NET y Python opera la cola.
GRANT SELECT, INSERT, UPDATE, DELETE ON seguridad.notificaciones_reglas TO rol_backend;
GRANT SELECT, INSERT, UPDATE, DELETE ON seguridad.notificaciones_tokens TO rol_backend;
GRANT SELECT, INSERT, UPDATE, DELETE ON seguridad.notificaciones_envios TO rol_backend;

COMMIT;
