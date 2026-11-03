# US-10: recomendaciones personalizadas

## Señales empleadas

El sistema calcula la afinidad por categoría usando dos señales:

- Categoría marcada explícitamente como interés: 5 puntos.
- Categoría de una compra confirmada realizada durante los últimos 6 meses: 3 puntos.

Si una categoría cumple ambas señales, acumula 8 puntos.

El historial de búsquedas individuales no se utiliza todavía porque el modelo actual solo registra consultas globales por categoría en `categoria_evento.total_consultas`. Se podrá agregar como señal posterior cuando exista una tabla de consultas por usuario.

## Filtros y orden

Solo se consideran eventos `PUBLICADO`, no desactivados por moderación, futuros y no finalizados. Los eventos que el usuario ya compró se excluyen de las recomendaciones.

El orden aplicado es:

1. Eventos con cupos disponibles.
2. Mayor puntaje de afinidad.
3. Mayor cantidad de entradas vendidas confirmadas.
4. Fecha de inicio más próxima.

El endpoint devuelve seis eventos por defecto y permite solicitar hasta veinte.

## Alternativa sin historial

Cuando el usuario no tiene intereses, compras recientes o coincidencias suficientes, el sistema completa la respuesta con eventos próximos y populares. La respuesta informa si la lista es personalizada mediante `personalizadas` y explica el criterio en `criterio`.

## Endpoints

- `GET /api/Evento/recomendados?cantidad=6`
- `GET /api/Usuario/me/intereses`
- `PUT /api/Usuario/me/intereses`
