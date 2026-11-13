using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    public class PermisosController : BaseController
    {
        protected override string[] PermisosModulo
        {
            get { return new[] { "Permisos" }; }
        }

        private CDPermiso datos = new CDPermiso();
        private CDRol datosRol = new CDRol();

        public ActionResult Index()
        {
            try
            {
                List<CMPermisos> lista = datos.ListarPermisosConJerarquia();
                ViewBag.Roles = new SelectList(datosRol.ListarRoles(), "id_rol", "Nombre");
                return View(lista);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar permisos: " + ex.Message;
                return View(new List<CMPermisos>());
            }
        }

        public ActionResult Mantenimiento(string buscar)
        {
            try
            {
                List<CMPermisos> lista = datos.ListarPermisos();

                if (!string.IsNullOrEmpty(buscar))
                {
                    buscar = buscar.ToLower();
                    lista = lista.FindAll(p =>
                        (p.NombreMenu != null && p.NombreMenu.ToLower().Contains(buscar)) ||
                        (p.Modulo != null && p.Modulo.ToLower().Contains(buscar)) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(buscar))
                    );
                }

                ViewBag.Buscar = buscar;
                return View(lista);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar la lista de permisos: " + ex.Message;
                return View(new List<CMPermisos>());
            }
        }

        public ActionResult Create()
        {
            try
            {
                CMPermisos permiso = new CMPermisos();
                List<CMPermisos> permisosPadre = datos.ObtenerPermisosPadre();

                var listaPadres = new List<SelectListItem>();
                listaPadres.Add(new SelectListItem { Value = "", Text = "-- Ninguno (Menú Principal) --" });

                foreach (var p in permisosPadre)
                {
                    listaPadres.Add(new SelectListItem
                    {
                        Value = p.id_permiso.ToString(),
                        Text = p.NombreMenu + (p.Modulo != null ? " (" + p.Modulo + ")" : "")
                    });
                }

                ViewBag.PermisosPadre = listaPadres;
                ViewBag.Tipos = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MENU", Text = "Menú", Selected = true },
                    new SelectListItem { Value = "ACCION", Text = "Acción" }
                };

                return View(permiso);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el formulario: " + ex.Message;
                return RedirectToAction("Mantenimiento");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMPermisos permiso)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (datos.AgregarPermiso(permiso))
                    {
                        TempData["Exito"] = "Permiso agregado correctamente";
                        return RedirectToAction("Mantenimiento");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo agregar el permiso");
                    }
                }

                List<CMPermisos> permisosPadre = datos.ObtenerPermisosPadre();
                var listaPadres = new List<SelectListItem>();
                listaPadres.Add(new SelectListItem { Value = "", Text = "-- Ninguno (Menú Principal) --" });
                foreach (var p in permisosPadre)
                {
                    listaPadres.Add(new SelectListItem
                    {
                        Value = p.id_permiso.ToString(),
                        Text = p.NombreMenu + (p.Modulo != null ? " (" + p.Modulo + ")" : "")
                    });
                }
                ViewBag.PermisosPadre = listaPadres;
                ViewBag.Tipos = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MENU", Text = "Menú" },
                    new SelectListItem { Value = "ACCION", Text = "Acción" }
                };

                return View(permiso);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("NombreMenu", ex.Message);

                List<CMPermisos> permisosPadre = datos.ObtenerPermisosPadre();
                var listaPadres = new List<SelectListItem>();
                listaPadres.Add(new SelectListItem { Value = "", Text = "-- Ninguno (Menú Principal) --" });
                foreach (var p in permisosPadre)
                {
                    listaPadres.Add(new SelectListItem
                    {
                        Value = p.id_permiso.ToString(),
                        Text = p.NombreMenu + (p.Modulo != null ? " (" + p.Modulo + ")" : "")
                    });
                }
                ViewBag.PermisosPadre = listaPadres;
                ViewBag.Tipos = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MENU", Text = "Menú" },
                    new SelectListItem { Value = "ACCION", Text = "Acción" }
                };

                return View(permiso);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar permiso: " + ex.Message;
                return RedirectToAction("Mantenimiento");
            }
        }

        public ActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de permiso inválido";
                    return RedirectToAction("Mantenimiento");
                }

                CMPermisos permiso = datos.ObtenerPermisoPorId(id);
                if (permiso == null)
                {
                    TempData["Error"] = "Permiso no encontrado";
                    return HttpNotFound();
                }

                List<CMPermisos> permisosPadre = datos.ObtenerPermisosPadre()
                    .Where(p => p.id_permiso != id)
                    .ToList();

                var listaPadres = new List<SelectListItem>();
                listaPadres.Add(new SelectListItem { Value = "", Text = "-- Ninguno (Menú Principal) --" });
                foreach (var p in permisosPadre)
                {
                    listaPadres.Add(new SelectListItem
                    {
                        Value = p.id_permiso.ToString(),
                        Text = p.NombreMenu + (p.Modulo != null ? " (" + p.Modulo + ")" : ""),
                        Selected = (p.id_permiso == permiso.Padre_id)
                    });
                }

                ViewBag.PermisosPadre = listaPadres;
                ViewBag.Tipos = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MENU", Text = "Menú", Selected = (permiso.Tipo == "MENU") },
                    new SelectListItem { Value = "ACCION", Text = "Acción", Selected = (permiso.Tipo == "ACCION") }
                };

                return View(permiso);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el permiso: " + ex.Message;
                return RedirectToAction("Mantenimiento");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CMPermisos permiso)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (datos.ModificarPermiso(permiso))
                    {
                        TempData["Exito"] = "Permiso modificado correctamente";
                        return RedirectToAction("Mantenimiento");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo modificar el permiso");
                    }
                }

                List<CMPermisos> permisosPadre = datos.ObtenerPermisosPadre()
                    .Where(p => p.id_permiso != permiso.id_permiso)
                    .ToList();

                var listaPadres = new List<SelectListItem>();
                listaPadres.Add(new SelectListItem { Value = "", Text = "-- Ninguno (Menú Principal) --" });
                foreach (var p in permisosPadre)
                {
                    listaPadres.Add(new SelectListItem
                    {
                        Value = p.id_permiso.ToString(),
                        Text = p.NombreMenu + (p.Modulo != null ? " (" + p.Modulo + ")" : ""),
                        Selected = (p.id_permiso == permiso.Padre_id)
                    });
                }
                ViewBag.PermisosPadre = listaPadres;
                ViewBag.Tipos = new List<SelectListItem>
                {
                    new SelectListItem { Value = "MENU", Text = "Menú", Selected = (permiso.Tipo == "MENU") },
                    new SelectListItem { Value = "ACCION", Text = "Acción", Selected = (permiso.Tipo == "ACCION") }
                };

                return View(permiso);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("NombreMenu", ex.Message);
                return View(permiso);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al modificar permiso: " + ex.Message;
                return RedirectToAction("Mantenimiento");
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de permiso inválido";
                    return RedirectToAction("Mantenimiento");
                }

                CMPermisos permiso = datos.ObtenerPermisoPorId(id);
                if (permiso == null)
                {
                    TempData["Error"] = "Permiso no encontrado";
                    return HttpNotFound();
                }

                return View(permiso);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el permiso: " + ex.Message;
                return RedirectToAction("Mantenimiento");
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
                    TempData["Error"] = "ID de permiso inválido";
                    return RedirectToAction("Mantenimiento");
                }

                if (datos.EliminarPermiso(id))
                {
                    TempData["Exito"] = "Permiso eliminado correctamente";
                    return RedirectToAction("Mantenimiento");
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el permiso";
                    return RedirectToAction("Mantenimiento");
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Delete", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar el permiso: " + ex.Message;
                return RedirectToAction("Delete", new { id = id });
            }
        }

        public ActionResult Asignar(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de rol inválido";
                    return RedirectToAction("Index", "Rol");
                }

                CMRol rol = datosRol.ObtenerRolPorId(id);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return RedirectToAction("Index", "Rol");
                }

                var viewModel = new CMAsignarPermisosViewModel
                {
                    IdRol = rol.id_rol,
                    NombreRol = rol.Nombre,
                    DescripcionRol = rol.Descripcion,
                    Grupos = datos.ObtenerMatrizPermisos(id)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar asignación: " + ex.Message;
                return RedirectToAction("Index", "Rol");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Asignar(CMAsignarPermisosViewModel model)
        {
            try
            {
                if (model == null || model.IdRol <= 0)
                {
                    TempData["Error"] = "ID de rol inválido";
                    return RedirectToAction("Index", "Rol");
                }

                List<int> permisosSeleccionados = new List<int>();

                if (model.Grupos != null)
                {
                    foreach (var grupo in model.Grupos)
                    {
                        if (grupo.Submodulos == null) continue;

                        foreach (var fila in grupo.Submodulos)
                        {
                            if (fila.Ver && fila.IdVer.HasValue && fila.IdVer.Value > 0)
                                permisosSeleccionados.Add(fila.IdVer.Value);

                            if (fila.Crear && fila.IdCrear.HasValue && fila.IdCrear.Value > 0)
                                permisosSeleccionados.Add(fila.IdCrear.Value);

                            if (fila.Editar && fila.IdEditar.HasValue && fila.IdEditar.Value > 0)
                                permisosSeleccionados.Add(fila.IdEditar.Value);

                            if (fila.Eliminar && fila.IdEliminar.HasValue && fila.IdEliminar.Value > 0)
                                permisosSeleccionados.Add(fila.IdEliminar.Value);
                        }
                    }
                }

                permisosSeleccionados = permisosSeleccionados.Distinct().ToList();

                System.Diagnostics.Debug.WriteLine($"IDs marcados en UI: {string.Join(",", permisosSeleccionados)}");

                List<int> permisosConPadres = datos.ObtenerPermisosConPadres(permisosSeleccionados);

                System.Diagnostics.Debug.WriteLine($"IDs con padres: {string.Join(",", permisosConPadres)}");
                System.Diagnostics.Debug.WriteLine($"Total: {permisosConPadres.Count}");

                int idUsuario = ObtenerUsuarioActual();
                string mensaje;
                bool resultado = datos.AsignarPermisosARol(
                    model.IdRol,
                    permisosConPadres,  
                    idUsuario,
                    out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = "Permisos asignados correctamente. Cierre sesión y vuelva a entrar para ver los cambios.";
                    return RedirectToAction("Index", "Rol");
                }
                else
                {
                    TempData["Error"] = mensaje;
                    return RedirectToAction("Asignar", new { id = model.IdRol });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al asignar permisos: " + ex.Message;
                return RedirectToAction("Asignar", new { id = model.IdRol });
            }
        }

        public ActionResult VerAsignados(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "ID de rol inválido";
                    return RedirectToAction("Index", "Rol");
                }

                CMRol rol = datosRol.ObtenerRolPorId(id);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return RedirectToAction("Index", "Rol");
                }

                List<CMRolPermiso> asignados = datos.ListarPermisosPorRol(id);

                ViewBag.Rol = rol;
                return View(asignados);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar permisos asignados: " + ex.Message;
                return RedirectToAction("Index", "Rol");
            }
        }

        private int ObtenerUsuarioActual()
        {
            if (Session["UsuarioId"] != null)
            {
                return Convert.ToInt32(Session["UsuarioId"]);
            }
            return 1; 
        }
    }
}