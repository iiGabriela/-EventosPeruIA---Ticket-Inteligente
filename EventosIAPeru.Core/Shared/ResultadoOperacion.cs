namespace EventosIAPeru.Core.Shared
{
    public enum TipoResultado
    {
        Ok,
        NoEncontrado,
        NoAutorizado,
        Invalido
    }

    /// <summary>
    /// Resultado de una operación de escritura. Permite al controller
    /// responder 404 / 403 / 400 con un mensaje claro para el frontend.
    /// </summary>
    public class ResultadoOperacion
    {
        public TipoResultado Tipo { get; private set; }
        public string? Mensaje { get; private set; }
        public int? Id { get; private set; }
        public bool Exito => Tipo == TipoResultado.Ok;

        public static ResultadoOperacion Ok(int? id = null)
            => new ResultadoOperacion { Tipo = TipoResultado.Ok, Id = id };

        public static ResultadoOperacion NoEncontrado(string mensaje)
            => new ResultadoOperacion { Tipo = TipoResultado.NoEncontrado, Mensaje = mensaje };

        public static ResultadoOperacion NoAutorizado(string mensaje)
            => new ResultadoOperacion { Tipo = TipoResultado.NoAutorizado, Mensaje = mensaje };

        public static ResultadoOperacion Invalido(string mensaje)
            => new ResultadoOperacion { Tipo = TipoResultado.Invalido, Mensaje = mensaje };
    }
}
