namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>
    /// US-13: revisa con IA el comentario de una reseña antes de publicarlo
    /// (insultos, odio, amenazas, spam, contenido sexual o datos personales).
    /// La implementación vive en la API, donde está configurado el proveedor de IA.
    /// </summary>
    public interface IModeradorResenasIA
    {
        /// <summary>¿Hay un proveedor de IA configurado?</summary>
        bool Disponible { get; }

        /// <summary>Devuelve si la reseña debe bloquearse y un motivo breve para mostrar al usuario.</summary>
        Task<(bool Bloquear, string Motivo)> Evaluar(string comentario, string eventoNombre);
    }
}
