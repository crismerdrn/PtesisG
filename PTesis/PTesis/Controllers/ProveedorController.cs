using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class ProveedorController : BaseController
    {
        private CDProveedor datos = new CDProveedor();

        public ActionResult Index(string buscar)
        {
            try
            {
                List<CMProveedor> lista;

                if (!string.IsNullOrEmpty(buscar))
                {
                    lista = datos.BuscarProveedor(buscar);
                }
                else
                {
                    lista = datos.ListarProveedores();
                }

                ViewBag.Buscar = buscar;
                return View(lista);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la lista de proveedores: " + ex.Message;
                return View(new List<CMProveedor>());
            }
        }

        public ActionResult Activos()
        {
            try
            {
                List<CMProveedor> lista = datos.ListarProveedoresActivos();
                return View("Index", lista); // Reutiliza la vista Index
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar proveedores activos: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public ActionResult Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de proveedor inválido";
                    return RedirectToAction("Index");
                }

                CMProveedor proveedor = datos.ObtenerProveedorPorId(id);
                if (proveedor == null)
                {
                    TempData["Error"] = "Proveedor no encontrado";
                    return HttpNotFound();
                }
                return View(proveedor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar los detalles: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public ActionResult Create()
        {
            return View(new CMProveedor { Estado = true }); // Por defecto activo
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMProveedor proveedor)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validaciones condicionales
                    if (proveedor.esEmpresa)
                    {
                        if (string.IsNullOrWhiteSpace(proveedor.RazonSocial))
                        {
                            ModelState.AddModelError("RazonSocial", "La razón social es obligatoria para empresas");
                            return View(proveedor);
                        }

                        if (string.IsNullOrWhiteSpace(proveedor.NIT))
                        {
                            ModelState.AddModelError("NIT", "El NIT es obligatorio para empresas");
                            return View(proveedor);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(proveedor.Nombre))
                        {
                            ModelState.AddModelError("Nombre", "El nombre es obligatorio para personas");
                            return View(proveedor);
                        }

                        if (string.IsNullOrWhiteSpace(proveedor.Apellido))
                        {
                            ModelState.AddModelError("Apellido", "El apellido es obligatorio para personas");
                            return View(proveedor);
                        }

                        if (string.IsNullOrWhiteSpace(proveedor.CI))
                        {
                            ModelState.AddModelError("CI", "El CI es obligatorio para personas");
                            return View(proveedor);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(proveedor.Telefono))
                    {
                        ModelState.AddModelError("Telefono", "El teléfono del proveedor es obligatorio");
                        return View(proveedor);
                    }

                    if (datos.AgregarProveedor(proveedor))
                    {
                        TempData["Exito"] = "Proveedor agregado correctamente";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo agregar el proveedor. Intente nuevamente.");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // Captura errores de duplicados
                if (ex.Message.Contains("NIT"))
                    ModelState.AddModelError("NIT", ex.Message);
                else if (ex.Message.Contains("CI"))
                    ModelState.AddModelError("CI", ex.Message);
                else if (ex.Message.Contains("teléfono"))
                    ModelState.AddModelError("Telefono", ex.Message);
                else
                    ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al agregar el proveedor: " + ex.Message);
            }

            return View(proveedor);
        }

        public ActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de proveedor inválido";
                    return RedirectToAction("Index");
                }

                CMProveedor proveedor = datos.ObtenerProveedorPorId(id);
                if (proveedor == null)
                {
                    TempData["Error"] = "Proveedor no encontrado";
                    return HttpNotFound();
                }
                return View(proveedor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el proveedor: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CMProveedor proveedor)
        {
            try
            {
                // 1. LIMPIEZA DE VALIDACIONES CRUZADAS
                // Le decimos al sistema: "Ignora los errores de los campos que no estoy viendo"
                if (proveedor.esEmpresa)
                {
                    // Si es Empresa, borramos errores de Persona
                    ModelState.Remove("Nombre");
                    ModelState.Remove("Apellido");
                    ModelState.Remove("CI");
                }
                else
                {
                    // Si es Persona, borramos errores de Empresa
                    ModelState.Remove("RazonSocial");
                    ModelState.Remove("NIT");
                }

                // 2. AHORA SÍ VERIFICAMOS SI ES VÁLIDO
                if (ModelState.IsValid)
                {
                    // 3. VALIDACIÓN MANUAL Y LIMPIEZA DE DATOS
                    if (proveedor.esEmpresa)
                    {
                        // Validación específica para Empresa
                        if (string.IsNullOrWhiteSpace(proveedor.RazonSocial))
                        {
                            ModelState.AddModelError("RazonSocial", "La razón social es obligatoria");
                            return View(proveedor);
                        }
                        if (string.IsNullOrWhiteSpace(proveedor.NIT))
                        {
                            ModelState.AddModelError("NIT", "El NIT es obligatorio");
                            return View(proveedor);
                        }

                        // Limpiamos datos de persona para que se guarden como NULL en la BD
                        proveedor.Nombre = null;
                        proveedor.Apellido = null;
                        proveedor.CI = null;
                    }
                    else
                    {
                        // Validación específica para Persona
                        if (string.IsNullOrWhiteSpace(proveedor.Nombre))
                        {
                            ModelState.AddModelError("Nombre", "El nombre es obligatorio");
                            return View(proveedor);
                        }
                        if (string.IsNullOrWhiteSpace(proveedor.Apellido))
                        {
                            ModelState.AddModelError("Apellido", "El apellido es obligatorio");
                            return View(proveedor);
                        }
                        if (string.IsNullOrWhiteSpace(proveedor.CI))
                        {
                            ModelState.AddModelError("CI", "El CI es obligatorio");
                            return View(proveedor);
                        }

                        // Limpiamos datos de empresa
                        proveedor.RazonSocial = null;
                        proveedor.NIT = null;
                    }

                    // 4. INTENTAR GUARDAR EN BASE DE DATOS
                    if (datos.ModificarProveedor(proveedor))
                    {
                        TempData["Exito"] = "Proveedor modificado correctamente";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo modificar el proveedor. Intente nuevamente.");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // Captura errores de duplicados específicos
                if (ex.Message.Contains("NIT"))
                    ModelState.AddModelError("NIT", ex.Message);
                else if (ex.Message.Contains("CI"))
                    ModelState.AddModelError("CI", ex.Message);
                else if (ex.Message.Contains("teléfono"))
                    ModelState.AddModelError("Telefono", ex.Message);
                else
                    ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al modificar el proveedor: " + ex.Message);
            }

            // Si llegamos aquí, hubo un error, retornamos a la vista
            return View(proveedor);
        }

        public ActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de proveedor inválido";
                    return RedirectToAction("Index");
                }

                CMProveedor proveedor = datos.ObtenerProveedorPorId(id);
                if (proveedor == null)
                {
                    TempData["Error"] = "Proveedor no encontrado";
                    return HttpNotFound();
                }
                return View(proveedor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el proveedor: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (datos.EliminarProveedor(id))
                {
                    TempData["Exito"] = "Proveedor eliminado correctamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el proveedor";
                }
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                // Captura error de integridad referencial
                TempData["Error"] = ex.Message;
                return RedirectToAction("Delete", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar el proveedor: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public ActionResult CambiarEstado(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "ID de proveedor inválido" });
                }

                CMProveedor proveedor = datos.ObtenerProveedorPorId(id);
                if (proveedor == null)
                {
                    return Json(new { success = false, message = "Proveedor no encontrado" });
                }

                bool nuevoEstado = !proveedor.Estado;
                if (datos.CambiarEstadoProveedor(id, nuevoEstado))
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Proveedor {(nuevoEstado ? "activado" : "desactivado")} correctamente",
                        nuevoEstado = nuevoEstado
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No se pudo cambiar el estado" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult VerificarDuplicado(string tipo, string valor, int? idExcluir = null)
        {
            try
            {
                bool existe = false;
                string mensaje = "";

                switch (tipo.ToLower())
                {
                    case "nit":
                        existe = datos.ExisteProveedor(valor, true, idExcluir);
                        mensaje = existe ? "Ya existe una empresa con este NIT" : "NIT disponible";
                        break;
                    case "ci":
                        existe = datos.ExisteProveedor(valor, false, idExcluir);
                        mensaje = existe ? "Ya existe una persona con este CI" : "CI disponible";
                        break;
                    case "telefono":
                        existe = datos.ExisteTelefono(valor, idExcluir);
                        mensaje = existe ? "Ya existe un proveedor con este teléfono" : "Teléfono disponible";
                        break;
                    default:
                        return Json(new { valido = false, mensaje = "Tipo de verificación no válido" });
                }

                return Json(new { valido = !existe, mensaje = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { valido = false, mensaje = "Error en la verificación: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerPorTipo(bool esEmpresa)
        {
            try
            {
                var proveedores = datos.ObtenerProveedoresPorTipo(esEmpresa);
                return Json(new { success = true, data = proveedores }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult VerificarEliminacion(int id)
        {
            try
            {
                bool tieneCompras = datos.TieneComprasAsociadas(id);
                if (tieneCompras)
                {
                    return Json(new
                    {
                        puedeEliminar = false,
                        mensaje = "No se puede eliminar porque tiene compras asociadas. ¿Desea desactivarlo en lugar de eliminar?"
                    });
                }
                return Json(new { puedeEliminar = true, mensaje = "Puede eliminarse sin problemas" });
            }
            catch (Exception ex)
            {
                return Json(new { puedeEliminar = false, mensaje = "Error al verificar: " + ex.Message });
            }
        }
    }
}