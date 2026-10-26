-- US-11 / US-12: reporte agregado de ventas por evento del organizador.
-- Función almacenada que se consume desde VentaRepository con Dapper.
-- Solo cuentan las compras CONFIRMADAS. El periodo (desde/hasta) filtra las ventas,
-- pero entradas_vendidas_total siempre es el histórico (define la ocupación real).

CREATE OR REPLACE FUNCTION fn_ventas_por_evento(
    p_organizador_id integer,
    p_desde timestamptz DEFAULT NULL,
    p_hasta timestamptz DEFAULT NULL)
RETURNS TABLE (
    evento_id integer,
    nombre varchar,
    fecha_inicio timestamptz,
    fecha_fin timestamptz,
    sede varchar,
    precio numeric,
    aforo_total integer,
    estado varchar,
    entradas_vendidas_total integer,
    entradas_vendidas integer,
    recaudado numeric)
LANGUAGE sql
STABLE
AS $$
    SELECT e.evento_id,
           e.nombre::varchar,
           e.fecha_inicio,
           e.fecha_fin,
           e.sede::varchar,
           e.precio::numeric,
           e.aforo_total,
           e.estado::varchar,
           COALESCE(SUM(c.cantidad), 0)::integer,
           COALESCE(SUM(c.cantidad) FILTER (
               WHERE (p_desde IS NULL OR c.fecha_compra >= p_desde)
                 AND (p_hasta IS NULL OR c.fecha_compra < p_hasta)), 0)::integer,
           COALESCE(SUM(c.cantidad * c.precio_unitario) FILTER (
               WHERE (p_desde IS NULL OR c.fecha_compra >= p_desde)
                 AND (p_hasta IS NULL OR c.fecha_compra < p_hasta)), 0)::numeric
    FROM evento e
    LEFT JOIN compra c ON c.evento_id = e.evento_id AND c.estado = 'CONFIRMADA'
    WHERE e.organizador_id = p_organizador_id
    GROUP BY e.evento_id
    ORDER BY e.fecha_inicio DESC;
$$;
