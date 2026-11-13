using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class CompraController : BaseController
    {
        private CDCompra cdCompra = new CDCompra();
        private CDProveedor cdProveedor = new CDProveedor();
        private CDUsuario cdUsuario = new CDUsuario();
        private CDTipoCompra cdTipoCompra = new CDTipoCompra();
        private CDProducto cdProducto = new CDProducto();
        private CDInsumo cdInsumo = new CDInsumo();
        private CDPolleria cdPolleria = new CDPolleria();
        private CDStock cdStock = new CDStock();

        public ActionResult Index(string buscar = "", string estado = "", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            if (!TienePermiso("Compras"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Compras.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = TienePermiso("Compras");
            ViewBag.PuedeEditar = PuedeEditar("Compras");
            ViewBag.PuedeEliminar = PuedeEliminar("Compras");

            DateTime? fechaInicio = null;
            if (fechaDesde.HasValue)
            {
                fechaInicio = fechaDesde.Value.Date; // 00:00:00.000
            }

            DateTime? fechaFin = null;
            if (fechaHasta.HasValue)
            {
                // Lleva la fecha hasta el último instante del día para que incluya las compras con hora
                fechaFin = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
            }

            // Al pasar objetos DateTime a Dapper/ADO.NET, se envían como tipo binario datetime 
            // y SQL Server NO tiene que adivinar si el formato es dmy o ymd.
            var lista = cdCompra.ListarCompras(estado, fechaInicio, fechaFin);

            if (!string.IsNullOrEmpty(buscar))
            {
                string textoBusqueda = buscar.Trim().ToLower();
                lista = lista.Where(c =>
                    (c.Referencia != null && c.Referencia.ToLower().Contains(textoBusqueda)) ||
                    (c.NombreProveedor != null && c.NombreProveedor.ToLower().Contains(textoBusqueda)) ||
                    (c.DocumentoProveedor != null && c.DocumentoProveedor.ToLower().Contains(textoBusqueda)) ||
                    (c.Estado != null && c.Estado.ToLower().Contains(textoBusqueda))
                ).ToList();

                ViewBag.Buscar = buscar;
            }

            ViewBag.TotalCompras = lista.Count;
            ViewBag.Completadas = lista.Count(c => c.Estado == "COMPLETADA");
            ViewBag.Pendientes = lista.Count(c => c.Estado == "PENDIENTE");
            ViewBag.Anuladas = lista.Count(c => c.Estado == "ANULADA");
            ViewBag.MontoTotal = lista.Where(c => c.Estado == "COMPLETADA").Sum(c => c.MontoTotal);

            ViewBag.Estado = estado;
            ViewBag.FechaDesde = fechaDesde.HasValue ? fechaDesde.Value.ToString("yyyy-MM-dd") : "";
            ViewBag.FechaHasta = fechaHasta.HasValue ? fechaHasta.Value.ToString("yyyy-MM-dd") : "";

            return View(lista);
        }

        public ActionResult Create()
        {
            if (!TienePermiso("Compras"))
            {
                TempData["Error"] = "No tiene permisos para crear Compras.";
                return RedirectToAction("Index");
            }

            var polleriaActiva = ObtenerPolleriaActiva();
            if (polleriaActiva == null)
            {
                TempData["Error"] = "No hay pollerías activas configuradas. Configure una pollería activa primero.";
                return RedirectToAction("Index", "Polleria");
            }

            ViewBag.IdPolleria = polleriaActiva.id_polleria;
            ViewBag.PolleriaActual = polleriaActiva;

            CargarDatosParaVista();

            var compra = new CMCompra
            {
                Estado = "COMPLETADA",
                FechaCompra = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            return View(compra);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMCompra compra, string detallesJson)
        {
            try
            {
                if (!TienePermiso("Compras"))
                {
                    TempData["Error"] = "No tiene permisos para crear Compras.";
                    return RedirectToAction("Index");
                }

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }

                var detalles = ParsearDetalles(detallesJson);
                if (detalles == null || detalles.Count == 0)
                {
                    TempData["Error"] = "Debe agregar al menos un item a la compra.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }

                compra.MontoTotal = detalles.Sum(d => d.subtotal);

                var usuarioActual = ObtenerUsuarioActual();
                if (usuarioActual != null)
                {
                    compra.USUARIOS_id_usuario = usuarioActual.id_usuario;
                }
                else
                {
                    TempData["Error"] = "No se pudo identificar al usuario.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra);
                    return View(compra);
                }

                var proveedor = cdProveedor.ObtenerProveedorPorId(compra.PROVEEDOR_id_proveedor);
                if (proveedor == null || !proveedor.Estado)
                {
                    TempData["Error"] = "El proveedor seleccionado no existe o está inactivo.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra);
                    return View(compra);
                }

                var polleriaActiva = ObtenerPolleriaActiva();
                if (polleriaActiva == null)
                {
                    TempData["Error"] = "No hay pollerías activas configuradas.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra);
                    return View(compra);
                }

                int idPolleria = polleriaActiva.id_polleria;

                string errorValidacion = ValidarItemsCompra(detalles);
                if (!string.IsNullOrEmpty(errorValidacion))
                {
                    TempData["Error"] = errorValidacion;
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra);
                    return View(compra);
                }

                var respuesta = cdCompra.RegistrarCompraConSP(compra, detalles, idPolleria);

                if (respuesta.Resultado)
                {
                    TempData["Exito"] = respuesta.Mensaje;
                    TempData["CompraId"] = respuesta.IdGenerado;
                    return RedirectToAction("Details", new { id = respuesta.IdGenerado });
                }
                else
                {
                    TempData["Error"] = respuesta.Mensaje;
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra);
                    return View(compra);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPCIÓN: {ex.Message}\nStackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Error: {ex.Message}";
                CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra);
                return View(compra);
            }
        }

        public ActionResult Details(int id)
        {
            if (!TienePermiso("Compras"))
            {
                TempData["Error"] = "No tiene permisos para ver detalles de Compras.";
                return RedirectToAction("Index");
            }

            var compra = cdCompra.ObtenerCompraPorId(id);

            if (compra == null)
            {
                TempData["Error"] = "Compra no encontrada.";
                return RedirectToAction("Index");
            }

            var polleriaActiva = ObtenerPolleriaActiva();
            if (polleriaActiva != null)
            {
                foreach (var detalle in compra.Detalles)
                {
                    if (detalle.TipoItem == "PRODUCTO" && detalle.PRODUCTO_id_producto.HasValue)
                    {
                        detalle.StockActual = cdStock.ObtenerStockDisponibleProducto(
                            detalle.PRODUCTO_id_producto.Value,
                            polleriaActiva.id_polleria
                        );
                    }
                    else if (detalle.TipoItem == "INSUMO" && detalle.INSUMO_id_insumo.HasValue)
                    {
                        detalle.StockActual = cdStock.ObtenerStockDisponibleInsumo(
                            detalle.INSUMO_id_insumo.Value,
                            polleriaActiva.id_polleria
                        );
                    }
                }
            }

            ViewBag.MostrarStock = true;
            ViewBag.PuedeAnular = (compra.Estado == "COMPLETADA" || compra.Estado == "PENDIENTE") && PuedeEliminar("Compras");
            ViewBag.PuedeCompletar = (compra.Estado == "PENDIENTE") && PuedeEditar("Compras");

            return View(compra);
        }

        // GET: Compra/Edit/5
        public ActionResult Edit(int id)
        {
            if (!PuedeEditar("Compras"))
            {
                TempData["Error"] = "No tiene permisos para editar Compras.";
                return RedirectToAction("Index");
            }

            // 1. Validar que el ID que llega sea mayor a 0
            if (id <= 0)
            {
                TempData["Error"] = "El identificador de la compra recibido es inválido (ID: " + id + ").";
                return RedirectToAction("Index");
            }

            CMCompra compra = null;
            try
            {
                compra = cdCompra.ObtenerCompraPorId(id);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error en base de datos: " + ex.Message;
                return RedirectToAction("Index");
            }

            if (compra == null)
            {
                TempData["Error"] = "La compra con ID #" + id + " no existe o no devolvió datos.";
                return RedirectToAction("Index");
            }

            if (compra.Estado != "PENDIENTE")
            {
                TempData["Error"] = $"No se puede editar una compra con estado '{compra.Estado}'. Solo se pueden editar compras PENDIENTES.";
                return RedirectToAction("Details", new { id = id });
            }

            var polleriaActiva = ObtenerPolleriaActiva();
            ViewBag.IdPolleria = polleriaActiva?.id_polleria ?? 0;

            if (compra.Detalles != null && compra.Detalles.Any())
            {
                var detallesJson = compra.Detalles.Select(d => new
                {
                    IdItemSeleccionado = d.TipoItem == "PRODUCTO" ?
                        (d.PRODUCTO_id_producto ?? 0) :
                        (d.INSUMO_id_insumo ?? 0),
                    TipoItem = d.TipoItem,
                    nombre = d.NombreItem,
                    descripcion = d.DescripcionItem,
                    medida = d.MedidaItem,
                    abreviatura = d.AbreviaturaMedida,
                    cantidad = d.cantidad,
                    precioUnitario = d.precioUnitario,
                    subtotal = d.subtotal,
                    stockActual = d.StockActual
                }).ToList();

                ViewBag.DetallesJson = Newtonsoft.Json.JsonConvert.SerializeObject(detallesJson);
            }
            else
            {
                ViewBag.DetallesJson = "[]";
            }

            CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);

            return View(compra);
        }

        // POST: Compra/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CMCompra compra, string detallesJson)
        {
            try
            {
                if (!PuedeEditar("Compras"))
                {
                    TempData["Error"] = "No tiene permisos para editar Compras.";
                    return RedirectToAction("Index");
                }

                var compraExistente = cdCompra.ObtenerCompraPorId(id);
                if (compraExistente == null)
                {
                    TempData["Error"] = "Compra no encontrada.";
                    return RedirectToAction("Index");
                }

                if (compraExistente.Estado != "PENDIENTE")
                {
                    TempData["Error"] = $"No se puede editar una compra con estado '{compraExistente.Estado}'. Solo se pueden editar compras PENDIENTES.";
                    return RedirectToAction("Details", new { id = id });
                }

                // Si el modelo falla, conservamos los detalles en el ViewBag para no perder la tabla
                ViewBag.DetallesJson = string.IsNullOrWhiteSpace(detallesJson) ? "[]" : detallesJson;

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }

                var detalles = ParsearDetalles(detallesJson);
                if (detalles == null || detalles.Count == 0)
                {
                    TempData["Error"] = "Debe agregar al menos un item a la compra.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }

                compra.id_compra = id;
                compra.USUARIOS_id_usuario = compraExistente.USUARIOS_id_usuario;
                compra.MontoTotal = detalles.Sum(d => d.subtotal);

                var proveedor = cdProveedor.ObtenerProveedorPorId(compra.PROVEEDOR_id_proveedor);
                if (proveedor == null || !proveedor.Estado)
                {
                    TempData["Error"] = "El proveedor seleccionado no existe o está inactivo.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }

                var polleriaActiva = ObtenerPolleriaActiva();
                if (polleriaActiva == null)
                {
                    TempData["Error"] = "No hay pollerías activas configuradas.";
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }

                int idPolleria = polleriaActiva.id_polleria;

                string errorValidacion = ValidarItemsCompra(detalles);
                if (!string.IsNullOrEmpty(errorValidacion))
                {
                    TempData["Error"] = errorValidacion;
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }

                using (var connection = new SqlConnection(Conexion.CN))
                {
                    connection.Open();
                    var cmd = new SqlCommand("UPDATE COMPRA SET Estado = 'MODIFICADA' WHERE id_compra = @IdCompra", connection);
                    cmd.Parameters.AddWithValue("@IdCompra", id);
                    cmd.ExecuteNonQuery();
                }

                compra.id_compra = 0;
                var respuesta = cdCompra.RegistrarCompraConSP(compra, detalles, idPolleria);

                if (respuesta.Resultado)
                {
                    TempData["Exito"] = $"Compra actualizada exitosamente. Nueva ID: #{respuesta.IdGenerado}";
                    return RedirectToAction("Details", new { id = respuesta.IdGenerado > 0 ? respuesta.IdGenerado : id });
                }
                else
                {
                    TempData["Error"] = respuesta.Mensaje;
                    CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                    return View(compra);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar compra: {ex.Message}";
                CargarDatosParaVista(compra.PROVEEDOR_id_proveedor, compra.TIPO_COMPRA_id_tipoCompra, compra.IdTipoPago);
                return View(compra);
            }
        }

        public ActionResult Anular(int id)
        {
            if (!PuedeEliminar("Compras"))
            {
                TempData["Error"] = "No tiene permisos para anular Compras.";
                return RedirectToAction("Details", new { id = id });
            }

            var compra = cdCompra.ObtenerCompraPorId(id);
            if (compra == null)
            {
                TempData["Error"] = "Compra no encontrada.";
                return RedirectToAction("Index");
            }

            if (compra.Estado == "ANULADA")
            {
                TempData["Error"] = "La compra ya está anulada.";
                return RedirectToAction("Details", new { id = id });
            }

            if (compra.Estado == "COMPLETADA")
            {
                ViewBag.MostrarAdvertenciaStock = true;
                ViewBag.Advertencia = "⚠️ ALERTA: Esta compra está COMPLETADA. Al anularla, se revertirá el stock de todos los productos y/o insumos.";
            }
            return View(compra);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Anular(int id, string motivo)
        {
            try
            {
                if (!PuedeEliminar("Compras"))
                {
                    TempData["Error"] = "No tiene permisos para anular Compras.";
                    return RedirectToAction("Details", new { id = id });
                }

                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe proporcionar un motivo para la anulación.";
                    return RedirectToAction("Anular", new { id = id });
                }

                var usuarioActual = ObtenerUsuarioActual();
                if (usuarioActual == null)
                {
                    TempData["Error"] = "No se pudo identificar la sesión del usuario.";
                    return RedirectToAction("Details", new { id = id });
                }

                // Parámetro de idUsuario agregado
                var respuesta = cdCompra.AnularCompraConSP(id, motivo, usuarioActual.id_usuario);

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
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id = id });
            }
        }

        public ActionResult Completar(int id)
        {
            if (!PuedeEditar("Compras"))
            {
                TempData["Error"] = "No tiene permisos para completar Compras.";
                return RedirectToAction("Details", new { id = id });
            }

            var compra = cdCompra.ObtenerCompraPorId(id);
            if (compra == null)
            {
                TempData["Error"] = "Compra no encontrada.";
                return RedirectToAction("Index");
            }

            if (compra.Estado == "COMPLETADA")
            {
                TempData["Error"] = "La compra ya está completada.";
                return RedirectToAction("Details", new { id = id });
            }

            ViewBag.Compra = compra;
            ViewBag.MostrarInfoStock = true;
            ViewBag.InfoStock = "⚠️ ALERTA: Al completar esta compra, se actualizará el stock de todos los productos y/o insumos.";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarCompletar(int id)
        {
            try
            {
                if (!PuedeEditar("Compras"))
                {
                    TempData["Error"] = "No tiene permisos para completar Compras.";
                    return RedirectToAction("Details", new { id = id });
                }

                var usuarioActual = ObtenerUsuarioActual();
                var polleriaActiva = ObtenerPolleriaActiva();

                if (usuarioActual == null || polleriaActiva == null)
                {
                    TempData["Error"] = "Usuario o pollería activa no identificados.";
                    return RedirectToAction("Details", new { id = id });
                }

                // Parámetros de usuario y pollería incluidos
                var respuesta = cdCompra.CambiarEstadoCompra(id, "COMPLETADA", usuarioActual.id_usuario, polleriaActiva.id_polleria);

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
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id = id });
            }
        }

        public ActionResult ImprimirComprobante(int id)
        {
            if (!TienePermiso("Compras"))
            {
                TempData["Error"] = "No tiene permisos para imprimir comprobantes.";
                return RedirectToAction("Details", new { id = id });
            }

            var compra = cdCompra.ObtenerCompraPorId(id);

            if (compra == null)
            {
                TempData["Error"] = "Compra no encontrada.";
                return RedirectToAction("Index");
            }

            var polleria = ObtenerPolleriaActiva() ?? new CMPolleria
            {
                Nombre = "El Escondite del Loro",
                Direccion = "B/ La Cabaña",
                Telefono = "4578512"
            };

            ViewBag.Polleria = polleria;
            ViewBag.FechaImpresion = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            return View(compra);
        }

        [HttpPost]
        public JsonResult BuscarProductos(string term)
        {
            try
            {
                var lista = cdProducto.ListarProductosParaCompra(term) ?? new List<CMProducto>();
                var productos = lista
                    .Select(p => new
                    {
                        id = p.id_producto,
                        text = $"{p.Nombre} - {p.Medida ?? "Sin medida"}",
                        tipo = "PRODUCTO",
                        nombre = p.Nombre,
                        descripcion = p.Descripcion ?? "",
                        medida = p.Medida ?? "Sin medida",
                        abreviatura = p.AbreviaturaMedida ?? "",
                        precio = p.PrecioVenta
                    })
                    .Take(20)
                    .ToList();

                return Json(productos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en BuscarProductos: {ex.Message}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult BuscarInsumos(string term)
        {
            try
            {
                var insumos = cdInsumo.ListarInsumos()
                    .Where(i => (i.nombre.ToLower().Contains(term.ToLower()) ||
                               i.descripcion.ToLower().Contains(term.ToLower())) && i.estado)
                    .Select(i => new
                    {
                        id = i.id_insumo,
                        text = $"{i.nombre} - {i.NombreMedida ?? "Sin medida"}",
                        tipo = "INSUMO",
                        nombre = i.nombre,
                        descripcion = i.descripcion,
                        medida = i.NombreMedida ?? "Sin medida",
                        abreviatura = i.Abreviatura ?? ""
                    })
                    .Take(20)
                    .ToList();

                return Json(insumos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en BuscarInsumos: {ex.Message}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ObtenerStockDisponible(int idItem, string tipoItem)
        {
            try
            {
                var polleriaActiva = ObtenerPolleriaActiva();
                if (polleriaActiva == null)
                {
                    return Json(new { success = false, message = "No hay pollería activa" }, JsonRequestBehavior.AllowGet);
                }

                decimal stockActual = 0;
                if (tipoItem == "PRODUCTO")
                {
                    stockActual = cdStock.ObtenerStockDisponibleProducto(idItem, polleriaActiva.id_polleria);
                }
                else if (tipoItem == "INSUMO")
                {
                    stockActual = cdStock.ObtenerStockDisponibleInsumo(idItem, polleriaActiva.id_polleria);
                }

                return Json(new { success = true, stockActual }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en ObtenerStockDisponible: {ex.Message}");
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #region MÉTODOS AUXILIARES

        private void CargarDatosParaVista(int? idProveedorSeleccionado = null, int? idTipoCompraSeleccionado = null, int? idTipoPagoSeleccionado = null)
        {
            try
            {
                var proveedores = cdProveedor.ListarProveedoresActivos();
                ViewBag.ListaProveedores = new SelectList(proveedores, "id_proveedor", "NombreCompleto", idProveedorSeleccionado);

                var tiposCompra = cdTipoCompra.ListarTipoCompra();
                ViewBag.ListaTiposCompra = new SelectList(tiposCompra, "id_tipoCompra", "Nombre", idTipoCompraSeleccionado);

                // Se obtienen los métodos de pago directamente desde CDCompra
                var tiposPago = cdCompra.ListarTiposPago();
                ViewBag.ListaTiposPago = new SelectList(tiposPago, "id_tipoPago", "nombre", idTipoPagoSeleccionado ?? 1);

                ViewBag.ListaEstados = new SelectList(new List<SelectListItem>
        {
            new SelectListItem { Value = "PENDIENTE", Text = "PENDIENTE" },
            new SelectListItem { Value = "COMPLETADA", Text = "COMPLETADA", Selected = true }
        }, "Value", "Text");

                var usuarioActual = ObtenerUsuarioActual();
                ViewBag.UsuarioActual = usuarioActual;

                var polleriaActiva = ObtenerPolleriaActiva();
                if (polleriaActiva != null)
                {
                    ViewBag.IdPolleria = polleriaActiva.id_polleria;
                    ViewBag.PolleriaActual = polleriaActiva;
                }
                else
                {
                    ViewBag.IdPolleria = 0;
                    ViewBag.PolleriaActual = new CMPolleria { Nombre = "No hay pollería activa" };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en CargarDatosParaVista: {ex.Message}");
            }
        }

        private CMPolleria ObtenerPolleriaActiva()
        {
            try
            {
                var pollerias = cdPolleria.Listar();
                return pollerias?.FirstOrDefault(p => p.Estado);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error obteniendo pollería activa: {ex.Message}");
                return null;
            }
        }

        private List<CMDetalleCompra> ParsearDetalles(string detallesJson)
        {
            if (string.IsNullOrEmpty(detallesJson) || detallesJson == "[]")
            {
                return new List<CMDetalleCompra>();
            }

            try
            {
                var detalles = new List<CMDetalleCompra>();
                var jsonArray = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(detallesJson);

                foreach (var item in jsonArray)
                {
                    try
                    {
                        int idItem = Convert.ToInt32(item.IdItemSeleccionado ?? 0);
                        string tipoItem = item.TipoItem?.ToString() ?? "PRODUCTO";

                        var detalle = new CMDetalleCompra
                        {
                            IdItemSeleccionado = idItem,
                            TipoItem = tipoItem,
                            cantidad = Convert.ToDecimal(item.cantidad),
                            precioUnitario = Convert.ToDecimal(item.precioUnitario),
                            subtotal = Convert.ToDecimal(item.subtotal)
                        };

                        if (detalle.TipoItem == "PRODUCTO")
                        {
                            detalle.PRODUCTO_id_producto = idItem;
                        }
                        else if (detalle.TipoItem == "INSUMO")
                        {
                            detalle.INSUMO_id_insumo = idItem;
                        }

                        detalles.Add(detalle);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error parseando item: {ex.Message}");
                    }
                }

                return detalles;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al parsear JSON: {ex.Message}");
                return new List<CMDetalleCompra>();
            }
        }

        private string ValidarItemsCompra(List<CMDetalleCompra> detalles)
        {
            foreach (var detalle in detalles)
            {
                if (detalle.TipoItem == "PRODUCTO")
                {
                    var producto = cdProducto.ObtenerProductoPorId(detalle.IdItemSeleccionado);
                    if (producto == null)
                        return $"Producto ID {detalle.IdItemSeleccionado} no existe";
                    if (!producto.Estado)
                        return $"Producto '{producto.Nombre}' está inactivo";
                }
                else if (detalle.TipoItem == "INSUMO")
                {
                    var insumo = cdInsumo.ObtenerInsumoPorId(detalle.IdItemSeleccionado);
                    if (insumo == null)
                        return $"Insumo ID {detalle.IdItemSeleccionado} no existe";
                    if (!insumo.estado)
                        return $"Insumo '{insumo.nombre}' está inactivo";
                }
            }
            return null;
        }

        private Usuario ObtenerUsuarioActual()
        {
            if (Session["Usuario"] != null)
            {
                return (Usuario)Session["Usuario"];
            }
            return null;
        }

        #endregion
    }
}