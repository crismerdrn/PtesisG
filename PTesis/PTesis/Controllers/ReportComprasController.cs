using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class ReportComprasController : BaseController
    {
        private CDReportCompra cdReportCompra = new CDReportCompra();
        private CDProveedor cdProveedor = new CDProveedor(); // <- Usamos tu clase existente

        public ActionResult Index(
            string fechaInicio = "",
            string fechaFin = "",
            int? idProveedor = null,
            string estado = "")
        {
            if (!TienePermiso("Reportes") && !TienePermiso("Compras"))
            {
                TempData["Error"] = "No tiene permisos para acceder al reporte de compras.";
                return RedirectToAction("Index", "Home");
            }

            var polleria = ObtenerPolleriaActiva();
            int? idPolleria = polleria?.id_polleria;

            DateTime desde = DateTime.Today.AddDays(-30);
            DateTime hasta = DateTime.Today;

            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out DateTime fIni))
                desde = fIni;

            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out DateTime fFin))
                hasta = fFin;

            var modelo = cdReportCompra.ObtenerReporteCompras(
                fechaInicio: desde,
                fechaFin: hasta,
                idPolleria: idPolleria,
                idProveedor: idProveedor,
                estado: estado
            );

            // Obtenemos los proveedores activos desde tu CDProveedor
            var proveedores = cdProveedor.ListarProveedoresActivos();

            // Mapeamos el texto visible: si es empresa muestra RazonSocial, si no Nombre y Apellido
            var listaCombo = proveedores.Select(p => new SelectListItem
            {
                Value = p.id_proveedor.ToString(),
                Text = p.esEmpresa ? p.RazonSocial : $"{p.Nombre} {p.Apellido}".Trim()
            }).OrderBy(x => x.Text).ToList();

            ViewBag.ListaProveedores = new SelectList(listaCombo, "Value", "Text", idProveedor);

            ViewBag.FechaInicio = desde.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = hasta.ToString("yyyy-MM-dd");
            ViewBag.IdProveedor = idProveedor;
            ViewBag.Estado = estado;

            return View(modelo);
        }

        private CMPolleria ObtenerPolleriaActiva()
        {
            return new CMPolleria { id_polleria = 5, Nombre = "El Escondite del Loro" };
        }
    }
}