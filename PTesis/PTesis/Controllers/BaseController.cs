using System.Web.Mvc;
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

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (UsuarioSesion == null && !(filterContext.Controller is LoginController))
            {
                filterContext.Result = RedirectToAction("Index", "Login");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}