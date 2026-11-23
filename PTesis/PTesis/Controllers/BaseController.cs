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

        // Los controladores usan claves internas; la matriz de permisos guarda el nombre visible del submenú.
        private static readonly Dictionary<string, string[]> AliasModulos = new Dictionary<string, string[]>
        {
            { "Pollerias", new[] { "Pollería", "Polleria" } },
            { "InsumosProductos", new[] { "Insumo de Producto", "InsumoProducto" } },
            { "Stock", new[] { "Stock General", "StockGeneral" } },
            { "Reportes", new[] { "Reporte de Ventas", "Reporte de Compras", "Reporte de Stock", "ReporteVentas", "ReporteCompras", "ReporteStock" } },
            { "TipoPago", new[] { "Tipo de Pago" } },
            { "TipoProducto", new[] { "Tipo de Producto" } },
            { "Usuario", new[] { "Usuarios" } },
            { "Compras", new[] { "Compra" } },
            { "Ventas", new[] { "Venta" } }
        };

        // Acciones de Stock sin fila propia en la matriz: se derivan de los permisos de Stock General.
        private static readonly Dictionary<string, string> PermisosStockLectura = new Dictionary<string, string>
        {
            { "Stock_Historial", "Stock" }
        };

        private static readonly Dictionary<string, string> PermisosStockEdicion = new Dictionary<string, string>
        {
            { "Stock_Ajustar", "Stock" },
            { "Stock_Configurar", "Stock" },
            { "Stock_Transferir", "Stock" }
        };

        private static IEnumerable<string> NombresModulo(string modulo)
        {
            yield return modulo;
            string[] alias;
            if (AliasModulos.TryGetValue(modulo, out alias))
            {
                foreach (var a in alias) yield return a;
            }
        }

        private bool TieneAlguno(string modulo, params string[] prefijos)
        {
            return NombresModulo(modulo).Any(nombre => prefijos.Any(prefijo => UsuarioSesion.TienePermiso(prefijo + nombre)));
        }

        protected bool TienePermiso(string nombreMenu)
        {
            if (UsuarioSesion == null) return false;
            if (EsAdmin) return true;

            if (UsuarioSesion.TienePermiso(nombreMenu)) return true;

            string moduloBase;
            if (PermisosStockLectura.TryGetValue(nombreMenu, out moduloBase))
                return TienePermiso(moduloBase) || TieneAlguno("Reporte de Stock", "", "Ver ");
            if (PermisosStockEdicion.TryGetValue(nombreMenu, out moduloBase))
                return PuedeEditar(moduloBase);

            return TieneAlguno(nombreMenu, "", "Ver ", "Agregar ", "Crear ", "Editar ");
        }

        protected bool PuedeCrear(string modulo)
        {
            if (UsuarioSesion == null) return false;
            if (EsAdmin) return true;

            return TieneAlguno(modulo, "Agregar ", "Crear ");
        }

        /// <summary>
        /// Valida permisos de edición admitiendo tanto el método del modelo como la acción "Editar [modulo]".
        /// </summary>
        protected bool PuedeEditar(string modulo)
        {
            if (UsuarioSesion == null) return false;
            if (EsAdmin) return true;

            if (TieneAlguno(modulo, "Editar ", "Modificar "))
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

            if (TieneAlguno(modulo, "Eliminar ", "Borrar "))
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
                if (ViewBag.UsuarioNombre == null)
                    ViewBag.UsuarioNombre = ((UsuarioSesion.nombre ?? "") + " " + (UsuarioSesion.apellido ?? "")).Trim();
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