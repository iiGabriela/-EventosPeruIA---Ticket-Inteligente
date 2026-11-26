using EventosIAPeru.Core.Core.Entities;
using EventosIAPeru.Core.Core.Interfaces;

namespace EventosIAPeru.Core.Core.Services
{
    /// <summary>
    /// US-15: reglas automáticas de moderación.
    /// Se ejecuta cuando un organizador publica un evento o edita uno ya publicado.
    /// - Regla SIMPLE: compara el aforo o el precio contra un valor.
    /// - Regla IA: el modelo de lenguaje decide si el evento incumple el criterio escrito por el admin.
    /// Cada incumplimiento crea un reporte (origen SISTEMA o IA) y aplica la acción de la regla
    /// (EN_REVISION o DESACTIVADO). La decisión final siempre es del administrador.
    /// </summary>
    public class ModeracionAutomaticaService : IModeracionAutomatica
    {
        private readonly IModeracionRepository _moderacionRepository;
        private readonly IEvaluadorReglasIA? _evaluadorIA;

        public ModeracionAutomaticaService(IModeracionRepository moderacionRepository, IEvaluadorReglasIA? evaluadorIA = null)
        {
            _moderacionRepository = moderacionRepository;
            _evaluadorIA = evaluadorIA;
        }

        /// <summary>Regla de IA del sistema que revisa todo evento en busca de contenido malicioso.</summary>
        public const string NombreFiltroSeguridad = "Filtro de seguridad (IA)";
        public const string CriterioFiltroSeguridad =
            "El evento es malicioso o peligroso: parece una estafa o fraude (precios irreales, pide pagos, datos bancarios, " +
            "contraseñas o contacto fuera de la plataforma, enlaces sospechosos), promueve actividades ilegales (drogas, armas, " +
            "venta de alcohol a menores, apuestas ilegales), contiene discurso de odio, violencia, acoso o contenido sexual explícito. " +
            "No lo marques solo por ser nocturno, ofrecer alcohol a adultos o tener precios altos.";

        public async Task<int> EvaluarEvento(int eventoId)
        {
            // La regla de seguridad se crea sola la primera vez; el admin puede desactivarla en Reglas
            await _moderacionRepository.AsegurarReglaSistema(NombreFiltroSeguridad, CriterioFiltroSeguridad);

            var reglas = (await _moderacionRepository.GetReglas()).Where(r => r.Activa).ToList();
            if (reglas.Count == 0) return 0;

            var evento = await _moderacionRepository.GetEventoParaModerar(eventoId);
            // Un evento ya desactivado por un admin no se vuelve a evaluar
            if (evento == null || evento.EstadoModeracion == "DESACTIVADO") return 0;

            var incumplidas = 0;
            string? accionFinal = null;
            string? motivoFinal = null;

            foreach (var regla in reglas)
            {
                var motivo = await MotivoDeIncumplimiento(evento, regla);
                if (motivo == null) continue;

                // Si el admin todavía no resolvió el reporte anterior de esta regla, no se duplica
                if (await _moderacionRepository.ExisteReportePendienteDeRegla(eventoId, regla.ReglaId)) continue;

                incumplidas++;
                _moderacionRepository.AgregarReporte(new ReporteEvento
                {
                    EventoId = eventoId,
                    Origen = regla.Tipo == "IA" ? "IA" : "SISTEMA",
                    Motivo = Recortar(motivo, 500),
                    Estado = "PENDIENTE",
                    FechaReporte = DateTime.UtcNow,
                    ReglaId = regla.ReglaId
                });

                // Si varias reglas se incumplen, gana la acción más severa
                if (accionFinal == null || regla.Accion == "DESACTIVADO")
                {
                    accionFinal = regla.Accion;
                    motivoFinal = motivo;
                }
            }

            if (accionFinal == null) return 0;

            evento.EstadoModeracion = accionFinal;
            evento.MotivoModeracion = Recortar(motivoFinal!, 500);

            await _moderacionRepository.GuardarCambios();
            return incumplidas;
        }

        // Devuelve el motivo si el evento incumple la regla, o null si la cumple (o no se pudo evaluar)
        private async Task<string?> MotivoDeIncumplimiento(Evento evento, ReglaModeracion regla)
        {
            if (regla.Tipo == "SIMPLE")
            {
                return CumpleCondicion(evento, regla)
                    ? $"Regla automática \"{regla.Nombre}\": {regla.Criterio}"
                    : null;
            }

            if (regla.Tipo == "IA" && _evaluadorIA is { Disponible: true })
            {
                try
                {
                    var (incumple, explicacion) = await _evaluadorIA.Evaluar(evento, regla.Criterio);
                    return incumple ? $"Regla IA \"{regla.Nombre}\": {explicacion}" : null;
                }
                catch (Exception)
                {
                    // Si el proveedor de IA falla, publicar el evento no debe fallar: la regla se omite esta vez
                    return null;
                }
            }

            return null;
        }

        /// <summary>¿El evento cae dentro de la condición de una regla SIMPLE? (pública para las pruebas)</summary>
        public static bool CumpleCondicion(Evento evento, ReglaModeracion regla)
        {
            if (!regla.Valor.HasValue) return false;

            decimal actual;
            switch (regla.Campo)
            {
                case "AFORO_TOTAL": actual = evento.AforoTotal; break;
                case "PRECIO": actual = evento.Precio; break;
                default: return false;
            }

            return regla.Operador switch
            {
                "MAYOR" => actual > regla.Valor.Value,
                "MAYOR_IGUAL" => actual >= regla.Valor.Value,
                "MENOR" => actual < regla.Valor.Value,
                "MENOR_IGUAL" => actual <= regla.Valor.Value,
                "IGUAL" => actual == regla.Valor.Value,
                _ => false
            };
        }

        private static string Recortar(string texto, int maximo)
        {
            return texto.Length <= maximo ? texto : texto.Substring(0, maximo);
        }
    }
}
