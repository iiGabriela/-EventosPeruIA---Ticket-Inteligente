-- Datos de prueba para la demo: organizadores, administrador, asistentes, eventos y compras.
-- Las fechas se calculan desde el día en que se ejecuta (hora de Perú): los eventos siempre quedan próximos.
-- Se puede ejecutar varias veces: si ya se cargó, no hace nada.
-- Requisitos: esquema completo y categorías/roles base (ids 1-8 y ASISTENTE/ORGANIZADOR/ADMINISTRADOR).
-- Nota: estos usuarios solo existen en la BD. Para entrar desde el frontend hay que crearlos también en
-- Firebase con el mismo correo; en Development se puede usar el header X-User-Email.

DO $$
DECLARE
    hoy        date := (now() AT TIME ZONE 'America/Lima')::date;
    sabado     date;
    org_lima   int;
    org_cusco  int;
    org_dario  int;
    asistente  int;
    asistente2 int;
    admin_id   int;
    ev         int;
    compra     int;
    precio_unit numeric;
    r          record;
    rol_asis   int := (SELECT rol_id FROM rol WHERE nombre = 'ASISTENTE');
    rol_org    int := (SELECT rol_id FROM rol WHERE nombre = 'ORGANIZADOR');
    rol_admin  int := (SELECT rol_id FROM rol WHERE nombre = 'ADMINISTRADOR');
BEGIN
    IF EXISTS (SELECT 1 FROM usuario WHERE email = 'organizadora.lima@eventia.pe') THEN
        RAISE NOTICE 'Los datos de prueba ya estaban cargados.';
        RETURN;
    END IF;

    -- Próximo sábado (o hoy si ya es sábado)
    sabado := hoy + ((6 - EXTRACT(ISODOW FROM hoy)::int + 7) % 7);

    -- ---------------------------- Usuarios ----------------------------
    INSERT INTO usuario (nombre, email, dni, estado, verificado)
    VALUES ('Productora Lima Viva', 'organizadora.lima@eventia.pe', '40000001', 'ACTIVO', true) RETURNING usuario_id INTO org_lima;
    INSERT INTO usuario (nombre, email, dni, estado, verificado)
    VALUES ('Cusco Cultural', 'organizador.cusco@eventia.pe', '40000002', 'ACTIVO', true) RETURNING usuario_id INTO org_cusco;
    INSERT INTO usuario (nombre, email, dni, estado, verificado)
    VALUES ('Administrador Eventia', 'admin@eventia.pe', '40000003', 'ACTIVO', true) RETURNING usuario_id INTO admin_id;
    INSERT INTO usuario (nombre, email, dni, estado, verificado)
    VALUES ('Lucía Ramos', 'asistente.demo@eventia.pe', '40000004', 'ACTIVO', false) RETURNING usuario_id INTO asistente;
    INSERT INTO usuario (nombre, email, dni, estado, verificado)
    VALUES ('Carlos Medina', 'asistente2.demo@eventia.pe', '40000005', 'ACTIVO', false) RETURNING usuario_id INTO asistente2;

    INSERT INTO usuario_rol (usuario_id, rol_id) VALUES
        (org_lima, rol_asis), (org_lima, rol_org),
        (org_cusco, rol_asis), (org_cusco, rol_org),
        (admin_id, rol_asis), (admin_id, rol_admin),
        (asistente, rol_asis), (asistente2, rol_asis);

    -- Si existe la cuenta de Dario (organizador del grupo), también le damos eventos con ventas
    SELECT usuario_id INTO org_dario FROM usuario WHERE email = 'floresdario807@gmail.com';
    IF org_dario IS NULL THEN org_dario := org_lima; END IF;
    INSERT INTO usuario_rol (usuario_id, rol_id) VALUES (org_dario, rol_org) ON CONFLICT DO NOTHING;

    -- Intereses (señal para las recomendaciones de US-10)
    INSERT INTO usuario_interes (usuario_id, categoria_id) VALUES (asistente, 2), (asistente, 4), (asistente2, 1), (asistente2, 3);

    -- ---------------------------- Eventos ----------------------------
    -- Fechas en hora de Perú convertidas a UTC con AT TIME ZONE.
    CREATE TEMP TABLE seed_evento (
        clave text, organizador int, categoria int, nombre text, descripcion text,
        inicio timestamptz, horas int, sede text, direccion text, departamento text, provincia text, distrito text,
        aforo int, precio numeric, estado text, moderacion text
    ) ON COMMIT DROP;

    INSERT INTO seed_evento VALUES
    ('rock',   org_lima,  2, 'Noche de Rock Peruano', 'Concierto en vivo con bandas de rock nacional, zona de food trucks y merchandising oficial.',
        (sabado + time '20:00') AT TIME ZONE 'America/Lima', 4, 'Explanada Costa Verde', 'Circuito de Playas s/n', 'Lima', 'Lima', 'Magdalena del Mar', 1500, 85, 'PUBLICADO', 'CONFORME'),
    ('feria',  org_lima,  4, 'Feria Gastronómica Sabores del Perú', 'Más de 40 puestos de comida regional, show de cocina en vivo y música criolla para toda la familia.',
        (sabado + time '11:00') AT TIME ZONE 'America/Lima', 8, 'Parque de la Exposición', 'Av. 28 de Julio s/n', 'Lima', 'Lima', 'Lima', 3000, 0, 'PUBLICADO', 'CONFORME'),
    ('yoga',   org_lima,  8, 'Yoga al Amanecer en la Costa Verde', 'Sesión de yoga para todos los niveles frente al mar. Trae tu propio mat.',
        (sabado + 1 + time '07:00') AT TIME ZONE 'America/Lima', 2, 'Parque Raimondi', 'Malecón Cisneros 1200', 'Lima', 'Lima', 'Miraflores', 80, 25, 'PUBLICADO', 'CONFORME'),
    ('ia',     org_dario, 1, 'Summit de Inteligencia Artificial Lima', 'Charlas sobre LLMs, RAG y agentes de IA aplicados a empresas peruanas, con talleres prácticos.',
        (hoy + 10 + time '09:00') AT TIME ZONE 'America/Lima', 8, 'Centro de Convenciones de Lima', 'Av. Arqueología 206', 'Lima', 'Lima', 'San Borja', 400, 120, 'PUBLICADO', 'CONFORME'),
    ('startup',org_dario, 3, 'Startup Pitch Night', 'Emprendedores presentan sus proyectos ante inversionistas. Networking al final del evento.',
        (hoy + 4 + time '19:00') AT TIME ZONE 'America/Lima', 3, 'Cowork Barranco', 'Av. Grau 340', 'Lima', 'Lima', 'Barranco', 4, 30, 'PUBLICADO', 'CONFORME'),
    ('web',    org_dario, 5, 'Taller de Vue.js y Quasar desde cero', 'Taller práctico para construir una SPA con Vue 3 y Quasar Framework consumiendo una API REST.',
        (hoy + 14 + time '15:00') AT TIME ZONE 'America/Lima', 4, 'Universidad ESAN', 'Alonso de Molina 1652', 'Lima', 'Lima', 'Santiago de Surco', 60, 40, 'PUBLICADO', 'CONFORME'),
    ('jazz',   org_lima,  2, 'Jazz bajo las estrellas', 'Noche de jazz y bossa nova en un ambiente al aire libre con vista al mar.',
        (hoy + 6 + time '20:30') AT TIME ZONE 'America/Lima', 3, 'Anfiteatro del Parque Salazar', 'Malecón de la Reserva 610', 'Lima', 'Lima', 'Miraflores', 250, 60, 'PUBLICADO', 'CONFORME'),
    ('finanzas', org_lima, 7, 'Charla: Finanzas personales para jóvenes', 'Aprende a crear un presupuesto, ahorrar e invertir tus primeros soles. Ingreso libre con registro.',
        (hoy + 3 + time '18:00') AT TIME ZONE 'America/Lima', 2, 'Biblioteca Nacional del Perú', 'Av. De la Poesía 160', 'Lima', 'Lima', 'San Borja', 120, 0, 'PUBLICADO', 'CONFORME'),
    ('inti',   org_cusco, 4, 'Festival Cultural Andino', 'Danzas tradicionales, artesanía y gastronomía andina en la plaza principal.',
        (hoy + 12 + time '10:00') AT TIME ZONE 'America/Lima', 9, 'Plaza de Armas del Cusco', 'Plaza de Armas s/n', 'Cusco', 'Cusco', 'Cusco', 2000, 15, 'PUBLICADO', 'CONFORME'),
    ('trek',   org_cusco, 8, 'Trekking a la Montaña de Colores', 'Caminata guiada de día completo con transporte y desayuno incluidos.',
        (hoy + 9 + time '04:00') AT TIME ZONE 'America/Lima', 14, 'Punto de encuentro: Plaza San Francisco', 'Calle Santa Clara s/n', 'Cusco', 'Cusco', 'Cusco', 30, 150, 'PUBLICADO', 'CONFORME'),
    ('arequipa', org_cusco, 6, 'Feria del Libro de Arequipa', 'Presentaciones de libros, firmas de autores y talleres de escritura creativa.',
        (hoy + 20 + time '10:00') AT TIME ZONE 'America/Lima', 10, 'Centro Cultural UNSA', 'Calle Santa Catalina 101', 'Arequipa', 'Arequipa', 'Arequipa', 800, 10, 'PUBLICADO', 'CONFORME'),
    ('marinera', org_cusco, 2, 'Concurso de Marinera Norteña', 'Parejas de todo el norte compiten en el tradicional concurso de marinera.',
        (hoy + 25 + time '16:00') AT TIME ZONE 'America/Lima', 5, 'Coliseo Gran Chimú', 'Av. Mansiche s/n', 'La Libertad', 'Trujillo', 'Trujillo', 1200, 35, 'PUBLICADO', 'EN_REVISION'),
    ('borrador', org_dario, 5, 'Taller de fotografía con celular', 'Borrador: aún falta confirmar la sede.',
        (hoy + 30 + time '10:00') AT TIME ZONE 'America/Lima', 3, 'Por confirmar', 'Por confirmar', 'Lima', 'Lima', 'Miraflores', 25, 50, 'BORRADOR', 'EN_REVISION');

    -- Se insertan uno por uno para guardar el id de cada evento por su clave
    CREATE TEMP TABLE seed_ids (clave text, evento_id int, precio numeric) ON COMMIT DROP;
    FOR r IN SELECT * FROM seed_evento LOOP
        INSERT INTO evento (organizador_id, categoria_id, nombre, descripcion, fecha_inicio, fecha_fin, sede, direccion,
                            departamento, provincia, distrito, aforo_total, precio, estado, estado_moderacion,
                            fecha_creacion, fecha_actualizacion)
        VALUES (r.organizador, r.categoria, r.nombre, r.descripcion, r.inicio, r.inicio + make_interval(hours => r.horas),
                r.sede, r.direccion, r.departamento, r.provincia, r.distrito, r.aforo, r.precio, r.estado, r.moderacion,
                now() - interval '20 days', now())
        RETURNING evento_id INTO ev;
        INSERT INTO seed_ids VALUES (r.clave, ev, r.precio);
    END LOOP;

    -- ---------------------------- Compras y entradas ----------------------------
    -- Una entrada (con su QR) por cada unidad comprada; máximo 4 por compra.
    -- "startup" queda AGOTADO (aforo 4, se venden 4).
    FOR r IN SELECT * FROM (VALUES
        ('startup',  asistente,  4, 'YAPE',    2),
        ('ia',       asistente,  2, 'TARJETA', 9),
        ('ia',       asistente2, 3, 'PLIN',    5),
        ('ia',       asistente,  1, 'YAPE',    1),
        ('web',      asistente2, 2, 'TARJETA', 3),
        ('rock',     asistente,  4, 'TARJETA', 12),
        ('rock',     asistente2, 2, 'YAPE',    6),
        ('jazz',     asistente2, 2, 'PLIN',    4),
        ('yoga',     asistente,  1, 'YAPE',    2),
        ('inti',     asistente2, 3, 'TARJETA', 7),
        ('trek',     asistente,  2, 'TARJETA', 8)
    ) AS v(clave, usuario, cantidad, metodo, dias) LOOP
        SELECT evento_id, precio INTO ev, precio_unit FROM seed_ids WHERE clave = r.clave;

        INSERT INTO compra (usuario_id, evento_id, codigo_operacion, cantidad, precio_unitario, metodo_pago, estado, fecha_compra)
        VALUES (r.usuario, ev, gen_random_uuid(), r.cantidad, precio_unit, r.metodo, 'CONFIRMADA',
                now() - make_interval(days => r.dias))
        RETURNING compra_id INTO compra;

        INSERT INTO entrada (compra_id, codigo_qr, titular_nombre, titular_documento, tipo_acceso, estado, fecha_generacion)
        SELECT compra,
               upper(md5(random()::text) || md5(random()::text)),
               CASE WHEN g = 1 THEN (SELECT nombre FROM usuario WHERE usuario_id = r.usuario) ELSE 'Acompañante ' || g END,
               CASE WHEN g = 1 THEN (SELECT dni FROM usuario WHERE usuario_id = r.usuario) ELSE lpad((70000000 + compra * 10 + g)::text, 8, '0') END,
               'GENERAL', 'VALIDA', now() - make_interval(days => r.dias)
        FROM generate_series(1, r.cantidad) AS g;
    END LOOP;

    RAISE NOTICE 'Datos de prueba cargados: 5 usuarios, 13 eventos y 11 compras.';
END $$;
