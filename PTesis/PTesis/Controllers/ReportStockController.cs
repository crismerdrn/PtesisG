using CapaDatos;
using CapaModelo;
using Dapper;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class ReportStockController : BaseController
    {
        private CDReportStock cdReportStock = new CDReportStock();

        public ActionResult Index(string tipoFiltro = "TODOS", string fechaDesde = "", string fechaHasta = "")
        {
            if (!TienePermiso("Reportes") && !TienePermiso("Stock"))
            {
                TempData["Error"] = "No tiene permisos para acceder al reporte de inventario.";
                return RedirectToAction("Index", "Home");
            }

            var polleria = ObtenerPolleriaActiva();
            int? idPolleria = polleria?.id_polleria;

            DateTime? desde = null;
            DateTime? hasta = null;

            if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out DateTime fD))
                desde = fD;

            if (!string.IsNullOrEmpty(fechaHasta) && DateTime.TryParse(fechaHasta, out DateTime fH))
                hasta = fH;

            var modelo = cdReportStock.ObtenerReporteStock(
                idPolleria: idPolleria,
                tipoFiltro: tipoFiltro,
                fechaDesde: desde,
                fechaHasta: hasta
            );

            ViewBag.TipoFiltro = tipoFiltro;
            ViewBag.FechaDesde = desde?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(-15).ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = hasta?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

            return View(modelo);
        }

        private CMPolleria ObtenerPolleriaActiva()
        {
            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();
                    string query = "SELECT TOP 1 id_polleria, nombre FROM POLLERIA WHERE estado = 1 ORDER BY id_polleria";
                    var polleria = conexion.QueryFirstOrDefault<CMPolleria>(query);
                    if (polleria != null) return polleria;
                }
            }
            catch { }
            return new CMPolleria { id_polleria = 5, Nombre = "El Escondite del Loro" };
        }
    }
}