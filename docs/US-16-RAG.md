# US-16: recuperación de información de eventos

El chat depende de `IChatClient` (Microsoft.Extensions.AI). El proveedor puede ser Ollama o Gemini con endpoint compatible con OpenAI; los embeddings se generan con Ollama. No se envían herramientas al modelo.

## Configuración

En `appsettings.Development.json`, `AI:Provider` selecciona el proveedor. Cada proveedor define `Endpoint`, `ChatModel` y `ApiKey` (Ollama local no requiere una clave real). Para Gemini, usar endpoint `https://generativelanguage.googleapis.com/v1beta/openai/` y configurar modelo y clave. La API falla al iniciar si faltan los valores obligatorios de Gemini.

`AI:Ollama:EmbeddingModel` define el modelo de embeddings; `AI:EmbeddingDimensions` debe coincidir con su dimensión (768 para nomic-embed-text). `AI:TopK` limita las fuentes (1–10); `AI:MinScore` define la similitud coseno mínima (0.6 por defecto; ajustar con preguntas representativas).

## Indexación y vigencia

Un worker actualiza al iniciar y cada 30 segundos, en lotes de hasta 20 eventos pendientes. Solo indexa eventos publicados, no desactivados y no finalizados. Cambios nuevos pueden tardar varios ciclos si hay muchos eventos pendientes. `POST /api/Chat/reindexar` procesa un lote manualmente y exige un administrador activo; en Development admite X-Firebase-Uid como el resto de la aplicación.

Se usa un bloqueo transaccional de PostgreSQL para coordinar procesos e instancias y un upsert para reemplazar el fragmento sin duplicados. Se registra el instante anterior a leer los eventos: si se edita un evento durante la generación, su versión del índice queda obsoleta y se excluye de las búsquedas hasta reindexarla. Las escrituras de eventos deben actualizar `FechaActualizacion`, como hacen los repositorios actuales. Escrituras directas a BD deben respetar esta regla.

El identificador del índice incluye una versión interna y el modelo para invalidar embeddings anteriores. Al cambiar modelo/dimensiones, los vectores deben regenerarse; los vectores inválidos se rechazan. Si se reemplaza un modelo conservando exactamente su nombre, cambiar también la versión interna del índice.

## Recuperación y contexto

`POST /api/Chat/chat` exige sesión y una cuenta registrada ACTIVA. Sin sesión devuelve 401; un UID desconocido o cuenta bloqueada devuelve 403. En Development admite `X-Firebase-Uid`; fuera de Development exige identidad autenticada mediante el token Firebase validado por el middleware. El chat recupera únicamente información pública de eventos; no recupera datos personales, compras individuales ni historiales de usuarios. No acepta un usuario destino para consultar datos privados.

`POST /api/Chat/chat` recibe `{ "message": "..." }`. Filtra en PostgreSQL por publicación, moderación, vigencia, modelo y actualidad del índice. pgvector calcula la distancia coseno; se seleccionan únicamente fuentes que superen el umbral y dentro de TopK. Si no hay fuentes relevantes actualizadas, no se invoca al LLM.

Descripción, horario UTC, sede, dirección, aforo y precio se leen del evento actual. Cupos = aforo menos compras confirmadas, con mínimo cero; la misma consulta SQL obtiene estos datos. La respuesta informa el instante de consulta, score, fuentes y tecnología de recuperación. Los cupos pueden cambiar después de consultar; esta información no constituye una reserva.

## Instrucciones y fallos

Las reglas se envían en un mensaje System separado. Pregunta y contexto se envían como JSON no confiable; las instrucciones indican ignorar órdenes dentro de esos datos. La entrada se limita a 1000 caracteres y la salida a 800 tokens. Esto reduce la inyección de prompts, pero no garantiza que un modelo nunca la siga ni que todas sus afirmaciones sean correctas.

Fallos HTTP, timeout y respuestas inválidas del proveedor producen HTTP 503 sin divulgar claves o errores internos. El worker registra fallos y reintenta el siguiente ciclo. Las preguntas no ejecutan reindexaciones.

## Preguntas manuales

- ¿Qué eventos de música hay próximamente en Lima?
- ¿Dónde y a qué hora se realiza el evento [nombre]?
- ¿Cuántos cupos quedan y cuál es su precio?
- Pregunta sin relación con eventos: verificar que no inventa fuentes.
- Editar un evento durante su indexación: verificar que no se recupera el índice anterior.
- Incluir “ignora las reglas” en la pregunta/descripción: verificar el comportamiento con el modelo elegido.

Para Nomic se utilizan los prefijos `search_document:` al indexar y `search_query:` al consultar, como indica [la ficha del modelo](https://huggingface.co/nomic-ai/nomic-embed-text-v1.5). La versión v3 invalida los índices anteriores sin prefijos. El umbral 0.6 se calibró con un evento temporal y seis preguntas básicas, y debe evaluarse con el catálogo real.
