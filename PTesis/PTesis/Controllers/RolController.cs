using System;
using System.Collections.Generic;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    public class RolController : BaseController
    {
        protected override string[] PermisosModulo
        {
            get { return new[] { "Roles" }; }
        }

        private CDRol datos = new CDRol();

        public ActionResult Index(string buscar)
        {
            try
            {
                List<CMRol> lista = datos.ListarRoles();

                if (!string.IsNullOrEmpty(buscar))
                {
                    buscar = buscar.ToLower();
                    lista = lista.FindAll(r =>
                        (r.Nombre != null && r.Nombre.ToLower().Contains(buscar)) ||
                        (r.Descripcion != null && r.Descripcion.ToLower().Contains(buscar))
                    );
                }

                ViewBag.Buscar = buscar;
                return View(lista);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la lista de roles: " + ex.Message;
                return View(new List<CMRol>());
            }
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMRol rol)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrWhiteSpace(rol.Nombre))
                    {
                        ModelState.AddModelError("Nombre", "El nombre del rol es obligatorio");
                        return View(rol);
                    }

                    if (string.IsNullOrWhiteSpace(rol.Descripcion))
                    {
                        ModelState.AddModelError("Descripcion", "La descripción del rol es obligatoria");
                        return View(rol);
                    }

                    if (datos.AgregarRol(rol))
                    {
                        TempData["Exito"] = "Rol agregado correctamente";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo agregar el rol. Intente nuevamente.");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Nombre", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al agregar el rol: " + ex.Message);
            }

            return View(rol);
        }

        public ActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de rol inválido";
                    return RedirectToAction("Index");
                }

                CMRol rol = datos.ObtenerRolPorId(id);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return HttpNotFound();
                }
                return View(rol);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el rol: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CMRol rol)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrWhiteSpace(rol.Nombre))
                    {
                        ModelState.AddModelError("Nombre", "El nombre del rol es obligatorio");
                        return View(rol);
                    }

                    if (string.IsNullOrWhiteSpace(rol.Descripcion))
                    {
                        ModelState.AddModelError("Descripcion", "La descripción del rol es obligatoria");
                        return View(rol);
                    }

                    if (datos.ModificarRol(rol))
                    {
                        TempData["Exito"] = "Rol modificado correctamente";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo modificar el rol. Intente nuevamente.");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Nombre", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al modificar el rol: " + ex.Message);
            }

            return View(rol);
        }

        public ActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de rol inválido";
                    return RedirectToAction("Index");
                }

                CMRol rol = datos.ObtenerRolPorId(id);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return RedirectToAction("Index");
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el rol: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de rol inválido";
                    return RedirectToAction("Index");
                }

                if (datos.EliminarRol(id))
                {
                    TempData["Exito"] = "Rol eliminado correctamente";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el rol";
                    return RedirectToAction("Index");
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar el rol: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

    }
}