using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace PTesis
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // RUTA ESPECÍFICA para InsumoProducto(DEBE IR PRIMERO)
    routes.MapRoute(
        name: "InsumoProductoEditar",
        url: "InsumoProducto/Edit/{id}",
        defaults: new { controller = "InsumoProducto", action = "Edit" }
    );

            routes.MapRoute(
                name: "InsumoProductoEliminar",
                url: "InsumoProducto/Delete/{id}",
                defaults: new { controller = "InsumoProducto", action = "Delete" }
            );


            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Login", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
