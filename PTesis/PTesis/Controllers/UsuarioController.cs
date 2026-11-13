using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    [Authorize]
    public class UsuarioController : BaseController
    {
        private CDUsuario datos = new CDUsuario();

        public ActionResult Index(string buscar)
        {
            try
            {
                if (!TienePermiso("Usuario_Leer"))
                {
                    TempData["Error"] = "No tienes permiso para ver usuarios.";
                    return RedirectToAction("Index", "Home");
                }

                List<Usuario> lista = datos.ListarUsuarios();

                if (!string.IsNullOrEmpty(buscar))
                {
                    buscar = buscar.ToLower();
                    lista = lista.FindAll(u =>
                        (u.nombre != null && u.nombre.ToLower().Contains(buscar)) ||
                        (u.apellido != null && u.apellido.ToLower().Contains(buscar)) ||
                        (u.correo != null && u.correo.ToLower().Contains(buscar)) ||
                        (u.telefono != null && u.telefono.ToLower().Contains(buscar)) ||
                        (u.direccion != null && u.direccion.ToLower().Contains(buscar)) ||
                        (u.usuario != null && u.usuario.ToLower().Contains(buscar)) ||
                        (u.Rol != null && u.Rol.ToLower().Contains(buscar)) ||
                        (u.estado ? "activo" : "inactivo").Contains(buscar)
                    );
                }

                ViewBag.Buscar = buscar;
                return View(lista);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al listar usuarios: " + ex.Message;
                return View(new List<Usuario>());
            }
        }

        public ActionResult Create()
        {
            try
            {
                if (!TienePermiso("Usuario_Crear"))
                {
                    TempData["Error"] = "No tienes permiso para crear usuarios.";
                    return RedirectToAction("Index");
                }

                CargarRoles();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar formulario: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Usuario u)
        {
            try
            {
                if (!TienePermiso("Usuario_Crear"))
                {
                    TempData["Error"] = "No tienes permiso para crear usuarios.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    if (datos.ExisteUsuario(u.usuario))
                    {
                        ModelState.AddModelError("usuario", "El nombre de usuario ya existe.");
                        CargarRoles(u.ROL_id_rol);
                        return View(u);
                    }

                    if (datos.ExisteCorreo(u.correo))
                    {
                        ModelState.AddModelError("correo", "El correo ya está registrado.");
                        CargarRoles(u.ROL_id_rol);
                        return View(u);
                    }

                    u.password = ObtenerSHA256(u.password);

                    if (datos.AgregarUsuario(u))
                    {
                        TempData["Exito"] = "Usuario agregado correctamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo agregar el usuario.");
                    }
                }

                CargarRoles(u.ROL_id_rol);
                return View(u);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al agregar usuario: " + ex.Message);
                CargarRoles(u.ROL_id_rol);
                return View(u);
            }
        }

        public ActionResult Edit(int id)
        {
            try
            {
                if (!PuedeEditar("Usuario"))
                {
                    TempData["Error"] = "No tienes permiso para editar usuarios.";
                    return RedirectToAction("Index");
                }

                Usuario usuario = datos.ListarUsuarios().Find(u => u.id_usuario == id);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                if (usuario.ROL_id_rol == 0 && !string.IsNullOrEmpty(usuario.Rol))
                {
                    CDRol cdRol = new CDRol();
                    var roles = cdRol.ListarRoles();
                    var rolEncontrado = roles.FirstOrDefault(r => r.Nombre == usuario.Rol);
                    if (rolEncontrado != null)
                    {
                        usuario.ROL_id_rol = rolEncontrado.id_rol;
                    }
                }

                if (usuario.ROL_id_rol > 0)
                {
                    CDRol cdRol = new CDRol();
                    var roles = cdRol.ListarRoles();
                    bool rolExiste = roles.Any(r => r.id_rol == usuario.ROL_id_rol);
                    if (!rolExiste && roles.Any())
                    {
                        usuario.ROL_id_rol = roles.First().id_rol;
                    }
                }

                CargarRoles(usuario.ROL_id_rol);
                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar usuario: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Usuario u)
        {
            try
            {
                if (!PuedeEditar("Usuario"))
                {
                    TempData["Error"] = "No tienes permiso para editar usuarios.";
                    return RedirectToAction("Index");
                }

                if (u.ROL_id_rol == 0)
                {
                    ModelState.AddModelError("ROL_id_rol", "Debe seleccionar un rol válido.");
                    CargarRoles(0);
                    return View(u);
                }

                if (ModelState.IsValid)
                {
                    var usuarioExistente = datos.ListarUsuarios().FirstOrDefault(usr => usr.id_usuario == u.id_usuario);
                    if (usuarioExistente == null)
                    {
                        ModelState.AddModelError("", "El usuario ya no existe en la base de datos.");
                        CargarRoles(u.ROL_id_rol);
                        return View(u);
                    }

                    if (datos.ExisteUsuario(u.usuario, u.id_usuario))
                    {
                        ModelState.AddModelError("usuario", "El nombre de usuario ya existe.");
                        CargarRoles(u.ROL_id_rol);
                        return View(u);
                    }

                    if (datos.ExisteCorreo(u.correo, u.id_usuario))
                    {
                        ModelState.AddModelError("correo", "El correo ya está registrado.");
                        CargarRoles(u.ROL_id_rol);
                        return View(u);
                    }

                    if (!string.IsNullOrEmpty(u.password))
                    {

                        if (u.password.Length < 6)
                        {
                            ModelState.AddModelError("password", "La contraseña debe tener al menos 6 caracteres.");
                            CargarRoles(u.ROL_id_rol);
                            return View(u);
                        }
                        u.password = ObtenerSHA256(u.password);
                    }
                    else
                    {
                        u.password = usuarioExistente.password;
                    }

                    u.ROL_id_rol = u.ROL_id_rol > 0 ? u.ROL_id_rol : usuarioExistente.ROL_id_rol;

                    if (datos.ModificarUsuario(u))
                    {
                        TempData["Exito"] = "Usuario modificado correctamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo modificar el usuario.");
                    }
                }

                CargarRoles(u.ROL_id_rol);
                return View(u);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al modificar usuario: " + ex.Message);
                CargarRoles(u.ROL_id_rol);
                return View(u);
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                if (!PuedeEliminar("Usuario"))
                {
                    TempData["Error"] = "No tienes permiso para eliminar usuarios.";
                    return RedirectToAction("Index");
                }

                Usuario usuario = datos.ListarUsuarios().Find(u => u.id_usuario == id);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar usuario: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (!PuedeEliminar("Usuario"))
                {
                    TempData["Error"] = "No tienes permiso para eliminar usuarios.";
                    return RedirectToAction("Index");
                }

                var usuario = datos.ListarUsuarios().Find(u => u.id_usuario == id);
                if (usuario != null && usuario.Rol == "Administrador")
                {
                    var administradores = datos.ListarUsuarios().Count(u => u.Rol == "Administrador" && u.estado == true);
                    if (administradores <= 1)
                    {
                        TempData["Error"] = "No se puede eliminar el último administrador del sistema.";
                        return RedirectToAction("Index");
                    }
                }

                // Se envía la variable 'mensaje' para recibir la respuesta de la BD
                string mensaje;
                bool eliminado = datos.EliminarUsuario(id, out mensaje);

                if (eliminado)
                {
                    TempData["Exito"] = !string.IsNullOrEmpty(mensaje) ? mensaje : "Usuario eliminado correctamente.";
                }
                else
                {
                    TempData["Error"] = !string.IsNullOrEmpty(mensaje) ? mensaje : "No se pudo eliminar el usuario.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar usuario: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private void CargarRoles(int? idSeleccionado = null)
        {
            try
            {
                CDRol cdRol = new CDRol();
                List<CMRol> listaRoles = cdRol.ListarRoles();

                if (listaRoles == null || !listaRoles.Any())
                {
                    ViewBag.ListaRoles = new SelectList(new List<SelectListItem>());
                    return;
                }

                if (!idSeleccionado.HasValue || idSeleccionado.Value == 0)
                {
                    idSeleccionado = listaRoles.First().id_rol;
                }

                bool existeRol = listaRoles.Any(r => r.id_rol == idSeleccionado.Value);
                if (!existeRol)
                {
                    idSeleccionado = listaRoles.First().id_rol;
                }

                var selectList = new SelectList(
                    listaRoles,
                    "id_rol",
                    "Nombre",
                    idSeleccionado
                );

                ViewBag.ListaRoles = selectList;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar roles: " + ex.Message;
                ViewBag.ListaRoles = new SelectList(new List<SelectListItem>());
            }
        }

        private string ObtenerSHA256(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
        private Usuario ObtenerUsuarioPorId(int id)
        {
            try
            {
                return datos.ListarUsuarios().FirstOrDefault(u => u.id_usuario == id);
            }
            catch
            {
                return null;
            }
        }
    }
}