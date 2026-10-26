-- US-01: el registro pide DNI (8 dígitos) y debe ser único.
-- Los usuarios que ya existían quedan con DNI vacío (NULL).

ALTER TABLE usuario ADD COLUMN IF NOT EXISTS dni varchar(8);

ALTER TABLE usuario DROP CONSTRAINT IF EXISTS usuario_dni_formato_check;
ALTER TABLE usuario ADD CONSTRAINT usuario_dni_formato_check CHECK (dni IS NULL OR dni ~ '^[0-9]{8}$');

CREATE UNIQUE INDEX IF NOT EXISTS uq_usuario_dni ON usuario (dni) WHERE dni IS NOT NULL;
