using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    public class TipoProductoController : BaseController
    {
        private CDTipoProducto cdTipoProducto = new CDTipoProducto();

        public ActionResult Index(string buscar = "")
        {
            try
            {
                if (!TienePermiso("TipoProducto"))
                {
                    TempData["Error"] = "No tiene permisos para acceder a Tipos de Producto";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.PuedeCrear = TienePermiso("TipoProducto");
                ViewBag.PuedeEditar = PuedeEditar("TipoProducto");
                ViewBag.PuedeEliminar = PuedeEliminar("TipoProducto");

                List<CMTipoProducto> lista = null;

                try
                {
                    lista = cdTipoProducto.ListarTipoProductos();
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al cargar los tipos de producto: {ex.Message}";
                    lista = new List<CMTipoProducto>();
                }

                if (!string.IsNullOrEmpty(buscar))
                {
                    buscar = buscar.Trim();
                    if (buscar.Length > 50)
                        buscar = buscar.Substring(0, 50);

                    lista = lista.Where(t =>
                        t.descripcion != null &&
                        t.descripcion.ToLower().Contains(buscar.ToLower())
                    ).ToList();
                    ViewBag.Buscar = buscar;
                }

                return View(lista ?? new List<CMTipoProducto>());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Index: {ex.Message}");
                TempData["Error"] = "Ocurrió un error inesperado.";
                return View(new List<CMTipoProducto>());
            }
        }

        public ActionResult Create()
        {
            try
            {
                if (!TienePermiso("TipoProducto"))
                {
                    TempData["Error"] = "No tiene permisos para crear Tipos de Producto";
                    return RedirectToAction("Index");
                }

                return View(new CMTipoProducto());
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMTipoProducto tipoProducto)
        {
            try
            {
                if (!TienePermiso("TipoProducto"))
                {
                    TempData["Error"] = "No tiene permisos para crear Tipos de Producto";
                    return RedirectToAction("Index");
                }

                if (!ModelState.IsValid)
                {
                    return View(tipoProducto);
                }

                if (string.IsNullOrWhiteSpace(tipoProducto.descripcion))
                {
                    ModelState.AddModelError("descripcion", "La descripción es obligatoria.");
                    return View(tipoProducto);
                }

                tipoProducto.descripcion = tipoProducto.descripcion.Trim();
                if (tipoProducto.descripcion.Length > 100)
                {
                    ModelState.AddModelError("descripcion", "La descripción no puede exceder los 100 caracteres.");
                    return View(tipoProducto);
                }

                tipoProducto.estado = true;

                bool resultado = cdTipoProducto.AgregarTipoProducto(tipoProducto);
                if (resultado)
                {
                    TempData["Exito"] = "Tipo de producto creado exitosamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo crear el tipo de producto.";
                    return View(tipoProducto);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Ya existe"))
                    ModelState.AddModelError("descripcion", ex.Message);
                else
                    TempData["Error"] = $"Error: {ex.Message}";

                return View(tipoProducto);
            }
        }

        public ActionResult Edit(int id = 0)
        {
            try
            {
                if (!PuedeEditar("TipoProducto"))
                {
                    TempData["Error"] = "No tiene permisos para editar Tipos de Producto";
                    return RedirectToAction("Index");
                }

                if (id <= 0)
                {
                    TempData["Error"] = "ID de tipo de producto inválido.";
                    return RedirectToAction("Index");
                }

                var tipoProducto = cdTipoProducto.ObtenerTipoProductoPorId(id);
                if (tipoProducto == null)
                {
                    TempData["Error"] = "Tipo de producto no encontrado.";
                    return RedirectToAction("Index");
                }

                return View(tipoProducto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CMTipoProducto tipoProducto)
        {
            try
            {
                if (!PuedeEditar("TipoProducto"))
                {
                    TempData["Error"] = "No tiene permisos para editar Tipos de Producto";
                    return RedirectToAction("Index");
                }

                if (id != tipoProducto.id_tipoProducto)
                {
                    TempData["Error"] = "ID inconsistente.";
                    return RedirectToAction("Index");
                }

                if (!ModelState.IsValid)
                {
                    return View(tipoProducto);
                }

                if (string.IsNullOrWhiteSpace(tipoProducto.descripcion))
                {
                    ModelState.AddModelError("descripcion", "La descripción es obligatoria.");
                    return View(tipoProducto);
                }

                tipoProducto.descripcion = tipoProducto.descripcion.Trim();

                bool resultado = cdTipoProducto.ModificarTipoProducto(tipoProducto);
                if (resultado)
                {
                    TempData["Exito"] = "Tipo de producto actualizado exitosamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar el tipo de producto.";
                    return View(tipoProducto);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Ya existe"))
                    ModelState.AddModelError("descripcion", ex.Message);
                else
                    TempData["Error"] = $"Error: {ex.Message}";

                return View(tipoProducto);
            }
        }

        public ActionResult Delete(int id = 0)
        {
            try
            {
                if (!PuedeEliminar("TipoProducto"))
                {
                    TempData["Error"] = "No tiene permisos para eliminar Tipos de Producto";
                    return RedirectToAction("Index");
                }

                if (id <= 0)
                {
                    TempData["Error"] = "ID inválido.";
                    return RedirectToAction("Index");
                }

                var tipoProducto = cdTipoProducto.ObtenerTipoProductoPorId(id);
                if (tipoProducto == null)
                {
                    TempData["Error"] = "Tipo de producto no encontrado.";
                    return RedirectToAction("Index");
                }

                return View(tipoProducto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (!PuedeEliminar("TipoProducto"))
                {
                    TempData["Error"] = "No tiene permisos para eliminar Tipos de Producto";
                    return RedirectToAction("Index");
                }

                if (id <= 0)
                {
                    TempData["Error"] = "ID inválido.";
                    return RedirectToAction("Index");
                }

                bool resultado = cdTipoProducto.EliminarTipoProducto(id);
                if (resultado)
                {
                    TempData["Exito"] = "Tipo de producto eliminado exitosamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el tipo de producto.";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("utilizado"))
                {
                    TempData["Error"] = ex.Message;
                }
                else
                {
                    TempData["Error"] = $"Error: {ex.Message}";
                }
            }

            return RedirectToAction("Index");
        }
    }
}