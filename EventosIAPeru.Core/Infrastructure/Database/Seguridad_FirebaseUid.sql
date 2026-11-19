-- Seguridad (US-02): la cuenta se identifica por el UID de Firebase, no solo por el correo.
-- Aplicar sobre una base que ya existía. En una base nueva ya viene en 00_Esquema_Completo.sql.

ALTER TABLE public.usuario
    ADD COLUMN IF NOT EXISTS firebase_uid character varying(128);

-- Un UID de Firebase corresponde a un solo perfil
CREATE UNIQUE INDEX IF NOT EXISTS uq_usuario_firebase_uid
    ON public.usuario USING btree (firebase_uid) WHERE (firebase_uid IS NOT NULL);

-- Índices que faltaban para consultas frecuentes
CREATE INDEX IF NOT EXISTS ix_evento_organizador ON public.evento USING btree (organizador_id);
CREATE INDEX IF NOT EXISTS ix_resena_evento ON public.resena_evento USING btree (evento_id);
CREATE INDEX IF NOT EXISTS ix_mensaje_chat_conversacion ON public.mensaje_chat USING btree (conversacion_id, fecha);
CREATE INDEX IF NOT EXISTS ix_conversacion_chat_usuario ON public.conversacion_chat USING btree (usuario_id, fecha_inicio DESC);
CREATE INDEX IF NOT EXISTS ix_reporte_evento_regla ON public.reporte_evento USING btree (evento_id, regla_id) WHERE (regla_id IS NOT NULL);

-- La función de ventas no debe depender del search_path de quien la llama
ALTER FUNCTION public.fn_ventas_por_evento(integer, timestamp with time zone, timestamp with time zone)
    SET search_path = public;

-- Cómo enlazar a mano un perfil creado por script (por ejemplo, un usuario de 99_Datos_Prueba.sql)
-- con su cuenta de Firebase. El UID se copia de Firebase Console -> Authentication -> Usuarios:
--
--   UPDATE public.usuario SET firebase_uid = '<UID de Firebase>' WHERE email = 'correo@ejemplo.com';
--
-- Cómo dar el rol de administrador a una cuenta ya registrada desde el frontend:
--
--   INSERT INTO public.usuario_rol (usuario_id, rol_id)
--   SELECT u.usuario_id, r.rol_id FROM public.usuario u, public.rol r
--   WHERE u.email = 'correo@ejemplo.com' AND r.nombre = 'ADMINISTRADOR'
--   ON CONFLICT DO NOTHING;
