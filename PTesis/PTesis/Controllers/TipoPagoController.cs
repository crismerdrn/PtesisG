using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CapaModelo;
using CapaDatos;

namespace PTesis.Controllers
{
    public class TipoPagoController : BaseController
    {
        private CDTipoPago cdTipoPago = new CDTipoPago();

        // GET: TipoPago
        public ActionResult Index(string buscar = "")
        {
            if (!TienePermiso("TipoPago"))
            {
                TempData["Error"] = "No tiene permisos para acceder a Tipos de Pago";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = PuedeCrear("TipoPago"); // Para crear necesita permiso básico
            ViewBag.PuedeEditar = PuedeEditar("TipoPago");
            ViewBag.PuedeEliminar = PuedeEliminar("TipoPago");

            var lista = cdTipoPago.ListarTipoPagos();

            // Filtro de búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                lista = lista.Where(t =>
                    t.nombre.ToLower().Contains(buscar.ToLower()) ||
                    t.descripcion.ToLower().Contains(buscar.ToLower())
                ).ToList();
                ViewBag.Buscar = buscar;
            }
            return View(lista);
        }

        // GET: TipoPago/Create
        public ActionResult Create()
        {
            // VERIFICAR PERMISO PARA CREAR
            if (!PuedeCrear("TipoPago"))
            {
                TempData["Error"] = "No tiene permisos para crear Tipos de Pago";
                return RedirectToAction("Index");
            }

            return View();
        }

        // POST: TipoPago/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMTipoPago tipoPago)
        {
            try
            {
                // VERIFICAR PERMISO PARA CREAR
                if (!PuedeCrear("TipoPago"))
                {
                    TempData["Error"] = "No tiene permisos para crear Tipos de Pago";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    bool resultado = cdTipoPago.AgregarTipoPago(tipoPago);
                    if (resultado)
                    {
                        TempData["Exito"] = "Tipo de pago creado exitosamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Error"] = "Error al crear el tipo de pago.";
                    }
                }
                return View(tipoPago);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(tipoPago);
            }
        }

        // GET: TipoPago/Edit/5
        public ActionResult Edit(int id)
        {
            // VERIFICAR PERMISO PARA EDITAR
            if (!PuedeEditar("TipoPago"))
            {
                TempData["Error"] = "No tiene permisos para editar Tipos de Pago";
                return RedirectToAction("Index");
            }

            var tipoPago = cdTipoPago.ObtenerTipoPagoPorId(id);
            if (tipoPago == null)
            {
                TempData["Error"] = "Tipo de pago no encontrado.";
                return RedirectToAction("Index");
            }
            return View(tipoPago);
        }

        // POST: TipoPago/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CMTipoPago tipoPago)
        {
            try
            {
                // VERIFICAR PERMISO PARA EDITAR
                if (!PuedeEditar("TipoPago"))
                {
                    TempData["Error"] = "No tiene permisos para editar Tipos de Pago";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    tipoPago.id_tipoPago = id;
                    bool resultado = cdTipoPago.ModificarTipoPago(tipoPago);
                    if (resultado)
                    {
                        TempData["Exito"] = "Tipo de pago actualizado exitosamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Error"] = "Error al actualizar el tipo de pago.";
                    }
                }
                return View(tipoPago);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(tipoPago);
            }
        }

        // GET: TipoPago/Delete/5
        public ActionResult Delete(int id)
        {
            // VERIFICAR PERMISO PARA ELIMINAR
            if (!PuedeEliminar("TipoPago"))
            {
                TempData["Error"] = "No tiene permisos para eliminar Tipos de Pago";
                return RedirectToAction("Index");
            }

            var tipoPago = cdTipoPago.ObtenerTipoPagoPorId(id);
            if (tipoPago == null)
            {
                TempData["Error"] = "Tipo de pago no encontrado.";
                return RedirectToAction("Index");
            }
            return View(tipoPago);
        }

        // POST: TipoPago/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                // VERIFICAR PERMISO PARA ELIMINAR
                if (!PuedeEliminar("TipoPago"))
                {
                    TempData["Error"] = "No tiene permisos para eliminar Tipos de Pago";
                    return RedirectToAction("Index");
                }

                if (id <= 0)
                {
                    TempData["Error"] = "ID inválido.";
                    return RedirectToAction("Index");
                }

                bool resultado = cdTipoPago.EliminarTipoPago(id);
                if (resultado)
                {
                    TempData["Exito"] = "Tipo de pago eliminado exitosamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el tipo de pago.";
                }
            }
            catch (Exception ex)
            {
                // Validación de restricción de clave foránea / registro en uso
                if (ex.Message.Contains("REFERENCE constraint") ||
                    ex.Message.Contains("FK_") ||
                    ex.Message.Contains("conflicto") ||
                    ex.Message.Contains("utilizado") ||
                    ex.Message.Contains("en uso"))
                {
                    TempData["Error"] = "No se puede eliminar el tipo de pago porque está siendo utilizado en otros registros";
                }
                else
                {
                    TempData["Error"] = $"Error al eliminar el tipo de pago: {ex.Message}";
                }
            }

            // Redirige al Index donde se muestra la alerta flotante de TempData
            return RedirectToAction("Index");
        }
    }
}