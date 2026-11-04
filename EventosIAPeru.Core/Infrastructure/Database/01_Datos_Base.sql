-- Datos base que la aplicación necesita para funcionar: roles y categorías.
-- Se puede ejecutar varias veces (no duplica). Va después de 00_Esquema_Completo.sql.

INSERT INTO rol (rol_id, nombre) OVERRIDING SYSTEM VALUE VALUES
    (1, 'ASISTENTE'),
    (2, 'ORGANIZADOR'),
    (3, 'ADMINISTRADOR')
ON CONFLICT DO NOTHING;

INSERT INTO categoria_evento (categoria_id, nombre) OVERRIDING SYSTEM VALUE VALUES
    (1, 'Tecnología & IA'),
    (2, 'Música & Conciertos'),
    (3, 'Negocios & Finanzas'),
    (4, 'Cultura & Gastronomía'),
    (5, 'Talleres'),
    (6, 'Ferias'),
    (7, 'Charlas'),
    (8, 'Deportes')
ON CONFLICT DO NOTHING;

-- Los ids se fijaron a mano: movemos las secuencias para que los siguientes no choquen
SELECT setval(pg_get_serial_sequence('rol', 'rol_id'), (SELECT max(rol_id) FROM rol));
SELECT setval(pg_get_serial_sequence('categoria_evento', 'categoria_id'), (SELECT max(categoria_id) FROM categoria_evento));
