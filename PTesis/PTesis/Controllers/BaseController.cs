using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    public class BaseController : Controller
    {
        protected Usuario UsuarioSesion
        {
            get { return Session["Usuario"] as Usuario; }
        }

        protected bool EsAdmin
        {
            get { return UsuarioSesion?.Rol == "Administrador"; }
        }

        /// <summary>
        /// Valida si el usuario tiene el permiso directo o alguna de sus acciones asociadas.
        /// No requiere cambiar llamadas existentes como TienePermiso("Clientes").
        /// </summary>
        /// <summary>
        /// Permisos que habilitan el acceso al controlador completo (basta con uno).
        /// </summary>
        protected virtual string[] PermisosModulo
        {
            get { return null; }
        }

        protected bool TienePermiso(string nombreMenu)
        {
            if (UsuarioSesion == null) return false;
            if (EsAdmin) return true;

            if (UsuarioSesion.TienePermiso(nombreMenu)) return true;

            if (UsuarioSesion.TienePermiso("Ver " + nombreMenu) ||
                UsuarioSesion.TienePermiso("Agregar " + nombreMenu) ||
                UsuarioSesion.TienePermiso("Crear " + nombreMenu) ||
                UsuarioSesion.TienePermiso("Editar " + nombreMenu))
            {
                return true;
            }

            return false;
        }

        protected bool PuedeCrear(string modulo)
        {
            if (UsuarioSesion == null) return false;
            if (EsAdmin) return true;

            return UsuarioSesion.TienePermiso("Agregar " + modulo) ||
                   UsuarioSesion.TienePermiso("Crear " + modulo);
        }

        /// <summary>
        /// Valida permisos de edición admitiendo tanto el método del modelo como la acción "Editar [modulo]".
        /// </summary>
        protected bool PuedeEditar(string modulo)
        {
            if (UsuarioSesion == null) return false;
            if (EsAdmin) return true;

            if (UsuarioSesion.TienePermiso("Editar " + modulo) || UsuarioSesion.TienePermiso("Modificar " + modulo))
                return true;

            return UsuarioSesion.PuedeEditar(modulo);
        }

        /// <summary>
        /// Valida permisos de eliminación admitiendo tanto el método del modelo como la acción "Eliminar [modulo]".
        /// </summary>
        protected bool PuedeEliminar(string modulo)
        {
            if (UsuarioSesion == null) return false;
            if (EsAdmin) return true;

            if (UsuarioSesion.TienePermiso("Eliminar " + modulo) || UsuarioSesion.TienePermiso("Borrar " + modulo))
                return true;

            return UsuarioSesion.PuedeEliminar(modulo);
        }

        private const string ClavePolleriaTrabajo = "IdPolleriaTrabajo";
        private List<CMPolleria> polleriasActivas;

        protected List<CMPolleria> ObtenerPolleriasActivas()
        {
            if (polleriasActivas == null)
            {
                try
                {
                    polleriasActivas = new CDPolleria().Listar()
                        .Where(p => p.Estado)
                        .OrderBy(p => p.id_polleria)
                        .ToList();
                }
                catch
                {
                    polleriasActivas = new List<CMPolleria>();
                }
            }
            return polleriasActivas;
        }

        /// <summary>
        /// Pollería (sucursal) en la que trabaja el usuario: la elegida en el menú superior,
        /// o la primera activa si no eligió ninguna.
        /// </summary>
        protected CMPolleria ObtenerPolleriaActiva()
        {
            var activas = ObtenerPolleriasActivas();
            var idSeleccionada = Session?[ClavePolleriaTrabajo] as int?;
            return activas.FirstOrDefault(p => p.id_polleria == idSeleccionada) ?? activas.FirstOrDefault();
        }

        protected bool EsPolleriaActiva(int idPolleria)
        {
            return ObtenerPolleriasActivas().Any(p => p.id_polleria == idPolleria);
        }

        protected void SeleccionarPolleriaTrabajo(int idPolleria)
        {
            Session[ClavePolleriaTrabajo] = idPolleria;
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext.Result is ViewResult && UsuarioSesion != null)
            {
                ViewBag.PolleriasTrabajo = ObtenerPolleriasActivas();
                ViewBag.PolleriaTrabajo = ObtenerPolleriaActiva();
            }
            base.OnResultExecuting(filterContext);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (UsuarioSesion == null && !(filterContext.Controller is LoginController))
            {
                filterContext.Result = RedirectToAction("Index", "Login");
                return;
            }

            var permisosModulo = PermisosModulo;
            if (UsuarioSesion != null && permisosModulo != null && permisosModulo.Length > 0 &&
                !System.Linq.Enumerable.Any(permisosModulo, TienePermiso))
            {
                TempData["Error"] = "No tiene permisos para acceder a este módulo.";
                filterContext.Result = RedirectToAction("Index", "Home");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}