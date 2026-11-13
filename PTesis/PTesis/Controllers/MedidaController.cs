using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    public class MedidaController : BaseController
    {
        private CDMedida cdMedida = new CDMedida();

        // GET: Medida
        public ActionResult Index(string buscar = "", string fechaInicio = "", string fechaFin = "")
        {
            if (!TienePermiso("Medidas"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Medidas.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = PuedeCrear("Medidas");
            ViewBag.PuedeEditar = PuedeEditar("Medidas");
            ViewBag.PuedeEliminar = PuedeEliminar("Medidas");

            var lista = cdMedida.ListarMedidas();

            // 1. Filtro por texto (Descripción y Abreviatura)
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string textoBusqueda = buscar.Trim().ToLower();

                lista = lista.Where(m =>
                    (!string.IsNullOrEmpty(m.Descripcion) && m.Descripcion.ToLower().Contains(textoBusqueda)) ||
                    (!string.IsNullOrEmpty(m.Abreviatura) && m.Abreviatura.ToLower().Contains(textoBusqueda))
                ).ToList();

                ViewBag.Buscar = buscar;
            }

            DateTime fInicio;
            if (!string.IsNullOrWhiteSpace(fechaInicio) && DateTime.TryParse(fechaInicio, out fInicio))
            {
                lista = lista.Where(m => m.FechaRegistro.Date >= fInicio.Date).ToList();
                ViewBag.FechaInicio = fInicio.ToString("yyyy-MM-dd");
            }

            DateTime fFin;
            if (!string.IsNullOrWhiteSpace(fechaFin) && DateTime.TryParse(fechaFin, out fFin))
            {
                lista = lista.Where(m => m.FechaRegistro.Date <= fFin.Date).ToList();
                ViewBag.FechaFin = fFin.ToString("yyyy-MM-dd");
            }

            return View(lista);
        }

        // GET: Medida/Create
        public ActionResult Create()
        {
            if (!PuedeCrear("Medidas"))
            {
                TempData["Error"] = "No tiene permisos para crear Medidas.";
                return RedirectToAction("Index");
            }

            var medida = new CMMedida
            {
                Estado = true
            };

            return View(medida);
        }

        // POST: Medida/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMMedida medida)
        {
            if (!PuedeCrear("Medidas"))
            {
                TempData["Error"] = "No tiene permisos para crear Medidas.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                medida.Estado = true; // Por defecto activa
                bool resultado = cdMedida.AgregarMedida(medida);

                if (resultado)
                {
                    TempData["Exito"] = "Medida registrada correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo registrar la Medida en la base de datos.";
                }
            }
            else
            {
                TempData["Error"] = "Por favor complete los campos obligatorios.";
            }

            return View(medida);
        }

        // GET: Medida/Edit/5
        public ActionResult Edit(int id)
        {
            if (!PuedeEditar("Medidas"))
            {
                TempData["Error"] = "No tiene permisos para editar Medidas.";
                return RedirectToAction("Index");
            }

            var medida = cdMedida.ObtenerMedidaPorId(id);

            if (medida == null)
            {
                TempData["Error"] = "Medida no encontrada.";
                return RedirectToAction("Index");
            }

            return View(medida);
        }

        // POST: Medida/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CMMedida medida)
        {
            if (!PuedeEditar("Medidas"))
            {
                TempData["Error"] = "No tiene permisos para editar Medidas.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                medida.id_medida = id;
                bool resultado = cdMedida.ModificarMedida(medida);

                if (resultado)
                {
                    TempData["Exito"] = "Medida actualizada correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar la Medida en la base de datos.";
                }
            }
            else
            {
                TempData["Error"] = "Por favor revise las observaciones en el formulario.";
            }

            return View(medida);
        }

        // GET: Medida/Delete/5
        public ActionResult Delete(int id)
        {
            if (!PuedeEliminar("Medidas"))
            {
                TempData["Error"] = "No tiene permisos para eliminar Medidas.";
                return RedirectToAction("Index");
            }

            var medida = cdMedida.ObtenerMedidaPorId(id);
            if (medida == null)
            {
                TempData["Error"] = "Medida no encontrada.";
                return RedirectToAction("Index");
            }

            return View(medida);
        }

        // POST: Medida/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!PuedeEliminar("Medidas"))
            {
                TempData["Error"] = "No tiene permisos para eliminar Medidas.";
                return RedirectToAction("Index");
            }

            string mensajeError;
            bool resultado = cdMedida.EliminarMedida(id, out mensajeError);

            if (resultado)
            {
                TempData["Exito"] = "Medida eliminada correctamente.";
            }
            else
            {
                // Pasa el mensaje amigable al SweetAlert del Layout
                TempData["Error"] = mensajeError;
            }

            return RedirectToAction("Index");
        }
    }
}