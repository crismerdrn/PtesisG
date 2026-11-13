using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class StockController : BaseController
    {
        private CDStock cdStock = new CDStock();
        private CDUsuario cdUsuario = new CDUsuario();

        #region VISTAS PRINCIPALES

        // GET: Stock/Productos
        public ActionResult Productos(int? id_polleria = null, string buscar = "")
        {
            if (!TienePermiso("Stock"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Stock.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = TienePermiso("Stock");
            ViewBag.PuedeEditar = PuedeEditar("Stock");
            ViewBag.PuedeAjustar = TienePermiso("Stock_Ajustar");

            List<CMStock> lista;

            if (!string.IsNullOrEmpty(buscar))
            {
                // Filtrar por búsqueda en memoria (o puedes modificar el stored procedure)
                lista = cdStock.ListarStockProductos(id_polleria)
                    .Where(x => x.producto_nombre.ToLower().Contains(buscar.ToLower()) ||
                               x.producto_descripcion.ToLower().Contains(buscar.ToLower()))
                    .ToList();
                ViewBag.Buscar = buscar;
            }
            else
            {
                lista = cdStock.ListarStockProductos(id_polleria);
            }

            // Obtener lista de pollerías para el dropdown
            var cdPolleria = new CDPolleria();
            ViewBag.Pollerias = cdPolleria.Listar()
                .Select(p => new SelectListItem
                {
                    Value = p.id_polleria.ToString(),
                    Text = p.Nombre
                }).ToList();

            ViewBag.IdPolleriaFiltro = id_polleria;
            return View(lista);
        }

        // GET: Stock/Insumos
        public ActionResult Insumos(int? id_polleria = null, string buscar = "")
        {
            if (!TienePermiso("Stock"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Stock.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = TienePermiso("Stock");
            ViewBag.PuedeEditar = PuedeEditar("Stock");
            ViewBag.PuedeAjustar = TienePermiso("Stock_Ajustar");

            List<CMStockInsumo> lista;

            if (!string.IsNullOrEmpty(buscar))
            {
                lista = cdStock.ListarStockInsumos(id_polleria)
                    .Where(x => x.insumo_nombre.ToLower().Contains(buscar.ToLower()) ||
                               x.insumo_descripcion.ToLower().Contains(buscar.ToLower()))
                    .ToList();
                ViewBag.Buscar = buscar;
            }
            else
            {
                lista = cdStock.ListarStockInsumos(id_polleria);
            }

            var cdPolleria = new CDPolleria();
            ViewBag.Pollerias = cdPolleria.Listar()
                .Select(p => new SelectListItem
                {
                    Value = p.id_polleria.ToString(),
                    Text = p.Nombre
                }).ToList();

            ViewBag.IdPolleriaFiltro = id_polleria;
            return View(lista);
        }

        #endregion

        #region STOCK BAJO

        // GET: Stock/StockBajo
        public ActionResult StockBajo(string tipo = "productos", int? id_polleria = null)
        {
            if (!TienePermiso("Stock"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Stock.";
                return RedirectToAction("Index", "Home");
            }

            if (tipo == "productos")
            {
                var productosBajoStock = cdStock.ObtenerProductosStockBajo(id_polleria);
                ViewBag.Tipo = "productos";

                var cdPolleria = new CDPolleria();
                ViewBag.Pollerias = cdPolleria.Listar()
                    .Select(p => new SelectListItem
                    {
                        Value = p.id_polleria.ToString(),
                        Text = p.Nombre
                    }).ToList();

                ViewBag.IdPolleriaFiltro = id_polleria;
                return View("StockBajoProductos", productosBajoStock);
            }
            else
            {
                var insumosBajoStock = cdStock.ObtenerInsumosStockBajo(id_polleria);
                ViewBag.Tipo = "insumos";

                var cdPolleria = new CDPolleria();
                ViewBag.Pollerias = cdPolleria.Listar()
                    .Select(p => new SelectListItem
                    {
                        Value = p.id_polleria.ToString(),
                        Text = p.Nombre
                    }).ToList();

                ViewBag.IdPolleriaFiltro = id_polleria;
                return View("StockBajoInsumos", insumosBajoStock);
            }
        }

        #endregion

        #region AJUSTES DE STOCK

        // GET: Stock/AjustarProducto
        public ActionResult AjustarProducto(int id)
        {
            if (!TienePermiso("Stock_Ajustar"))
            {
                TempData["Error"] = "No tiene permisos para ajustar stock.";
                return RedirectToAction("Productos");
            }

            var stock = cdStock.ObtenerStockProductoPorId(id);

            if (stock == null)
            {
                TempData["Error"] = "Registro de stock no encontrado.";
                return RedirectToAction("Productos");
            }

            var ajuste = new CMStockAjuste
            {
                id_item = stock.PRODUCTO_id_producto,
                tipo_item = "PRODUCTO",
                id_polleria = stock.POLLERIA_id_polleria,
                item_nombre = stock.producto_nombre,
                polleria_nombre = stock.polleria_nombre,
                stock_actual = stock.stock_actual
            };

            return View(ajuste);
        }

        // POST: Stock/AjustarProducto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjustarProducto(CMStockAjuste ajuste)
        {
            try
            {
                if (!TienePermiso("Stock_Ajustar"))
                {
                    TempData["Error"] = "No tiene permisos para ajustar stock.";
                    return RedirectToAction("Productos");
                }

                if (ModelState.IsValid)
                {
                    // Obtener usuario actual (necesitas implementar esto según tu sistema de autenticación)
                    ajuste.id_usuario = ObtenerUsuarioId(); // Método que debes implementar

                    string mensaje = string.Empty;
                    bool resultado = cdStock.AjustarStockProducto(ajuste, out mensaje);

                    if (resultado)
                    {
                        TempData["Exito"] = mensaje;
                        return RedirectToAction("Productos", new { id_polleria = ajuste.id_polleria });
                    }
                    else
                    {
                        TempData["Error"] = mensaje;
                    }
                }

                // Recargar datos para mostrar si hay error
                var stock = cdStock.ObtenerStockProductoPorId(ajuste.id_item);
                if (stock != null)
                {
                    ajuste.item_nombre = stock.producto_nombre;
                    ajuste.polleria_nombre = stock.polleria_nombre;
                    ajuste.stock_actual = stock.stock_actual;
                }

                return View(ajuste);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(ajuste);
            }
        }

        // GET: Stock/AjustarInsumo
        public ActionResult AjustarInsumo(int id)
        {
            if (!TienePermiso("Stock_Ajustar"))
            {
                TempData["Error"] = "No tiene permisos para ajustar stock.";
                return RedirectToAction("Insumos");
            }

            // Necesitarías un método similar para obtener stock de insumo por ID
            // Por ahora uso una aproximación
            var listaInsumos = cdStock.ListarStockInsumos();
            var stockInsumo = listaInsumos.FirstOrDefault(x => x.id_stock_insumo == id);

            if (stockInsumo == null)
            {
                TempData["Error"] = "Registro de stock no encontrado.";
                return RedirectToAction("Insumos");
            }

            var ajuste = new CMStockAjuste
            {
                id_item = stockInsumo.INSUMO_id_insumo,
                tipo_item = "INSUMO",
                id_polleria = stockInsumo.POLLERIA_id_polleria,
                item_nombre = stockInsumo.insumo_nombre,
                polleria_nombre = stockInsumo.polleria_nombre,
                stock_actual = stockInsumo.stock_actual
            };

            return View(ajuste);
        }

        // POST: Stock/AjustarInsumo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjustarInsumo(CMStockAjuste ajuste)
        {
            try
            {
                if (!TienePermiso("Stock_Ajustar"))
                {
                    TempData["Error"] = "No tiene permisos para ajustar stock.";
                    return RedirectToAction("Insumos");
                }

                if (ModelState.IsValid)
                {
                    ajuste.id_usuario = ObtenerUsuarioId();

                    string mensaje = string.Empty;
                    bool resultado = cdStock.AjustarStockInsumo(ajuste, out mensaje);

                    if (resultado)
                    {
                        TempData["Exito"] = mensaje;
                        return RedirectToAction("Insumos", new { id_polleria = ajuste.id_polleria });
                    }
                    else
                    {
                        TempData["Error"] = mensaje;
                    }
                }

                return View(ajuste);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(ajuste);
            }
        }

        #endregion

        #region HISTORIAL Y MOVIMIENTOS

        // GET: Stock/Historial
        public ActionResult Historial(
            int? id_polleria = null,
            string fecha_desde = null,
            string fecha_hasta = null,
            string tipo_item = null,
            string tipo_movimiento = null,
            int? id_item = null)
        {
            if (!TienePermiso("Stock_Historial"))
            {
                TempData["Error"] = "No tiene permisos para ver el historial.";
                return RedirectToAction("Index", "Home");
            }

            DateTime? fechaDesde = null;
            DateTime? fechaHasta = null;

            if (!string.IsNullOrEmpty(fecha_desde))
                fechaDesde = DateTime.Parse(fecha_desde);
            if (!string.IsNullOrEmpty(fecha_hasta))
                fechaHasta = DateTime.Parse(fecha_hasta);

            var historial = cdStock.ObtenerHistorialMovimientos(
                id_polleria, fechaDesde, fechaHasta, tipo_item, tipo_movimiento, id_item);

            // Obtener datos para filtros
            var cdPolleria = new CDPolleria();
            ViewBag.Pollerias = cdPolleria.Listar()
                .Select(p => new SelectListItem
                {
                    Value = p.id_polleria.ToString(),
                    Text = p.Nombre,
                    Selected = id_polleria == p.id_polleria
                }).ToList();

            // Lista de tipos de items
            ViewBag.TiposItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos" },
                new SelectListItem { Value = "PRODUCTO", Text = "Productos" },
                new SelectListItem { Value = "INSUMO", Text = "Insumos" }
            };

            // Lista de tipos de movimiento
            ViewBag.TiposMovimiento = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos" },
                new SelectListItem { Value = "VENTA", Text = "Venta" },
                new SelectListItem { Value = "COMPRA", Text = "Compra" },
                new SelectListItem { Value = "AJUSTE", Text = "Ajuste" },
                new SelectListItem { Value = "TRANSFERENCIA", Text = "Transferencia" },
                new SelectListItem { Value = "PRODUCCION", Text = "Producción" }
            };

            ViewBag.FechaDesde = fecha_desde;
            ViewBag.FechaHasta = fecha_hasta;
            ViewBag.TipoItemSeleccionado = tipo_item;
            ViewBag.TipoMovimientoSeleccionado = tipo_movimiento;

            return View(historial);
        }

        // GET: Stock/Kardex/5
        public ActionResult Kardex(int id_producto, int id_polleria, string fecha_desde = null, string fecha_hasta = null)
        {
            if (!TienePermiso("Stock_Historial"))
            {
                TempData["Error"] = "No tiene permisos para ver el kardex.";
                return RedirectToAction("Productos");
            }

            DateTime? fechaDesde = null;
            DateTime? fechaHasta = null;

            if (!string.IsNullOrEmpty(fecha_desde))
                fechaDesde = DateTime.Parse(fecha_desde);
            if (!string.IsNullOrEmpty(fecha_hasta))
                fechaHasta = DateTime.Parse(fecha_hasta);

            var kardex = cdStock.ObtenerKardexProducto(id_producto, id_polleria, fechaDesde, fechaHasta);

            // Obtener información del producto
            var stock = cdStock.ListarStockProductos(id_polleria)
                .FirstOrDefault(x => x.PRODUCTO_id_producto == id_producto);

            if (stock == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction("Productos");
            }

            ViewBag.ProductoNombre = stock.producto_nombre;
            ViewBag.PolleriaNombre = stock.polleria_nombre;
            ViewBag.StockActual = stock.stock_actual;
            ViewBag.StockMinimo = stock.stock_minimo;
            ViewBag.IdProducto = id_producto;
            ViewBag.IdPolleria = id_polleria;
            ViewBag.FechaDesde = fecha_desde;
            ViewBag.FechaHasta = fecha_hasta;

            return View(kardex);
        }

        #endregion

        #region CONFIGURACIÓN Y TRANSFERENCIAS

        // GET: Stock/ConfigurarMinimo
        public ActionResult ConfigurarMinimo(int id, string tipo = "producto")
        {
            if (!TienePermiso("Stock_Configurar"))
            {
                TempData["Error"] = "No tiene permisos para configurar stock mínimo.";
                return RedirectToAction(tipo == "producto" ? "Productos" : "Insumos");
            }

            ViewBag.Tipo = tipo;
            ViewBag.Id = id;

            if (tipo == "producto")
            {
                var stock = cdStock.ObtenerStockProductoPorId(id);
                if (stock == null)
                {
                    TempData["Error"] = "Registro de stock no encontrado.";
                    return RedirectToAction("Productos");
                }

                return View(stock);
            }
            else
            {
                // Similar para insumos
                return RedirectToAction("Insumos");
            }
        }

        // POST: Stock/ConfigurarMinimo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfigurarMinimo(int id, string tipo, decimal stock_minimo)
        {
            try
            {
                if (!TienePermiso("Stock_Configurar"))
                {
                    TempData["Error"] = "No tiene permisos para configurar stock mínimo.";
                    return RedirectToAction(tipo == "producto" ? "Productos" : "Insumos");
                }

                string mensaje = string.Empty;
                bool resultado = false;
                int usuarioId = ObtenerUsuarioId();

                if (tipo == "producto")
                {
                    var stock = cdStock.ObtenerStockProductoPorId(id);
                    if (stock != null)
                    {
                        resultado = cdStock.ActualizarStockMinimoProducto(
                            stock.PRODUCTO_id_producto,
                            stock.POLLERIA_id_polleria,
                            stock_minimo,
                            usuarioId,
                            out mensaje);
                    }
                }
                else
                {
                    // Para insumos
                    // Implementar similar al de productos
                }

                if (resultado)
                {
                    TempData["Exito"] = mensaje;
                }
                else
                {
                    TempData["Error"] = mensaje;
                }

                return RedirectToAction(tipo == "producto" ? "Productos" : "Insumos");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction(tipo == "producto" ? "Productos" : "Insumos");
            }
        }

        // GET: Stock/Transferir
        public ActionResult Transferir()
        {
            if (!TienePermiso("Stock_Transferir"))
            {
                TempData["Error"] = "No tiene permisos para transferir stock.";
                return RedirectToAction("Productos");
            }

            var transferencia = new CMTransfersVentaStock();

            // Obtener productos
            var cdProducto = new CDProducto();
            var productos = cdProducto.ListarProductos(null).Where(p => p.Estado == true).ToList();
            ViewBag.Productos = productos.Select(p => new SelectListItem
            {
                Value = p.id_producto.ToString(),
                Text = p.Nombre
            }).ToList();

            // Obtener pollerías
            var cdPolleria = new CDPolleria();
            var pollerias = cdPolleria.Listar().Where(p => p.Estado == true).ToList();
            ViewBag.Pollerias = pollerias.Select(p => new SelectListItem
            {
                Value = p.id_polleria.ToString(),
                Text = p.Nombre
            }).ToList();

            return View(transferencia);
        }

        // POST: Stock/Transferir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Transferir(CMStockTransferVenta transferencia)
        {
            try
            {
                if (!TienePermiso("Stock_Transferir"))
                {
                    TempData["Error"] = "No tiene permisos para transferir stock.";
                    return RedirectToAction("Productos");
                }

                if (ModelState.IsValid)
                {
                    // Validar que origen y destino sean diferentes
                    if (transferencia.id_polleria_origen == transferencia.id_polleria_destino)
                    {
                        TempData["Error"] = "La pollería origen y destino no pueden ser la misma.";
                        return View(transferencia);
                    }

                    // Validar stock disponible
                    decimal stockDisponible = cdStock.ObtenerStockDisponibleProducto(
                        transferencia.id_producto, transferencia.id_polleria_origen);

                    if (stockDisponible < transferencia.cantidad)
                    {
                        TempData["Error"] = $"Stock insuficiente. Disponible: {stockDisponible}";
                        return View(transferencia);
                    }

                    // Obtener usuario actual
                    transferencia.id_usuario = ObtenerUsuarioId();

                    string mensaje = string.Empty;
                    bool resultado = cdStock.TransferirStockEntrePollerias(transferencia, out mensaje);

                    if (resultado)
                    {
                        TempData["Exito"] = mensaje;
                        return RedirectToAction("Productos");
                    }
                    else
                    {
                        TempData["Error"] = mensaje;
                    }
                }

                // Recargar datos para el dropdown en caso de error
                var cdProducto = new CDProducto();
                var productos = cdProducto.ListarProductos(null).Where(p => p.Estado == true).ToList();
                ViewBag.Productos = productos.Select(p => new SelectListItem
                {
                    Value = p.id_producto.ToString(),
                    Text = p.Nombre,
                    Selected = p.id_producto == transferencia.id_producto
                }).ToList();

                var cdPolleria = new CDPolleria();
                var pollerias = cdPolleria.Listar().Where(p => p.Estado == true).ToList();
                ViewBag.Pollerias = pollerias.Select(p => new SelectListItem
                {
                    Value = p.id_polleria.ToString(),
                    Text = p.Nombre
                }).ToList();

                return View(transferencia);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(transferencia);
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES

        // Método para obtener el ID del usuario actual (debes implementarlo según tu sistema)
        private int ObtenerUsuarioId()
        {
            if (Session["Usuario"] != null && Session["Usuario"] is Usuario)
            {
                return ((Usuario)Session["Usuario"]).id_usuario;
            }
            return 1;
        }

        private Usuario ObtenerUsuarioActual()
        {
            if (Session["Usuario"] != null && Session["Usuario"] is Usuario)
            {
                return (Usuario)Session["Usuario"];
            }
            return null;
        }

        // GET: Stock/ObtenerStockProducto (para AJAX)
        [HttpGet]
        public JsonResult ObtenerStockProducto(int id_producto, int id_polleria)
        {
            try
            {
                decimal stock = cdStock.ObtenerStockDisponibleProducto(id_producto, id_polleria);
                return Json(new { stock = stock }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Stock/ObtenerStockInsumo (para AJAX)
        [HttpGet]
        public JsonResult ObtenerStockInsumo(int id_insumo, int id_polleria)
        {
            try
            {
                decimal stock = cdStock.ObtenerStockDisponibleInsumo(id_insumo, id_polleria);
                return Json(new { stock = stock }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Stock/ValidarStock (para AJAX)
        [HttpGet]
        public JsonResult ValidarStock(int id_producto, int id_polleria, decimal cantidad)
        {
            try
            {
                string mensaje = string.Empty;
                bool resultado = cdStock.ValidarStockParaVenta(id_producto, id_polleria, cantidad, out mensaje);

                return Json(new
                {
                    valido = resultado,
                    mensaje = mensaje
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    valido = false,
                    mensaje = $"Error: {ex.Message}"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion
    }
}