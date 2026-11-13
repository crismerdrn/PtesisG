using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;
namespace PTesis.Controllers
{
    public class TipoVentaController : BaseController
    {
        private CDTipoVenta cdTipoVenta = new CDTipoVenta();

        // GET: TipoVenta/Index
        public ActionResult Index(string buscar = "")
        {
            if (!TienePermiso("Ventas") && !TienePermiso("Administrador"))
            {
                TempData["Error"] = "No tiene permisos para acceder a tipos de venta.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Buscar = buscar;
            var lista = cdTipoVenta.ListarTipoVentas();

            if (!string.IsNullOrEmpty(buscar))
            {
                string filtro = buscar.Trim().ToLower();
                lista = lista.Where(x =>
                    x.nombre.ToLower().Contains(filtro) ||
                    (x.descripcion != null && x.descripcion.ToLower().Contains(filtro))
                ).ToList();
            }

            return View(lista);
        }

        // GET: TipoVenta/Create
        public ActionResult Create()
        {
            if (!TienePermiso("Ventas") && !TienePermiso("Administrador"))
            {
                TempData["Error"] = "No tiene permisos para crear tipos de venta.";
                return RedirectToAction("Index");
            }

            return View(new CMTipoVenta());
        }

        // POST: TipoVenta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMTipoVenta tipoVenta)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor complete los campos obligatorios.";
                return View(tipoVenta);
            }

            bool respuesta = cdTipoVenta.AgregarTipoVenta(tipoVenta);
            if (respuesta)
            {
                TempData["Exito"] = "Tipo de venta registrado exitosamente.";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "No se pudo registrar el tipo de venta.";
            return View(tipoVenta);
        }

        // GET: TipoVenta/Edit/5
        public ActionResult Edit(int id)
        {
            if (!TienePermiso("Ventas") && !TienePermiso("Administrador"))
            {
                TempData["Error"] = "No tiene permisos para editar tipos de venta.";
                return RedirectToAction("Index");
            }

            var tipoVenta = cdTipoVenta.ObtenerTipoVentaPorId(id);
            if (tipoVenta == null)
            {
                TempData["Error"] = "Tipo de venta no encontrado.";
                return RedirectToAction("Index");
            }

            return View(tipoVenta);
        }

        // POST: TipoVenta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CMTipoVenta tipoVenta)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor complete los campos obligatorios.";
                return View(tipoVenta);
            }

            bool respuesta = cdTipoVenta.ModificarTipoVenta(tipoVenta);
            if (respuesta)
            {
                TempData["Exito"] = "Tipo de venta actualizado exitosamente.";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "No se pudo actualizar el tipo de venta.";
            return View(tipoVenta);
        }

        // GET: TipoVenta/Delete/5
        public ActionResult Delete(int id)
        {
            if (!TienePermiso("Ventas") && !TienePermiso("Administrador"))
            {
                TempData["Error"] = "No tiene permisos para eliminar tipos de venta.";
                return RedirectToAction("Index");
            }

            var tipoVenta = cdTipoVenta.ObtenerTipoVentaPorId(id);
            if (tipoVenta == null)
            {
                TempData["Error"] = "Tipo de venta no encontrado.";
                return RedirectToAction("Index");
            }

            return View(tipoVenta);
        }

        // POST: TipoVenta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!TienePermiso("Ventas") && !TienePermiso("Administrador"))
            {
                TempData["Error"] = "No tiene permisos para eliminar tipos de venta.";
                return RedirectToAction("Index");
            }

            string mensajeError;
            bool respuesta = cdTipoVenta.EliminarTipoVenta(id, out mensajeError);

            if (respuesta)
            {
                TempData["Exito"] = "Tipo de venta eliminado correctamente.";
            }
            else
            {
                TempData["Error"] = mensajeError;
            }

            return RedirectToAction("Index");
        }
    }
}