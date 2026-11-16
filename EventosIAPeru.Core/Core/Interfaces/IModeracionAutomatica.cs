using EventosIAPeru.Core.Core.Entities;

namespace EventosIAPeru.Core.Core.Interfaces
{
    /// <summary>
    /// US-15: aplica las reglas automáticas de moderación a un evento publicado o editado.
    /// </summary>
    public interface IModeracionAutomatica
    {
        /// <summary>Evalúa las reglas activas. Devuelve cuántas reglas incumple el evento.</summary>
        Task<int> EvaluarEvento(int eventoId);
    }

    /// <summary>
    /// US-15: evalúa una regla de tipo IA (criterio en lenguaje natural) con el modelo de lenguaje.
    /// La implementación vive en la API, que es donde está configurado el proveedor de IA.
    /// </summary>
    public interface IEvaluadorReglasIA
    {
        /// <summary>¿Hay un proveedor de IA configurado?</summary>
        bool Disponible { get; }

        Task<(bool Incumple, string Explicacion)> Evaluar(Evento evento, string criterio);
    }
}
