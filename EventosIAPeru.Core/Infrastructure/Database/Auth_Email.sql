-- Firebase solo valida el login: el usuario se identifica por su correo.
-- Quita la columna firebase_uid (el correo ya es único en uq_usuario_email_normalizado).
-- Cada integrante debe correrlo en su BD local después de traer estos cambios.

BEGIN;

-- Por si algún correo quedó con mayúsculas o espacios
UPDATE usuario SET email = lower(btrim(email)) WHERE email <> lower(btrim(email));

ALTER TABLE usuario DROP CONSTRAINT IF EXISTS usuario_firebase_uid_key;
ALTER TABLE usuario DROP COLUMN IF EXISTS firebase_uid;

COMMIT;
