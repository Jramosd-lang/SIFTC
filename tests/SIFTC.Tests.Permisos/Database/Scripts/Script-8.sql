CREATE INDEX ix_usuarios_rol            ON seguridad.usuarios (id_rol);
CREATE INDEX ix_auditorias_usuario      ON seguridad.auditorias (id_usuario);
CREATE INDEX ix_auditorias_fecha        ON seguridad.auditorias (fecha_evento);
CREATE INDEX ix_seguidas_entidad        ON seguridad.entidades_seguidas (id_entidad);
CREATE INDEX ix_lotes_usuario           ON cruda.lotes_carga (id_usuario);
CREATE INDEX ix_ext_matriz_lote         ON cruda.ext_matriz_financiera_entidades (id_lote);
CREATE INDEX ix_entidades_sector        ON limpia.tbl_entidades (id_sector);
CREATE INDEX ix_datos_fin_lote          ON limpia.tbl_datos_financieros (id_lote);
CREATE INDEX ix_datos_fin_periodo       ON limpia.tbl_datos_financieros (periodo);
CREATE INDEX ix_metricas_dimension      ON analitica.dim_metrica (id_dimension);
CREATE INDEX ix_valores_lote            ON analitica.fct_valores_metrica (id_lote);
CREATE INDEX ix_valores_metrica         ON analitica.fct_valores_metrica (id_metrica);
CREATE INDEX ix_valores_origen          ON analitica.fct_valores_metrica (id_registro_origen);
CREATE INDEX ix_indices_lote            ON analitica.fct_indices_competitividad (id_lote);
CREATE INDEX ix_indices_fecha           ON analitica.fct_indices_competitividad (fecha_carga);
	