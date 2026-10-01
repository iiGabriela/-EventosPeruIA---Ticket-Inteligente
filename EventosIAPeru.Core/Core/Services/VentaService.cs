using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EventosIAPeru.Core.Core.DTOs;
using EventosIAPeru.Core.Core.Interfaces;
using EventosIAPeru.Core.Shared;

namespace EventosIAPeru.Core.Core.Services
{
    /// <summary>US-11 (panel de ventas) y US-12 (exportación a Excel y PDF).</summary>
    public class VentaService : IVentaService
    {
        private readonly IVentaRepository _ventaRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        static VentaService()
        {
            // Licencia gratuita de QuestPDF para proyectos académicos / pequeñas empresas
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public VentaService(IVentaRepository ventaRepository, IUsuarioRepository usuarioRepository)
        {
            _ventaRepository = ventaRepository;
            _usuarioRepository = usuarioRepository;
        }

        // ---------------------------- US-11 ----------------------------

        public async Task<PanelVentasDTO?> GetPanelVentas(string firebaseUid, FiltroVentasDTO filtro)
        {
            var usuario = await _usuarioRepository.GetUsuarioByFirebaseUid(firebaseUid);
            if (usuario == null) return null;

            var (desde, hasta, descripcion) = ResolverPeriodo(filtro);
            var ventas = (await _ventaRepository.GetVentasPorEvento(usuario.UsuarioId, desde, hasta)).ToList();
            var ahora = DateTime.UtcNow;

            foreach (var venta in ventas)
            {
                venta.CuposDisponibles = Math.Max(venta.AforoTotal - venta.EntradasVendidasTotal, 0);
                venta.PorcentajeOcupacion = venta.AforoTotal == 0
                    ? 0
                    : Math.Round(100m * venta.EntradasVendidasTotal / venta.AforoTotal, 2);
                venta.EstadoVenta = CalcularEstadoVenta(venta, ahora);
            }

            var aforoTotal = ventas.Sum(v => v.AforoTotal);
            var vendidasTotal = ventas.Sum(v => v.EntradasVendidasTotal);

            var panel = new PanelVentasDTO();
            panel.Periodo = descripcion;
            panel.FechaDesde = desde;
            panel.FechaHasta = hasta;
            panel.IngresosTotales = ventas.Sum(v => v.Recaudado);
            panel.EntradasVendidas = ventas.Sum(v => v.EntradasVendidas);
            panel.EntradasVendidasTotal = vendidasTotal;
            panel.AforoTotal = aforoTotal;
            panel.TasaOcupacion = aforoTotal == 0 ? 0 : Math.Round(100m * vendidasTotal / aforoTotal, 2);
            panel.EventosConControlAforo = ventas.Count(v => v.EstadoVenta == "ACTIVO" || v.EstadoVenta == "AGOTADO");
            panel.TotalEventos = ventas.Count;
            panel.Eventos = ventas;
            return panel;
        }

        // ---------------------------- US-12 ----------------------------

        public byte[] GenerarExcel(PanelVentasDTO panel)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Ventas");

            hoja.Cell(1, 1).Value = "Reporte de ventas y aforo - EventosPeruIA";
            hoja.Cell(1, 1).Style.Font.Bold = true;
            hoja.Cell(1, 1).Style.Font.FontSize = 14;
            hoja.Cell(2, 1).Value = $"Periodo: {DescribirPeriodo(panel)}";
            hoja.Cell(3, 1).Value = $"Generado: {FechaHelper.AHoraPeru(DateTime.UtcNow):dd/MM/yyyy HH:mm}";

            // Resumen consolidado
            hoja.Cell(5, 1).Value = "Ingresos del periodo (S/)";
            hoja.Cell(5, 2).Value = (double)panel.IngresosTotales;
            hoja.Cell(5, 2).Style.NumberFormat.Format = "#,##0.00";
            hoja.Cell(6, 1).Value = "Entradas vendidas en el periodo";
            hoja.Cell(6, 2).Value = (double)panel.EntradasVendidas;
            hoja.Cell(7, 1).Value = "Entradas vendidas / aforo total";
            hoja.Cell(7, 2).Value = $"{panel.EntradasVendidasTotal} / {panel.AforoTotal}";
            hoja.Cell(8, 1).Value = "Tasa de ocupación (%)";
            hoja.Cell(8, 2).Value = (double)panel.TasaOcupacion;
            hoja.Cell(9, 1).Value = "Eventos con control de aforo activo";
            hoja.Cell(9, 2).Value = (double)panel.EventosConControlAforo;
            hoja.Range(5, 1, 9, 1).Style.Font.Bold = true;

            // Desglose por evento
            var cabeceras = new[]
            {
                "Evento", "Fecha", "Sede", "Precio (S/)", "Aforo", "Vendidas (periodo)",
                "Vendidas (total)", "Disponibles", "Ocupación (%)", "Recaudado (S/)", "Estado"
            };
            var fila = 11;
            for (var i = 0; i < cabeceras.Length; i++)
            {
                hoja.Cell(fila, i + 1).Value = cabeceras[i];
            }
            var rangoCabecera = hoja.Range(fila, 1, fila, cabeceras.Length);
            rangoCabecera.Style.Font.Bold = true;
            rangoCabecera.Style.Fill.BackgroundColor = XLColor.LightGray;

            foreach (var venta in panel.Eventos)
            {
                fila++;
                hoja.Cell(fila, 1).Value = venta.Nombre;
                hoja.Cell(fila, 2).Value = FechaHelper.AHoraPeru(venta.FechaInicio);
                hoja.Cell(fila, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                hoja.Cell(fila, 3).Value = venta.Sede;
                hoja.Cell(fila, 4).Value = (double)venta.Precio;
                hoja.Cell(fila, 4).Style.NumberFormat.Format = "#,##0.00";
                hoja.Cell(fila, 5).Value = (double)venta.AforoTotal;
                hoja.Cell(fila, 6).Value = (double)venta.EntradasVendidas;
                hoja.Cell(fila, 7).Value = (double)venta.EntradasVendidasTotal;
                hoja.Cell(fila, 8).Value = (double)venta.CuposDisponibles;
                hoja.Cell(fila, 9).Value = (double)venta.PorcentajeOcupacion;
                hoja.Cell(fila, 10).Value = (double)venta.Recaudado;
                hoja.Cell(fila, 10).Style.NumberFormat.Format = "#,##0.00";
                hoja.Cell(fila, 11).Value = venta.EstadoVenta;
            }

            hoja.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            libro.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerarPdf(PanelVentasDTO panel)
        {
            var documento = Document.Create(contenedor =>
            {
                contenedor.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4.Landscape());
                    pagina.Margin(30);
                    pagina.DefaultTextStyle(estilo => estilo.FontSize(9));

                    pagina.Header().Column(columna =>
                    {
                        columna.Item().Text("Reporte de ventas y aforo - EventosPeruIA").FontSize(16).Bold();
                        columna.Item().Text($"Periodo: {DescribirPeriodo(panel)}");
                        columna.Item().Text($"Generado: {FechaHelper.AHoraPeru(DateTime.UtcNow):dd/MM/yyyy HH:mm}");
                    });

                    pagina.Content().PaddingVertical(10).Column(columna =>
                    {
                        columna.Spacing(8);

                        columna.Item().Row(fila =>
                        {
                            fila.RelativeItem().Element(Indicador).Text($"Ingresos del periodo\nS/ {panel.IngresosTotales:N2}");
                            fila.RelativeItem().Element(Indicador).Text($"Vendidas en el periodo\n{panel.EntradasVendidas}");
                            fila.RelativeItem().Element(Indicador).Text($"Vendidas / aforo\n{panel.EntradasVendidasTotal} / {panel.AforoTotal}");
                            fila.RelativeItem().Element(Indicador).Text($"Ocupación\n{panel.TasaOcupacion:N2} %");
                            fila.RelativeItem().Element(Indicador).Text($"Control de aforo activo\n{panel.EventosConControlAforo} eventos");
                        });

                        columna.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columnas =>
                            {
                                columnas.RelativeColumn(3);   // Evento
                                columnas.RelativeColumn(2);   // Fecha
                                columnas.RelativeColumn(2);   // Sede
                                columnas.RelativeColumn();    // Precio
                                columnas.RelativeColumn();    // Aforo
                                columnas.RelativeColumn();    // Vendidas periodo
                                columnas.RelativeColumn();    // Vendidas total
                                columnas.RelativeColumn();    // Disponibles
                                columnas.RelativeColumn();    // Ocupación
                                columnas.RelativeColumn(1.5f);// Recaudado
                                columnas.RelativeColumn(1.5f);// Estado
                            });

                            tabla.Header(cabecera =>
                            {
                                cabecera.Cell().Element(Cabecera).Text("Evento");
                                cabecera.Cell().Element(Cabecera).Text("Fecha");
                                cabecera.Cell().Element(Cabecera).Text("Sede");
                                cabecera.Cell().Element(Cabecera).AlignRight().Text("Precio");
                                cabecera.Cell().Element(Cabecera).AlignRight().Text("Aforo");
                                cabecera.Cell().Element(Cabecera).AlignRight().Text("Vend. periodo");
                                cabecera.Cell().Element(Cabecera).AlignRight().Text("Vend. total");
                                cabecera.Cell().Element(Cabecera).AlignRight().Text("Disponibles");
                                cabecera.Cell().Element(Cabecera).AlignRight().Text("Ocupación");
                                cabecera.Cell().Element(Cabecera).AlignRight().Text("Recaudado");
                                cabecera.Cell().Element(Cabecera).Text("Estado");
                            });

                            foreach (var venta in panel.Eventos)
                            {
                                tabla.Cell().Element(Celda).Text(venta.Nombre);
                                tabla.Cell().Element(Celda).Text($"{FechaHelper.AHoraPeru(venta.FechaInicio):dd/MM/yyyy HH:mm}");
                                tabla.Cell().Element(Celda).Text(venta.Sede);
                                tabla.Cell().Element(Celda).AlignRight().Text($"{venta.Precio:N2}");
                                tabla.Cell().Element(Celda).AlignRight().Text($"{venta.AforoTotal}");
                                tabla.Cell().Element(Celda).AlignRight().Text($"{venta.EntradasVendidas}");
                                tabla.Cell().Element(Celda).AlignRight().Text($"{venta.EntradasVendidasTotal}");
                                tabla.Cell().Element(Celda).AlignRight().Text($"{venta.CuposDisponibles}");
                                tabla.Cell().Element(Celda).AlignRight().Text($"{venta.PorcentajeOcupacion:N2} %");
                                tabla.Cell().Element(Celda).AlignRight().Text($"{venta.Recaudado:N2}");
                                tabla.Cell().Element(Celda).Text(venta.EstadoVenta);
                            }
                        });
                    });

                    pagina.Footer().AlignCenter().Text(texto =>
                    {
                        texto.Span("Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
                });
            });

            return documento.GeneratePdf();
        }

        // ---------------------------- Ayudantes ----------------------------

        /// <summary>Convierte el filtro en un rango de fechas UTC y su descripción.</summary>
        private static (DateTime? Desde, DateTime? Hasta, string Descripcion) ResolverPeriodo(FiltroVentasDTO filtro)
        {
            if (filtro.FechaDesde.HasValue || filtro.FechaHasta.HasValue)
            {
                return (FechaHelper.AUtc(filtro.FechaDesde), FechaHelper.FinDeDiaUtc(filtro.FechaHasta), "Rango personalizado");
            }

            switch ((filtro.Periodo ?? "TODO").ToUpperInvariant())
            {
                case "MES":
                    return (FechaHelper.InicioMesActualUtc(), null, "Este mes");
                case "SEMANA":
                    return (DateTime.UtcNow.AddDays(-7), null, "Últimos 7 días");
                default:
                    return (null, null, "Histórico completo");
            }
        }

        private static string CalcularEstadoVenta(VentaEventoDTO venta, DateTime ahora)
        {
            if (venta.Estado == "CANCELADO") return "CANCELADO";
            if (venta.Estado == "BORRADOR") return "BORRADOR";
            if (venta.FechaFin <= ahora) return "FINALIZADO";
            if (venta.CuposDisponibles == 0) return "AGOTADO";
            return "ACTIVO";
        }

        private static string DescribirPeriodo(PanelVentasDTO panel)
        {
            var desde = panel.FechaDesde.HasValue ? FechaHelper.AHoraPeru(panel.FechaDesde.Value).ToString("dd/MM/yyyy") : "inicio";
            var hasta = panel.FechaHasta.HasValue ? FechaHelper.AHoraPeru(panel.FechaHasta.Value).ToString("dd/MM/yyyy HH:mm") : "hoy";
            return $"{panel.Periodo} ({desde} - {hasta})";
        }

        private static IContainer Indicador(IContainer contenedor)
        {
            return contenedor.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(6);
        }

        private static IContainer Cabecera(IContainer contenedor)
        {
            return contenedor.Background(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(3);
        }

        private static IContainer Celda(IContainer contenedor)
        {
            return contenedor.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3);
        }
    }
}
