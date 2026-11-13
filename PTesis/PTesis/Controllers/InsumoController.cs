using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    public class InsumoController : BaseController
    {
        private CDInsumo cdInsumo = new CDInsumo();
        private CDMedida cdMedida = new CDMedida();

        public ActionResult Index(string buscar = "")
        {
            if (!TienePermiso("Insumos"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Insumos.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = TienePermiso("Insumos");
            ViewBag.PuedeEditar = PuedeEditar("Insumos");
            ViewBag.PuedeEliminar = PuedeEliminar("Insumos");

            var lista = cdInsumo.ListarInsumos();

            if (!string.IsNullOrEmpty(buscar))
            {
                string textoBusqueda = buscar.Trim().ToLower();
                lista = lista.Where(i =>
                    i.nombre.ToLower().Contains(textoBusqueda) ||
                    (i.descripcion != null && i.descripcion.ToLower().Contains(textoBusqueda))
                ).ToList();

                ViewBag.Buscar = buscar;
            }

            return View(lista);
        }

        public ActionResult Create()
        {
            if (!TienePermiso("Insumos"))
            {
                TempData["Error"] = "No tiene permisos para crear Insumos.";
                return RedirectToAction("Index");
            }

            CargarMedidas();

            var insumo = new CMInsumo
            {
                estado = true
            };

            return View(insumo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMInsumo insumo)
        {
            try
            {
                if (!TienePermiso("Insumos"))
                {
                    TempData["Error"] = "No tiene permisos para crear Insumos.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    string mensaje = "";
                    bool resultado = cdInsumo.RegistrarInsumo(insumo, out mensaje);

                    if (resultado)
                    {
                        TempData["Exito"] = mensaje;
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Error"] = mensaje;
                    }
                }

                CargarMedidas(insumo.MEDIDAS_id_medida);
                return View(insumo);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                CargarMedidas(insumo.MEDIDAS_id_medida);
                return View(insumo);
            }
        }

        public ActionResult Edit(int id)
        {
            if (!PuedeEditar("Insumos"))
            {
                TempData["Error"] = "No tiene permisos para editar Insumos.";
                return RedirectToAction("Index");
            }

            var insumo = cdInsumo.ObtenerInsumoPorId(id);

            if (insumo == null)
            {
                TempData["Error"] = "Insumo no encontrado.";
                return RedirectToAction("Index");
            }

            CargarMedidas(insumo.MEDIDAS_id_medida);

            return View(insumo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CMInsumo insumo)
        {
            try
            {
                if (!PuedeEditar("Insumos"))
                {
                    TempData["Error"] = "No tiene permisos para editar Insumos.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    insumo.id_insumo = id;
                    string mensaje = "";

                    bool resultado = cdInsumo.EditarInsumo(insumo, out mensaje);

                    if (resultado)
                    {
                        TempData["Exito"] = mensaje;
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Error"] = mensaje;
                    }
                }

                CargarMedidas(insumo.MEDIDAS_id_medida);
                return View(insumo);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                CargarMedidas(insumo.MEDIDAS_id_medida);
                return View(insumo);
            }
        }

        public ActionResult Delete(int id)
        {
            if (!PuedeEliminar("Insumos"))
            {
                TempData["Error"] = "No tiene permisos para eliminar Insumos.";
                return RedirectToAction("Index");
            }

            var insumo = cdInsumo.ObtenerInsumoPorId(id);
            if (insumo == null)
            {
                TempData["Error"] = "Insumo no encontrado.";
                return RedirectToAction("Index");
            }

            return View(insumo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (!PuedeEliminar("Insumos"))
                {
                    TempData["Error"] = "No tiene permisos para eliminar Insumos.";
                    return RedirectToAction("Index");
                }

                string mensaje = "";
                bool resultado = cdInsumo.EliminarInsumo(id, out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = mensaje;
                }
                else
                {
                    TempData["Error"] = mensaje;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Delete", new { id = id });
            }
        }

        #region Métodos Auxiliares

        private void CargarMedidas(int? idSeleccionado = null)
        {
            var medidas = cdMedida.ListarMedidasActivas();

            ViewBag.ListaMedidas = new SelectList(medidas, "id_medida", "Descripcion", idSeleccionado);
        }

        #endregion
    }
}