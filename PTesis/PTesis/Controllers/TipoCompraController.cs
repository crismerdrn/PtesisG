using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class TipoCompraController : BaseController
    {
        private CDTipoCompra cdTipoCompra = new CDTipoCompra();

        public ActionResult Index(string buscar = "")
        {
            var lista = cdTipoCompra.ListarTipoCompra();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string termino = buscar.Trim().ToLower();

                lista = lista.Where(t =>
                    (!string.IsNullOrEmpty(t.Nombre) && t.Nombre.ToLower().Contains(termino)) ||
                    (!string.IsNullOrEmpty(t.Descripcion) && t.Descripcion.ToLower().Contains(termino))
                ).ToList();

                ViewBag.Buscar = buscar;
            }

            return View(lista);
        }

        public ActionResult Create()
        {
            CargarCategorias();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMTipoCompra tipoCompra)
        {
            if (ModelState.IsValid)
            {
                tipoCompra.Estado = true;

                bool ok = cdTipoCompra.AgregarTipoCompra(tipoCompra);

                if (ok)
                {
                    TempData["SuccessMessage"] = "Tipo de compra registrado con éxito.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo registrar el tipo de compra.";
                }
            }

            CargarCategorias();
            return View(tipoCompra);
        }

        public ActionResult Edit(int id)
        {
            var tipo = cdTipoCompra.ObtenerTipoCompraPorId(id);

            if (tipo == null)
            {
                TempData["ErrorMessage"] = "Tipo de compra no encontrado.";
                return RedirectToAction("Index");
            }

            CargarCategorias();
            return View(tipo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CMTipoCompra tipoCompra)
        {
            if (ModelState.IsValid)
            {
                tipoCompra.id_tipoCompra = id;

                bool ok = cdTipoCompra.ModificarTipoCompra(tipoCompra);

                if (ok)
                {
                    TempData["SuccessMessage"] = "Tipo de compra modificado con éxito.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo modificar el tipo de compra.";
                }
            }

            CargarCategorias();
            return View(tipoCompra);
        }

        public ActionResult Delete(int id)
        {
            var tipo = cdTipoCompra.ObtenerTipoCompraPorId(id);

            if (tipo == null)
            {
                TempData["ErrorMessage"] = "Tipo de compra no encontrado.";
                return RedirectToAction("Index");
            }

            return View(tipo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            string mensajeError;
            bool ok = cdTipoCompra.EliminarTipoCompra(id, out mensajeError);

            if (ok)
            {
                TempData["Exito"] = "Tipo de compra eliminado correctamente.";
            }
            else
            {
                // Usamos TempData["Error"] que es el que lee tu _Layout para mostrar SweetAlert
                TempData["Error"] = mensajeError;
            }

            return RedirectToAction("Index");
        }

        private void CargarCategorias()
        {
            ViewBag.Categorias = new List<SelectListItem>
            {
                new SelectListItem { Text = "Materia Prima (Insumos)", Value = "INSUMO" },
                new SelectListItem { Text = "Mercadería para Venta (Productos)", Value = "PRODUCTO" }
            };
        }
    }
}