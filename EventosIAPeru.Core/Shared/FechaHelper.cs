namespace EventosIAPeru.Core.Shared
{
    /// <summary>
    /// PostgreSQL (timestamptz) solo acepta fechas en UTC desde Npgsql.
    /// Las fechas que llegan sin zona horaria se interpretan como hora de Perú (UTC-5, sin horario de verano).
    /// </summary>
    public static class FechaHelper
    {
        private static readonly TimeSpan OffsetPeru = TimeSpan.FromHours(-5);

        public static DateTime AUtc(DateTime fecha)
        {
            if (fecha.Kind == DateTimeKind.Utc) return fecha;
            if (fecha.Kind == DateTimeKind.Local) return fecha.ToUniversalTime();
            return DateTime.SpecifyKind(fecha - OffsetPeru, DateTimeKind.Utc);
        }

        public static DateTime? AUtc(DateTime? fecha)
        {
            if (!fecha.HasValue) return null;
            return AUtc(fecha.Value);
        }

        /// <summary>Convierte una fecha UTC a hora de Perú (para mostrar en reportes).</summary>
        public static DateTime AHoraPeru(DateTime fechaUtc)
        {
            return DateTime.SpecifyKind(AUtc(fechaUtc) + OffsetPeru, DateTimeKind.Unspecified);
        }

        /// <summary>Primer día del mes actual (hora de Perú), expresado en UTC.</summary>
        public static DateTime InicioMesActualUtc()
        {
            var ahoraPeru = DateTime.UtcNow + OffsetPeru;
            var inicioMes = new DateTime(ahoraPeru.Year, ahoraPeru.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return AUtc(inicioMes);
        }

        /// <summary>
        /// Para filtros "hasta": si llega solo la fecha (sin hora), incluye todo ese día.
        /// </summary>
        public static DateTime? FinDeDiaUtc(DateTime? fecha)
        {
            if (!fecha.HasValue) return null;
            var valor = fecha.Value;
            if (valor.TimeOfDay == TimeSpan.Zero) valor = valor.AddDays(1);
            return AUtc(valor);
        }
    }
}
