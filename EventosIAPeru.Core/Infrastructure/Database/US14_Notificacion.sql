-- US-14: prioridad visual para notificaciones internas.
-- Ejecutar una sola vez sobre la base de datos de EventosPeruIA.

ALTER TABLE notificacion
    ADD COLUMN IF NOT EXISTS prioridad varchar(20) NOT NULL DEFAULT 'INFORMATIVO';

CREATE INDEX IF NOT EXISTS ix_notificacion_usuario_leida_fecha
    ON notificacion (usuario_id, leida, fecha_creacion DESC);

CREATE INDEX IF NOT EXISTS ix_notificacion_evento_usuario
    ON notificacion (evento_id, usuario_id);
