using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Dapper;
using ModeloProductoStock = CapaModelo.CMProductoStock;


namespace PTesis.Controllers
{
    public class VentaController : BaseController
    {
        private CDVenta cdVenta = new CDVenta();
        private CDClientes cdClientes = new CDClientes();
        private CDProducto cdProducto = new CDProducto();
        private CDUsuario cdUsuario = new CDUsuario();

        #region Vistas principales

        // GET: Venta/Index
        public ActionResult Index(string buscar = "", string estado = "", string fechaDesde = "", string fechaHasta = "",
                                 int? idCliente = null, string tipoVenta = "")
        {
            if (!TienePermiso("Ventas"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Ventas.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = TienePermiso("Ventas");
            ViewBag.PuedeEditar = PuedeEditar("Ventas");
            ViewBag.PuedeEliminar = PuedeEliminar("Ventas");
            ViewBag.PuedeAnular = PuedeEditar("Ventas");

            // Parsear fechas
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;

            if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out DateTime desde))
                fechaInicio = desde;

            if (!string.IsNullOrEmpty(fechaHasta) && DateTime.TryParse(fechaHasta, out DateTime hasta))
                fechaFin = hasta;

            var usuarioActual = ObtenerUsuarioActual();
            ViewBag.UsuarioActual = usuarioActual;

            int? idUsuarioFiltro = null;

            if (usuarioActual != null && !TienePermiso("Administrador"))
            {
                idUsuarioFiltro = usuarioActual.id_usuario;
            }

            var polleriaActual = ObtenerPolleriaActiva();
            ViewBag.Polleria = polleriaActual;

            int? idPolleria = polleriaActual?.id_polleria;

            // Obtener lista de ventas (ajustar según tus métodos existentes)
            // var lista = cdVenta.ListarVentas(estado, fechaInicio, fechaFin, idCliente, idUsuarioFiltro, idPolleria);
            var lista = new List<CMVenta>(); // Reemplazar con tu método real

            // Filtro de búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                string textoBusqueda = buscar.Trim().ToLower();
                lista = lista.Where(v =>
                    v.NumeroVenta?.ToLower().Contains(textoBusqueda) == true ||
                    v.NombreCliente?.ToLower().Contains(textoBusqueda) == true ||
                    v.NombreVendedor?.ToLower().Contains(textoBusqueda) == true ||
                    v.TelefonoCliente?.ToLower().Contains(textoBusqueda) == true
                ).ToList();

                ViewBag.Buscar = buscar;
            }

            // Filtro por tipo de venta
            if (!string.IsNullOrEmpty(tipoVenta))
            {
                lista = lista.Where(v => v.Tipo_venta == tipoVenta).ToList();
            }

            ViewBag.Estado = estado;
            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            ViewBag.TipoVenta = tipoVenta;
            ViewBag.IdCliente = idCliente;

            // Lista de clientes
            var clientes = cdClientes.Listar();
            ViewBag.ListaClientes = new SelectList(clientes, "id_cliente", "NombreCompleto", idCliente);

            var tiposPago = ObtenerTiposPagoDesdeBD();

            if (tiposPago == null || !tiposPago.Any())
            {
                ViewBag.ListaTiposVenta = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "TODOS", Selected = string.IsNullOrEmpty(tipoVenta) }
        };
                ViewBag.WarningTiposPago = "No hay tipos de pago configurados en el sistema.";
            }
            else
            {
                var listaTipos = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "TODOS", Selected = string.IsNullOrEmpty(tipoVenta) }
        };
                foreach (var tipo in tiposPago)
                {
                    listaTipos.Add(new SelectListItem
                    {
                        Value = tipo.Value,
                        Text = tipo.Text,
                        Selected = tipoVenta == tipo.Value
                    });
                }

                ViewBag.ListaTiposVenta = listaTipos;
            }

            return View(lista);
        }

        // GET: Venta/Create
        public ActionResult Create()
        {
            if (!TienePermiso("Ventas"))
            {
                TempData["Error"] = "No tiene permisos para crear Ventas.";
                return RedirectToAction("Index");
            }

            CargarDatosParaVista();

            var venta = new CMVenta
            {
                Estado = "PENDIENTE",
                Fecha_venta = DateTime.Now,
                hora_venta = DateTime.Now.TimeOfDay,
                Tipo_venta = "EFECTIVO"
            };

            var usuarioActual = ObtenerUsuarioActual();
            if (usuarioActual != null)
            {
                venta.USUARIOS_id_usuario = usuarioActual.id_usuario;
                venta.NombreVendedor = $"{usuarioActual.nombre} {usuarioActual.apellido}";
            }

            return View(venta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMVenta venta, string detallesJson)
        {
            try
            {
                if (!TienePermiso("Ventas"))
                {
                    TempData["Error"] = "No tiene permisos para crear Ventas.";
                    return RedirectToAction("Index");
                }

                // 1. Asignar usuario y pollería activa
                var usuarioActual = ObtenerUsuarioActual();
                var polleria = ObtenerPolleriaActiva();

                if (usuarioActual != null)
                    venta.USUARIOS_id_usuario = usuarioActual.id_usuario;

                if (polleria != null)
                    venta.POLLERIA_id_polleria = polleria.id_polleria;

                // 2. Obtener el nombre del canal seleccionado (MESA, LLEVAR, DELIVERY) de forma dinámica
                if (venta.TIPO_VENTA_id_tipoVenta.HasValue)
                {
                    var canales = cdVenta.ListarTiposVenta();
                    var canal = canales?.FirstOrDefault(c => c.id_tipoVenta == venta.TIPO_VENTA_id_tipoVenta.Value);
                    venta.Tipo_venta = canal != null ? canal.nombre : "MESA";
                }
                else
                {
                    venta.Tipo_venta = "MESA";
                    venta.TIPO_VENTA_id_tipoVenta = 1;
                }

                // Asegurar que el método de pago tenga un valor por defecto si viene nulo
                if (string.IsNullOrWhiteSpace(venta.TipoPago))
                {
                    venta.TipoPago = "EFECTIVO";
                }

                // 3. Limpiar del ModelState campos calculados en servidor o enviados como JSON separado
                ModelState.Remove("Tipo_venta");
                ModelState.Remove("Detalles");
                ModelState.Remove("USUARIOS_id_usuario");
                ModelState.Remove("POLLERIA_id_polleria");
                ModelState.Remove("Total_venta");
                ModelState.Remove("MontoPagado");
                ModelState.Remove("hora_venta");
                ModelState.Remove("Fecha_venta");

                // 4. Validar ModelState limpio
                if (!ModelState.IsValid)
                {
                    var listaErrores = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => $"<b>{x.Key}:</b> {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                        .ToList();

                    TempData["Error"] = "Errores de validación:<br/>" + string.Join("<br/>", listaErrores);
                    CargarDatosParaVista(venta.CLIENTE_id_cliente, venta.TIPO_VENTA_id_tipoVenta);
                    return View(venta);
                }

                // 5. Parsear detalles JSON
                var detalles = ParsearDetallesVenta(detallesJson);

                if (detalles == null || detalles.Count == 0)
                {
                    TempData["Error"] = "Debe agregar al menos un producto a la venta.";
                    CargarDatosParaVista(venta.CLIENTE_id_cliente, venta.TIPO_VENTA_id_tipoVenta);
                    return View(venta);
                }

                if (polleria == null)
                {
                    TempData["Error"] = "No hay pollería activa configurada.";
                    CargarDatosParaVista(venta.CLIENTE_id_cliente, venta.TIPO_VENTA_id_tipoVenta);
                    return View(venta);
                }

                // 6. Validar stock preliminar con SP
                var validacionStock = cdVenta.ValidarStockParaVenta(polleria.id_polleria, detalles);

                if (!validacionStock.StockSuficiente)
                {
                    TempData["Error"] = "Stock insuficiente para completar la venta: " + validacionStock.Mensaje;
                    ViewBag.ValidacionesStock = validacionStock.ValidacionesStock;
                    CargarDatosParaVista(venta.CLIENTE_id_cliente, venta.TIPO_VENTA_id_tipoVenta);
                    return View(venta);
                }

                venta.Total_venta = detalles.Sum(d => d.subtotal);

                // 7. Registrar venta
                // El SP usa el 4to parámetro para buscar en TIPO_PAGO e insertar el pago,
                // y el 7mo parámetro para guardar el id del canal (MESA, LLEVAR, DELIVERY).
                var respuesta = cdVenta.RegistrarVentaConStockControl(
                    idUsuario: venta.USUARIOS_id_usuario,
                    idCliente: venta.CLIENTE_id_cliente,
                    idPolleria: venta.POLLERIA_id_polleria,
                    tipoVenta: venta.TipoPago,                      // Resuelve el medio de pago (EFECTIVO, QR, etc.)
                    detalles: detalles,
                    observaciones: null,
                    idTipoVenta: venta.TIPO_VENTA_id_tipoVenta      // Canal de atención (MESA, LLEVAR, etc.)
                );

                if (respuesta.Resultado)
                {
                    TempData["Exito"] = respuesta.Mensaje;
                    return RedirectToAction("Details", new { id = respuesta.IdVentaGenerada });
                }
                else
                {
                    TempData["Error"] = respuesta.Mensaje;
                    CargarDatosParaVista(venta.CLIENTE_id_cliente, venta.TIPO_VENTA_id_tipoVenta);
                    return View(venta);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPCIÓN en Venta/Create: {ex.Message}");
                TempData["Error"] = "Error: " + ex.Message;
                CargarDatosParaVista(venta.CLIENTE_id_cliente, venta.TIPO_VENTA_id_tipoVenta);
                return View(venta);
            }
        }

        // GET: Venta/Details/id
        // GET: Venta/Details/id
        public ActionResult Details(int id)
        {
            if (!TienePermiso("Ventas"))
            {
                TempData["Error"] = "No tiene permisos para ver detalles de Ventas.";
                return RedirectToAction("Index");
            }

            try
            {
                var venta = cdVenta.ObtenerDetallesVenta(id);

                if (venta == null)
                {
                    TempData["Error"] = $"No se encontró la venta con ID {id}.";
                    return RedirectToAction("Index");
                }

                if (venta.Detalles == null)
                    venta.Detalles = new List<CMDetalleVenta>();

                return View(venta);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al consultar venta: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        // GET: Venta/Anular/id
        public ActionResult Anular(int id)
        {
            if (!PuedeEditar("Ventas"))
            {
                TempData["Error"] = "No tiene permisos para anular Ventas.";
                return RedirectToAction("Index");
            }

            // Obtener venta (ajustar según tu método existente)
            // var venta = cdVenta.ObtenerDetallesVentaConSP(id);
            var venta = new CMVenta(); // Reemplazar con tu método real

            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada.";
                return RedirectToAction("Index");
            }

            if (venta.Estado == "ANULADA")
            {
                TempData["Error"] = "La venta ya está anulada.";
                return RedirectToAction("Details", new { id = id });
            }

            if (venta.Estado != "COMPLETADA")
            {
                TempData["Error"] = $"Solo se pueden anular ventas COMPLETADAS. Estado actual: {venta.Estado}";
                return RedirectToAction("Details", new { id = id });
            }

            return View(venta);
        }

        // POST: Venta/Anular/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Anular(int id, string motivo = "")
        {
            try
            {
                if (!PuedeEditar("Ventas"))
                {
                    TempData["Error"] = "No tiene permisos para anular Ventas.";
                    return RedirectToAction("Index");
                }

                var usuarioActual = ObtenerUsuarioActual();
                if (usuarioActual == null)
                {
                    TempData["Error"] = "No se pudo identificar al usuario.";
                    return RedirectToAction("Anular", new { id = id });
                }

                // Anular venta con reversión de stock
                var respuesta = cdVenta.AnularVentaConRevertirStock(id, usuarioActual.id_usuario, motivo);

                if (respuesta.Resultado)
                {
                    TempData["Exito"] = respuesta.Mensaje;
                }
                else
                {
                    TempData["Error"] = respuesta.Mensaje;
                }

                return RedirectToAction("Details", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Anular", new { id = id });
            }
        }

        #endregion

        #region Gestión de Stock

        // GET: Venta/StockDisponible
        public ActionResult StockDisponible(int? idPolleria = null, bool soloConStock = true)
        {
            if (!TienePermiso("Ventas"))
            {
                TempData["Error"] = "No tiene permisos para ver stock.";
                return RedirectToAction("Index");
            }

            var polleriaActiva = ObtenerPolleriaActiva();
            if (polleriaActiva != null && idPolleria == null)
            {
                idPolleria = polleriaActiva.id_polleria;
            }

            var stock = cdVenta.ObtenerStockDisponible(idPolleria, soloConStock);

            // Pasar lista de pollerías para el filtro
            ViewBag.ListaPollerias = ObtenerListaPollerias();
            ViewBag.IdPolleria = idPolleria;
            ViewBag.SoloConStock = soloConStock;
            ViewBag.PolleriaActiva = polleriaActiva;

            return View(stock);
        }

        // GET: Venta/AjustarStock
        public ActionResult AjustarStock(int? idProducto = null, int? idPolleria = null)
        {
            if (!TienePermiso("Administrador"))
            {
                TempData["Error"] = "Solo administradores pueden ajustar stock.";
                return RedirectToAction("StockDisponible");
            }

            var ajuste = new CMAjusteStock
            {
                FechaAjuste = DateTime.Now,
                IdUsuario = ObtenerUsuarioActual()?.id_usuario ?? 0
            };

            if (idProducto.HasValue && idPolleria.HasValue)
            {
                var stock = cdVenta.ObtenerStockDisponible(idPolleria, false)
                    .FirstOrDefault(s => s.IdProducto == idProducto && s.IdPolleria == idPolleria);

                if (stock != null)
                {
                    ajuste.IdProducto = stock.IdProducto;
                    ajuste.IdPolleria = stock.IdPolleria;
                    ajuste.NombreProducto = stock.NombreProducto;
                    ajuste.Polleria = stock.NombrePolleria;
                    ajuste.StockAnterior = stock.StockActual;
                    ajuste.NuevoStock = stock.StockActual;
                }
            }

            ViewBag.ListaPollerias = ObtenerListaPollerias();
            ViewBag.ListaProductos = ObtenerListaProductos();

            return View(ajuste);
        }

        // POST: Venta/AjustarStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjustarStock(CMAjusteStock ajuste)
        {
            if (!TienePermiso("Administrador"))
            {
                TempData["Error"] = "Solo administradores pueden ajustar stock.";
                return RedirectToAction("StockDisponible");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    ajuste.IdUsuario = ObtenerUsuarioActual()?.id_usuario ?? 0;

                    var respuesta = cdVenta.AjustarStockManual(ajuste);

                    if (respuesta.Resultado)
                    {
                        TempData["Exito"] = respuesta.Mensaje;
                        return RedirectToAction("StockDisponible", new { idPolleria = ajuste.IdPolleria });
                    }
                    else
                    {
                        TempData["Error"] = respuesta.Mensaje;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }

            ViewBag.ListaPollerias = ObtenerListaPollerias();
            ViewBag.ListaProductos = ObtenerListaProductos();

            return View(ajuste);
        }

        // GET: Venta/TransferirStock
        public ActionResult TransferirStock()
        {
            if (!TienePermiso("Administrador"))
            {
                TempData["Error"] = "Solo administradores pueden transferir stock.";
                return RedirectToAction("StockDisponible");
            }

            var transferencia = new CMTransfersVentaStock
            {
                FechaTransferencia = DateTime.Now,
                IdUsuario = ObtenerUsuarioActual()?.id_usuario ?? 0
            };

            ViewBag.ListaPollerias = ObtenerListaPollerias();
            ViewBag.ListaProductos = ObtenerListaProductos();

            return View(transferencia);
        }

        // POST: Venta/TransferirStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TransferirStock(CMTransfersVentaStock transferencia)
        {
            if (!TienePermiso("Administrador"))
            {
                TempData["Error"] = "Solo administradores pueden transferir stock.";
                return RedirectToAction("StockDisponible");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    transferencia.IdUsuario = ObtenerUsuarioActual()?.id_usuario ?? 0;

                    var respuesta = cdVenta.TransferirStockEntrePollerias(transferencia);

                    if (respuesta.Resultado)
                    {
                        TempData["Exito"] = respuesta.Mensaje;
                        return RedirectToAction("StockDisponible");
                    }
                    else
                    {
                        TempData["Error"] = respuesta.Mensaje;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }

            ViewBag.ListaPollerias = ObtenerListaPollerias();
            ViewBag.ListaProductos = ObtenerListaProductos();

            return View(transferencia);
        }

        // GET: Venta/ReporteMovimientosStock
        public ActionResult ReporteMovimientosStock(
            int? idPolleria = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            string tipoItem = null,
            string tipoMovimiento = null,
            int? idItem = null)
        {
            if (!TienePermiso("Reportes"))
            {
                TempData["Error"] = "No tiene permisos para ver reportes.";
                return RedirectToAction("Index");
            }

            // Establecer fechas por defecto (últimos 30 días)
            if (!fechaDesde.HasValue)
                fechaDesde = DateTime.Now.AddDays(-30);
            if (!fechaHasta.HasValue)
                fechaHasta = DateTime.Now;

            var movimientos = cdVenta.ObtenerMovimientosStock(
                idPolleria,
                fechaDesde,
                fechaHasta,
                tipoItem,
                tipoMovimiento,
                idItem
            );

            ViewBag.ListaPollerias = ObtenerListaPollerias();
            ViewBag.ListaTiposItem = ObtenerListaTiposItem();
            ViewBag.ListaTiposMovimiento = ObtenerListaTiposMovimiento();

            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");
            ViewBag.IdPolleria = idPolleria;
            ViewBag.TipoItem = tipoItem;
            ViewBag.TipoMovimiento = tipoMovimiento;
            ViewBag.IdItem = idItem;

            return View(movimientos);
        }
        #endregion

        #region Métodos AJAX

        [HttpPost]
        public JsonResult BuscarProductos(string term)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"BuscarProductos - Término: '{term}'");

                // Obtener pollería activa
                var polleria = ObtenerPolleriaActiva();
                if (polleria == null)
                {
                    System.Diagnostics.Debug.WriteLine("No hay pollería activa, usando ID 5 por defecto");
                    polleria = new CMPolleria { id_polleria = 5, Nombre = "El Escondite del Loro" };
                }

                System.Diagnostics.Debug.WriteLine($"Polleria ID: {polleria.id_polleria}, Nombre: {polleria.Nombre}");

                // Obtener productos con stock - ESTO VIENE DE CapaDatos
                var productos = cdVenta.ObtenerStockDisponible(polleria.id_polleria, true);

                // USAR EL MISMO TIPO - declarar como var o como CapaDatos.CMProductoStock
                // IMPORTANTE: productos ya es List<CapaDatos.CMProductoStock>

                System.Diagnostics.Debug.WriteLine($"Productos con stock encontrados: {productos?.Count ?? 0}");

                if (productos == null || !productos.Any())
                {
                    System.Diagnostics.Debug.WriteLine("NO HAY PRODUCTOS CON STOCK");
                    return Json(new List<object>(), JsonRequestBehavior.AllowGet);
                }

                // Filtrar por término de búsqueda - usando el mismo tipo (CapaDatos.CMProductoStock)
                term = term?.Trim() ?? "";

                IEnumerable<CapaDatos.CMProductoStock> productosFiltrados; // ← CORREGIDO: usar CapaDatos

                if (string.IsNullOrWhiteSpace(term))
                {
                    productosFiltrados = productos.Take(20);
                    System.Diagnostics.Debug.WriteLine("Sin filtro, devolviendo todos los productos");
                }
                else
                {
                    productosFiltrados = productos
                        .Where(p => p.NombreProducto != null &&
                                   p.NombreProducto.ToLower().Contains(term.ToLower()))
                        .Take(10);

                    System.Diagnostics.Debug.WriteLine($"Filtrado por '{term}': {productosFiltrados.Count()} productos");
                }

                // DEBUG: Mostrar productos filtrados
                foreach (var p in productosFiltrados)
                {
                    System.Diagnostics.Debug.WriteLine($"- Producto filtrado: {p.NombreProducto}, Stock: {p.StockActual}");
                }

                // Formatear resultado para Select2
                var resultados = productosFiltrados.Select(p => new
                {
                    id = p.IdProducto,
                    text = $"{p.NombreProducto} - Stock: {p.StockActual:N2} {p.Abreviatura}",
                    tipo = "PRODUCTO",
                    nombre = p.NombreProducto,
                    descripcion = p.Descripcion ?? "",
                    medida = p.Medida ?? "Unidad",
                    abreviatura = p.Abreviatura ?? "Un.",
                    precio = p.PrecioVenta,
                    stock = p.StockActual,
                    estadoStock = p.EstadoStock ?? "NORMAL",
                    claseCSS = p.ClaseCSS ?? "",
                    tieneStock = p.StockActual > 0
                }).ToList();

                System.Diagnostics.Debug.WriteLine($"Resultados a enviar: {resultados.Count}");

                return Json(resultados, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR BuscarProductos: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ObtenerProductoPorId(int id)
        {
            try
            {
                var polleria = ObtenerPolleriaActiva();
                if (polleria == null)
                {
                    return Json(new { success = false, message = "No hay pollería activa" }, JsonRequestBehavior.AllowGet);
                }

                var producto = cdVenta.ObtenerStockDisponible(polleria.id_polleria, false)
                    .FirstOrDefault(p => p.IdProducto == id);

                if (producto != null)
                {
                    return Json(new
                    {
                        success = true,
                        producto = new
                        {
                            id = producto.IdProducto,
                            nombre = producto.NombreProducto,
                            descripcion = producto.Descripcion,
                            medida = producto.Medida,
                            abreviatura = producto.Abreviatura,
                            precio = producto.PrecioVenta,
                            stock = producto.StockActual,
                            estadoStock = producto.EstadoStock,
                            tieneStock = producto.StockActual > 0
                        }
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = false, message = "Producto no encontrado" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR ObtenerProductoPorId: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ValidarStockVenta(string detallesJson)
        {
            try
            {
                var polleria = ObtenerPolleriaActiva();
                if (polleria == null)
                {
                    return Json(new { success = false, message = "No hay pollería activa" });
                }

                var detalles = ParsearDetallesVenta(detallesJson);
                if (detalles == null || detalles.Count == 0)
                {
                    return Json(new { success = false, message = "No hay detalles para validar" });
                }

                var respuesta = cdVenta.ValidarStockParaVenta(polleria.id_polleria, detalles);

                return Json(new
                {
                    success = respuesta.Resultado,
                    stockSuficiente = respuesta.StockSuficiente,
                    mensaje = respuesta.Mensaje,
                    validaciones = respuesta.ValidacionesStock?.Select(v => new
                    {
                        idProducto = v.IdProducto,
                        nombre = v.NombreProducto,
                        cantidadSolicitada = v.CantidadSolicitada,
                        stockActual = v.StockActual,
                        suficiente = v.Suficiente,
                        mensaje = v.Mensaje
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR ValidarStockVenta: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ObtenerStockProducto(int idProducto, int? idPolleria = null)
        {
            try
            {
                if (!idPolleria.HasValue)
                {
                    var polleria = ObtenerPolleriaActiva();
                    idPolleria = polleria?.id_polleria;
                }

                var stock = cdVenta.ObtenerStockDisponible(idPolleria, false)
                    .FirstOrDefault(p => p.IdProducto == idProducto && p.IdPolleria == idPolleria);

                if (stock != null)
                {
                    return Json(new
                    {
                        success = true,
                        stock = new
                        {
                            idProducto = stock.IdProducto,
                            nombreProducto = stock.NombreProducto,
                            stockActual = stock.StockActual,
                            stockMinimo = stock.StockMinimo,
                            estadoStock = stock.EstadoStock
                        }
                    });
                }

                return Json(new { success = false, message = "Producto no encontrado en esta pollería" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        #endregion

        #region Métodos Auxiliares

        private void CargarDatosParaVista(int? idClienteSeleccionado = null, int? idTipoVentaSeleccionado = null)
        {
            try
            {
                var clientes = cdClientes.Listar();
                ViewBag.ListaClientes = new SelectList(clientes, "id_cliente", "NombreCompleto", idClienteSeleccionado);

                ViewBag.ListaTiposPago = ObtenerTiposPagoDesdeBD();

                var tiposVenta = cdVenta.ListarTiposVenta();
                ViewBag.ListaCanalesVenta = new SelectList(tiposVenta, "id_tipoVenta", "nombre", idTipoVentaSeleccionado ?? 1);

                var usuarioActual = ObtenerUsuarioActual();
                ViewBag.UsuarioActual = usuarioActual;

                var polleria = ObtenerPolleriaActiva();
                ViewBag.Polleria = polleria;

                if (polleria != null)
                {
                    var productosConStock = cdVenta.ObtenerStockDisponible(polleria.id_polleria, true);
                    ViewBag.ProductosConStock = productosConStock;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CargarDatosParaVista: {ex.Message}");

                throw new Exception("Error al cargar datos para la venta: " + ex.Message);
            }
        }
        private List<SelectListItem> ObtenerTiposPagoDesdeBD()
        {
            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();
                    string query = @"SELECT id_tipoPago as Value, nombre as Text 
                            FROM TIPO_PAGO 
                            WHERE estado = 1 
                            ORDER BY nombre";

                    var tipos = conexion.Query<dynamic>(query).ToList();

                    if (tipos != null && tipos.Any())
                    {
                        var lista = new List<SelectListItem>();
                        bool primero = true;
                        foreach (var tipo in tipos)
                        {
                            lista.Add(new SelectListItem
                            {
                                Value = tipo.Value.ToString(),
                                Text = tipo.Text,
                                Selected = primero
                            });
                            primero = false;
                        }
                        return lista;
                    }

                    throw new Exception("No hay tipos de pago configurados en la base de datos. Por favor, contacte al administrador.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo tipos de pago: {ex.Message}");
                throw; 
            }
        }

        private List<CMDetalleVenta> ParsearDetallesVenta(string detallesJson)
        {
            if (string.IsNullOrEmpty(detallesJson) || detallesJson == "[]")
            {
                Debug.WriteLine("detallesJson está vacío o nulo");
                return new List<CMDetalleVenta>();
            }

            try
            {
                Debug.WriteLine($"Parseando JSON: {detallesJson.Substring(0, Math.Min(detallesJson.Length, 200))}...");

                var detalles = new List<CMDetalleVenta>();
                var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(detallesJson);

                Debug.WriteLine($"JSON parseado: {jsonArray?.Count ?? 0} elementos");

                foreach (var item in jsonArray)
                {
                    try
                    {
                        int idProducto = Convert.ToInt32(item.IdProducto ?? item.idProducto ?? item.productoId);
                        decimal cantidad = Convert.ToDecimal(item.cantidad);
                        decimal precioUnitario = Convert.ToDecimal(item.precioUnitario ?? item.precio);
                        decimal subtotal = Convert.ToDecimal(item.subtotal ?? (cantidad * precioUnitario));

                        var detalle = new CMDetalleVenta
                        {
                            PRODUCTO_id_producto = idProducto,
                            cantidad = cantidad,
                            precio_unitario = precioUnitario,
                            subtotal = subtotal,
                            NombreProducto = item.nombre?.ToString() ?? "",
                            DescripcionProducto = item.descripcion?.ToString() ?? "",
                            Medida = item.medida?.ToString() ?? "",
                            AbreviaturaMedida = item.abreviatura?.ToString() ?? "",
                            StockDisponible = item.stock != null ? Convert.ToDecimal(item.stock) : 0
                        };

                        Debug.WriteLine($"Detalle parseado: Producto ID={detalle.PRODUCTO_id_producto}, Cant={detalle.cantidad}, Precio={detalle.precio_unitario}");

                        detalles.Add(detalle);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error parseando item: {ex.Message}");
                        Debug.WriteLine($"Item problemático: {item}");
                    }
                }

                return detalles;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al parsear JSON: {ex.Message}");
                Debug.WriteLine($"JSON que falló: {detallesJson}");
                return new List<CMDetalleVenta>();
            }
        }

        private SelectList ObtenerListaPollerias()
        {
            try
            {
                // Implementar según tu capa de datos
                var pollerias = new List<CMPolleria>
                {
                    new CMPolleria { id_polleria = 1, Nombre = "Polleria 1" },
                    new CMPolleria { id_polleria = 2, Nombre = "Polleria 2" }
                };
                return new SelectList(pollerias, "id_polleria", "Nombre");
            }
            catch
            {
                return new SelectList(new List<CMPolleria>(), "id_polleria", "Nombre");
            }
        }

        private SelectList ObtenerListaProductos()
        {
            try
            {
                var polleria = ObtenerPolleriaActiva();
                var productos = cdVenta.ObtenerStockDisponible(polleria?.id_polleria, false)
                    .Select(p => new { id_producto = p.IdProducto, Nombre = p.NombreProducto })
                    .Distinct()
                    .ToList();

                return new SelectList(productos, "id_producto", "Nombre");
            }
            catch
            {
                return new SelectList(new List<object>(), "id_producto", "Nombre");
            }
        }

        private SelectList ObtenerListaTiposItem()
        {
            var tipos = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos" },
                new SelectListItem { Value = "PRODUCTO", Text = "Producto" },
                new SelectListItem { Value = "MATERIA_PRIMA", Text = "Materia Prima" }
            };
            return new SelectList(tipos, "Value", "Text");
        }

        private SelectList ObtenerListaTiposMovimiento()
        {
            var tipos = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Todos" },
                new SelectListItem { Value = "VENTA", Text = "Venta" },
                new SelectListItem { Value = "COMPRA", Text = "Compra" },
                new SelectListItem { Value = "TRANSFERENCIA", Text = "Transferencia" },
                new SelectListItem { Value = "AJUSTE", Text = "Ajuste" },
                new SelectListItem { Value = "ANULACION", Text = "Anulación" }
            };
            return new SelectList(tipos, "Value", "Text");
        }

        private CMPolleria ObtenerPolleriaActiva()
        {
            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();
                    string query = "SELECT TOP 1 id_polleria, nombre, direccion, telefono, estado FROM POLLERIA WHERE estado = 1 ORDER BY id_polleria";
                    var polleria = conexion.QueryFirstOrDefault<CMPolleria>(query);

                    if (polleria != null)
                        return polleria;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo pollería activa: {ex.Message}");
            }

            // Fallback: retornar pollería por defecto (ID 5)
            return new CMPolleria
            {
                id_polleria = 5,
                Nombre = "El Escondite del Loro",
                Direccion = "B/ La Cabaña",
                Telefono = "4578512",
                Estado = true
            };
        }
        private Usuario ObtenerUsuarioActual()
        {
            try
            {
                // 1. Verificar en Session
                if (Session["Usuario"] != null)
                {
                    var usuario = Session["Usuario"] as Usuario;
                    if (usuario != null)
                    {
                        return usuario;
                    }
                }

                // 2. Verificar por ID en Session
                if (Session["UsuarioId"] != null)
                {
                    int userId = Convert.ToInt32(Session["UsuarioId"]);

                    using (var conexion = new SqlConnection(Conexion.CN))
                    {
                        conexion.Open();
                        string query = @"
                    SELECT 
                        id_usuario, 
                        nombre, 
                        apellido, 
                        correo,   -- Cambiado de email a correo
                        telefono,
                        estado,
                        ROL_id_rol   -- Cambiado de rol_id a ROL_id_rol
                    FROM USUARIOS   -- Cambiado de USUARIO a USUARIOS
                    WHERE id_usuario = @Id AND estado = 1";

                        var usuario = conexion.QueryFirstOrDefault<Usuario>(query, new { Id = userId });

                        if (usuario != null)
                        {
                            Session["Usuario"] = usuario;
                            return usuario;
                        }
                    }
                }

                // 3. Fallback: Usuario por defecto (solo para desarrollo)
                System.Diagnostics.Debug.WriteLine("WARNING: Usando usuario por defecto (desarrollo)");
                var usuarioDefault = new Usuario
                {
                    id_usuario = 1,
                    nombre = "Admin",
                    apellido = "Sistema",
                    correo = "admin@system.com",
                    telefono = "00000000",
                    estado = true,
                    ROL_id_rol = 1
                };

                Session["Usuario"] = usuarioDefault;
                return usuarioDefault;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo usuario: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}