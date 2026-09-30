using System;
using System.Collections.Generic;
using Pgvector;

namespace EventosIAPeru.Core.Core.Entities;

public partial class EventoEmbedding
{
    public int EmbeddingId { get; set; }

    public int EventoId { get; set; }

    public int NumeroFragmento { get; set; }

    public string Contenido { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public Vector Embedding { get; set; } = null!;

    public DateTime FechaIndexacion { get; set; }

    public virtual Evento Evento { get; set; } = null!;
}
