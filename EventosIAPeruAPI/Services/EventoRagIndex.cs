using System.Net.Http.Json;
using System.Text.Json;
using EventosIAPeru.Core.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace EventosIAPeru.API.Services;

/// <summary>
/// US-16 / US-17: genera embeddings y actualiza el índice de eventos publicados en pgvector.
/// El proveedor se elige en AI:Provider: "Gemini" (en la nube, sin instalar nada) u "Ollama" (local).
/// El contenido del evento permite buscar por significado; los cupos se consultan en vivo al responder.
/// </summary>
public sealed class EventoRagIndex(EventosPeruIAContext db, IHttpClientFactory clients, IConfiguration config)
{
    private bool UsaGemini => string.Equals(config["AI:Provider"], "Gemini", StringComparison.OrdinalIgnoreCase);
    private int Dimensiones => config.GetValue("AI:EmbeddingDimensions", 768);

    public string Model => UsaGemini
        ? config["AI:Gemini:EmbeddingModel"] ?? "gemini-embedding-001"
        : config["AI:Ollama:EmbeddingModel"] ?? "nomic-embed-text";

    // La versión invalida vectores anteriores; al cambiar de proveedor o modelo el índice se regenera solo.
    public string IndexModel => $"us16-v3:{Model}";

    /// <summary>Con Gemini hace falta la API key; con Ollama basta con que esté iniciado.</summary>
    public bool Configurado => !UsaGemini || !string.IsNullOrWhiteSpace(config["AI:Gemini:ApiKey"]);

    // ---------------------------- US-16: embeddings ----------------------------

    /// <summary>Convierte un evento o una pregunta en un vector del modelo configurado.</summary>
    public async Task<Vector> Embed(string text, CancellationToken ct, bool document = false)
    {
        var values = UsaGemini ? await EmbedGemini(text, document, ct) : await EmbedOllama(text, document, ct);

        // Los modelos de Gemini admiten recortar el vector (Matryoshka): si llega más largo, usamos el prefijo.
        if (values.Length > Dimensiones) values = values[..Dimensiones];

        // Rechazamos respuestas vacías o incompatibles antes de guardarlas o compararlas.
        if (values.Length != Dimensiones || values.Any(x => !float.IsFinite(x)) || values.All(x => x == 0))
            throw new InvalidDataException("Embedding inválido o dimensiones incompatibles.");
        return new Vector(values);
    }

    private async Task<float[]> EmbedOllama(string text, bool document, CancellationToken ct)
    {
        // Nomic diferencia el texto a indexar (document) de la pregunta que se quiere buscar (query).
        using var response = await clients.CreateClient("embeddings").PostAsJsonAsync(
            config["AI:Ollama:EmbeddingEndpoint"] ?? "http://localhost:11434/api/embeddings",
            new { model = Model, prompt = (Model.StartsWith("nomic-embed-text", StringComparison.OrdinalIgnoreCase)
                ? (document ? "search_document: " : "search_query: ") : "") + text }, ct);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("embedding", out var array))
            throw new InvalidDataException("Embedding ausente.");
        return LeerNumeros(array);
    }

    private async Task<float[]> EmbedGemini(string text, bool document, CancellationToken ct)
    {
        var apiKey = config["AI:Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new HttpRequestException("Falta configurar AI:Gemini:ApiKey.");

        var baseUrl = (config["AI:Gemini:EmbeddingEndpoint"] ?? "https://generativelanguage.googleapis.com/v1beta").TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/models/{Model}:embedContent");
        // La clave viaja en un header, nunca en la URL (las URL quedan en logs).
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            model = $"models/{Model}",
            content = new { parts = new[] { new { text } } },
            // Gemini también distingue el texto que se indexa de la pregunta con la que se busca.
            taskType = document ? "RETRIEVAL_DOCUMENT" : "RETRIEVAL_QUERY",
            outputDimensionality = Dimensiones
        });

        using var response = await clients.CreateClient("embeddings").SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (json.RootElement.ValueKind != JsonValueKind.Object
            || !json.RootElement.TryGetProperty("embedding", out var embedding)
            || embedding.ValueKind != JsonValueKind.Object
            || !embedding.TryGetProperty("values", out var array))
            throw new InvalidDataException("Embedding ausente.");
        return LeerNumeros(array);
    }

    private static float[] LeerNumeros(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Embedding ausente.");
        return array.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.Number && x.TryGetSingle(out var n)
            ? n : throw new InvalidDataException("Embedding no numérico.")).ToArray();
    }

    // ---------------------------- US-16: reindexación ----------------------------

    /// <summary>Actualiza hasta 20 eventos nuevos o modificados. Devuelve cuántos procesó.</summary>
    public async Task<int> Refresh(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Solo un proceso puede indexar a la vez; PostgreSQL libera el bloqueo al terminar la transacción.
        if (!await db.Database.SqlQueryRaw<bool>("SELECT pg_try_advisory_xact_lock(160016) AS \"Value\"").SingleAsync(ct)) return 0;
        // Tomamos la fecha antes de leer: una edición posterior dejará este vector pendiente de actualizar.
        var snapshot = DateTime.UtcNow;
        // Solo procesamos eventos visibles y vigentes que aún no tienen un índice actualizado del mismo modelo.
        var events = await db.Evento.AsNoTracking().Include(e => e.Categoria)
            .Where(e => e.Estado == "PUBLICADO" && e.EstadoModeracion != "DESACTIVADO" && e.FechaFin > snapshot
                && !db.EventoEmbedding.Any(x => x.EventoId == e.EventoId && x.NumeroFragmento == 0
                    && x.Modelo == IndexModel && x.FechaIndexacion >= e.FechaActualizacion))
            .OrderBy(e => e.EventoId).Take(20).ToListAsync(ct);
        foreach (var ev in events)
        {
            var text = $"{ev.Nombre}. {ev.Categoria.Nombre}. {ev.Descripcion}. Horario UTC: {ev.FechaInicio:O} a {ev.FechaFin:O}. {ev.Sede}, {ev.Direccion}, {ev.Distrito}, {ev.Provincia}, {ev.Departamento}. Aforo: {ev.AforoTotal}. Precio: {ev.Precio}.";
            var vector = await Embed(text, ct, document: true);
            // Inserta el fragmento o reemplaza el existente sin crear duplicados por evento.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO evento_embedding (evento_id, numero_fragmento, contenido, modelo, embedding, fecha_indexacion) VALUES ({ev.EventoId}, 0, {text}, {IndexModel}, {vector}, {snapshot}) ON CONFLICT (evento_id, numero_fragmento) DO UPDATE SET contenido=EXCLUDED.contenido, modelo=EXCLUDED.modelo, embedding=EXCLUDED.embedding, fecha_indexacion=EXCLUDED.fecha_indexacion", ct);
        }
        await tx.CommitAsync(ct);
        return events.Count;
    }
}
