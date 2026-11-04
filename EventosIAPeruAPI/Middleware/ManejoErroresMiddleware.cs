using System.Diagnostics;

namespace EventosIAPeru.API.Middleware
{
    /// <summary>
    /// Middleware propio: atrapa cualquier error no controlado, lo registra en el log y responde
    /// un JSON con el mismo formato que el resto de la API ({ mensaje }). Solo en Development
    /// agrega el detalle técnico; en producción nunca se muestran trazas internas.
    /// También mide cuánto tarda cada petición y avisa en el log si es lenta.
    /// </summary>
    public class ManejoErroresMiddleware
    {
        private const int MilisegundosLento = 2000;

        private readonly RequestDelegate _next;
        private readonly ILogger<ManejoErroresMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ManejoErroresMiddleware(RequestDelegate next, ILogger<ManejoErroresMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var cronometro = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);

                // Si ya se empezó a enviar la respuesta no se puede cambiar
                if (context.Response.HasStarted) throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var respuesta = new Dictionary<string, object?>
                {
                    ["mensaje"] = "Ocurrió un error inesperado. Inténtalo nuevamente.",
                    ["traceId"] = context.TraceIdentifier
                };
                if (_environment.IsDevelopment())
                {
                    respuesta["detalle"] = ex.Message;
                }

                await context.Response.WriteAsJsonAsync(respuesta);
            }
            finally
            {
                cronometro.Stop();
                if (cronometro.ElapsedMilliseconds > MilisegundosLento)
                {
                    _logger.LogWarning("Petición lenta: {Metodo} {Ruta} tardó {Ms} ms",
                        context.Request.Method, context.Request.Path, cronometro.ElapsedMilliseconds);
                }
            }
        }
    }
}
