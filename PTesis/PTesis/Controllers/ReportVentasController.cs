using CapaDatos;
using CapaModelo;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class ReportVentasController : BaseController
    {
        private CDReportVenta cdReportVenta = new CDReportVenta();
        private CDVenta cdVenta = new CDVenta();

        // GET: ReportVentas
        public ActionResult Index(
            string fechaInicio = "",
            string fechaFin = "",
            int? idTipoVenta = null,
            int? idTipoPago = null)
        {
            if (!TienePermiso("Reportes") && !TienePermiso("Ventas"))
            {
                TempData["Error"] = "No tiene permisos para acceder al reporte de ventas.";
                return RedirectToAction("Index", "Home");
            }

            var polleria = ObtenerPolleriaActiva();
            int? idPolleria = polleria?.id_polleria;

            DateTime desde = DateTime.Today;
            DateTime hasta = DateTime.Today;

            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out DateTime fIni))
                desde = fIni;

            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out DateTime fFin))
                hasta = fFin;

            var modelo = cdReportVenta.ObtenerReporteVentas(
                fechaInicio: desde,
                fechaFin: hasta,
                idPolleria: idPolleria,
                idTipoVenta: idTipoVenta,
                idTipoPago: idTipoPago
            );

            ViewBag.ListaCanales = new SelectList(cdVenta.ListarTiposVenta(), "id_tipoVenta", "nombre", idTipoVenta);
            ViewBag.ListaTiposPago = new SelectList(ObtenerListaTiposPago(), "Value", "Text", idTipoPago);

            ViewBag.FechaInicio = desde.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = hasta.ToString("yyyy-MM-dd");
            ViewBag.IdTipoVenta = idTipoVenta;
            ViewBag.IdTipoPago = idTipoPago;

            return View(modelo);
        }

        #region Métodos Auxiliares

        private List<SelectListItem> ObtenerListaTiposPago()
        {
            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();
                    string query = @"SELECT id_tipoPago AS Value, nombre AS Text 
                                     FROM TIPO_PAGO 
                                     WHERE estado = 1 
                                     ORDER BY nombre";
                    return conexion.Query<SelectListItem>(query).ToList();
                }
            }
            catch
            {
                return new List<SelectListItem>();
            }
        }


        #endregion
    }
}