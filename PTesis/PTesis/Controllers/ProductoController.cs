using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using PagedList;

namespace PTesis.Controllers
{
    public class ProductoController : BaseController
    {
        private CDProducto cdProducto = new CDProducto();
        private CDMedida cdMedida = new CDMedida();
        private CDTipoProducto cdTipoProducto = new CDTipoProducto();

        // --- LECTURA (INDEX) ---

        public ActionResult Index(string buscar = "", int? idTipoProducto = null, int? pagina = null)
        {
            int pageSize = 10;  // Cantidad de productos por página
            int pageNumber = (pagina ?? 1);  // Página actual (1 si no se especifica)

            var lista = cdProducto.ListarProductos(idTipoProducto).AsQueryable();

            // Filtrar por búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                lista = lista.Where(p =>
                    p.Nombre.ToLower().Contains(buscar.ToLower()) ||
                    p.Descripcion.ToLower().Contains(buscar.ToLower())
                ).AsQueryable();
                ViewBag.Buscar = buscar;
            }

            // Ordenar y paginar
            var productosPaginados = lista.OrderByDescending(p => p.id_producto)
                                           .ToPagedList(pageNumber, pageSize);

            CargarDependencias();
            ViewBag.IdTipoProductoSeleccionado = idTipoProducto;

            return View(productosPaginados);
        }

        // --- CREAR ---

        public ActionResult Create()
        {
            CargarDependencias();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMProducto producto)
        {
            if (ModelState.IsValid)
            {
                int idGenerado;
                string mensaje;
                bool ok = cdProducto.RegistrarProducto(producto, out idGenerado, out mensaje);

                if (ok)
                {
                    TempData["Exito"] = "Producto registrado exitosamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", mensaje);
                    TempData["Error"] = mensaje;
                }
            }

            CargarDependencias();
            return View(producto);
        }

        // --- EDITAR ---

        public ActionResult Edit(int id)
        {
            var producto = cdProducto.ObtenerProductoPorId(id);

            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction("Index");
            }

            CargarDependencias();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CMProducto producto)
        {
            try
            {
                var precioFormValue = Request.Form["PrecioVenta"];
                System.Diagnostics.Debug.WriteLine($"Valor crudo de PrecioVenta: '{precioFormValue}'");

                if (!string.IsNullOrEmpty(precioFormValue))
                {
                    precioFormValue = precioFormValue.Replace(",", ".");

                    // Intentar convertir a decimal
                    if (decimal.TryParse(precioFormValue,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal precioDecimal))
                    {
                        producto.PrecioVenta = precioDecimal;

                        ModelState.Remove("PrecioVenta");

                        System.Diagnostics.Debug.WriteLine($"Precio convertido: {producto.PrecioVenta}");
                    }
                    else
                    {
                        ModelState.AddModelError("PrecioVenta", "Formato de precio inválido. Use formato 0.00");
                        TempData["Error"] = "Formato de precio inválido. Use formato 0.00";
                    }
                }

                if (ModelState.IsValid)
                {
                    string mensaje;
                    bool ok = cdProducto.ActualizarProducto(producto, out mensaje);

                    if (ok)
                    {
                        TempData["Exito"] = "Producto actualizado exitosamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", mensaje);
                        TempData["Error"] = mensaje;
                    }
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error de validación: {error.ErrorMessage}");
                    }
                    TempData["Error"] = "Por favor corrija los errores del formulario.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Excepción en Edit POST: {ex.Message}");
                TempData["Error"] = $"Error: {ex.Message}";
            }

            CargarDependencias();
            return View(producto);
        }

        // --- ELIMINAR (LÓGICO) ---

        public ActionResult Delete(int id)
        {
            var producto = cdProducto.ObtenerProductoPorId(id);

            if (producto == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction("Index");
            }

            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            string mensaje;
            bool ok = cdProducto.EliminarProducto(id, out mensaje);

            if (ok)
            {
                TempData["Exito"] = "Producto eliminado (inactivado) exitosamente.";
            }
            else
            {
                TempData["Error"] = "Error al eliminar el producto: " + mensaje;
            }

            return RedirectToAction("Index");
        }

        // --- MÉTODOS AUXILIARES ---

        private void CargarDependencias()
        {
            ViewBag.ListaMedidas = new SelectList(cdMedida.ListarMedidasActivas(), "id_medida", "Abreviatura");
            ViewBag.ListaTiposProducto = new SelectList(cdTipoProducto.ListarTipoProductos(), "id_tipoProducto", "descripcion");
        }
    }
}