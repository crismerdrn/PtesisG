using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PTesis.Controllers
{
    public class InsumoProductoController : BaseController
    {
        private CDInsumoProducto cdInsumoProducto = new CDInsumoProducto();
        private CDProducto cdProducto = new CDProducto();
        private CDInsumo cdInsumo = new CDInsumo();
        private CDPolleria cdPolleria = new CDPolleria();

        public ActionResult Index(int? idProducto, string buscar = "", string estado = "", int page = 1)
        {
            if (!TienePermiso("InsumosProductos"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Insumos-Productos.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = TienePermiso("InsumosProductos");
            ViewBag.PuedeEditar = PuedeEditar("InsumosProductos");
            ViewBag.PuedeEliminar = PuedeEliminar("InsumosProductos");

            List<CMInsumoProducto> lista;

            if (idProducto.HasValue && idProducto > 0)
            {
                var producto = cdProducto.ObtenerProductoPorId(idProducto.Value);
                if (producto == null)
                {
                    TempData["Error"] = $"Producto con ID {idProducto.Value} no encontrado.";
                    return RedirectToAction("Index");
                }

                lista = cdInsumoProducto.ObtenerPorProducto(idProducto.Value);
                ViewBag.IdProductoSeleccionado = idProducto.Value;
                ViewBag.NombreProducto = producto.Nombre;
                ViewBag.ProductoActual = producto;
            }
            else
            {
                lista = cdInsumoProducto.ObtenerTodos();
            }

            // Filtro por texto
            if (!string.IsNullOrEmpty(buscar))
            {
                string textoBusqueda = buscar.Trim().ToLower();
                lista = lista.Where(ip =>
                    (ip.NombreProducto?.ToLower().Contains(textoBusqueda) == true) ||
                    (ip.NombreInsumo?.ToLower().Contains(textoBusqueda) == true) ||
                    (ip.observaciones?.ToLower().Contains(textoBusqueda) == true)
                ).ToList();

                ViewBag.Buscar = buscar;
            }

            // Filtro por estado
            if (!string.IsNullOrEmpty(estado))
            {
                bool estadoBool = estado.ToLower() == "activo";
                lista = lista.Where(ip => ip.estado == estadoBool).ToList();
                ViewBag.EstadoFiltro = estado;
            }

            // --- LÓGICA DE PAGINACIÓN ---
            int pageSize = 10; // Registros por página
            int totalRegistros = lista.Count;
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / pageSize);
            if (page < 1) page = 1;
            if (page > totalPaginas && totalPaginas > 0) page = totalPaginas;

            var listaPaginada = lista.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalRegistros = totalRegistros;
            ViewBag.PageSize = pageSize;

            CargarDependencias(idProductoSeleccionado: ViewBag.IdProductoSeleccionado);
            return View(listaPaginada);
        }

        // --- CREAR ---
        [HttpGet]
        public ActionResult Create(int? idProducto)
        {
            if (!TienePermiso("InsumosProductos"))
            {
                TempData["Error"] = "No tiene permisos para crear relaciones insumo-producto.";
                return RedirectToAction("Index");
            }

            var model = new CMInsumoProducto();

            if (idProducto.HasValue)
            {
                var producto = cdProducto.ObtenerProductoPorId(idProducto.Value);
                if (producto == null)
                {
                    TempData["Error"] = "Producto no encontrado.";
                    return RedirectToAction("Index");
                }

                model.PRODUCTO_id_producto = idProducto.Value;
                ViewBag.NombreProducto = producto.Nombre;
                ViewBag.ProductoActual = producto;
            }

            // CORREGIDO: Pasar el parámetro correctamente
            CargarDependencias(idProductoSeleccionado: model.PRODUCTO_id_producto);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMInsumoProducto insumoProducto)
        {
            try
            {
                if (!TienePermiso("InsumosProductos"))
                {
                    TempData["Error"] = "No tiene permisos para crear relaciones insumo-producto.";
                    return RedirectToAction("Index");
                }

                // Asegurar que el estado sea true (activo) para nuevas creaciones
                insumoProducto.estado = true;

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                    CargarDependencias(idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto);
                    return View(insumoProducto);
                }

                // Validar que el producto exista
                var producto = cdProducto.ObtenerProductoPorId(insumoProducto.PRODUCTO_id_producto);
                if (producto == null)
                {
                    ModelState.AddModelError("PRODUCTO_id_producto", "El producto seleccionado no existe.");
                    CargarDependencias(idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto);
                    return View(insumoProducto);
                }

                // Validar que el insumo exista
                var insumo = cdInsumo.ObtenerInsumoPorId(insumoProducto.INSUMO_id_insumo);
                if (insumo == null || !insumo.estado)
                {
                    ModelState.AddModelError("INSUMO_id_insumo", "El insumo seleccionado no existe o está inactivo.");
                    CargarDependencias(idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto);
                    return View(insumoProducto);
                }

                // MODIFICACIÓN CLAVE: Verificar si ya existe una relación (activa o inactiva)
                var relacionExistente = cdInsumoProducto.ObtenerRelacionExistente(
                    insumoProducto.PRODUCTO_id_producto,
                    insumoProducto.INSUMO_id_insumo);

                if (relacionExistente != null)
                {
                    // Si existe una relación INACTIVA, mostrar opción de reactivación
                    if (!relacionExistente.estado)
                    {
                        System.Diagnostics.Debug.WriteLine($"Encontrada relación inactiva ID: {relacionExistente.id_insumo_producto}");

                        // Guardar información para mostrar en la vista
                        TempData["RelacionInactiva"] = relacionExistente.id_insumo_producto;
                        TempData["MensajeConfirmacion"] = $"Ya existe una asignación inactiva de este insumo al producto. ¿Desea reactivarla con los nuevos datos?";
                        TempData["CantidadAnterior"] = relacionExistente.cantidad_requerida;
                        TempData["ObservacionesAnteriores"] = relacionExistente.observaciones;

                        // Guardar los datos del formulario para usarlos después
                        TempData["NuevoInsumoProducto"] = Newtonsoft.Json.JsonConvert.SerializeObject(insumoProducto);
                        TempData["IdProductoFiltro"] = insumoProducto.PRODUCTO_id_producto;

                        // Mantener los datos del formulario en el ViewBag
                        ViewBag.NombreProducto = producto?.Nombre;
                        CargarDependencias(idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto);
                        return View(insumoProducto);
                    }
                    else
                    {
                        // Si existe una relación ACTIVA, mostrar error
                        ModelState.AddModelError("", "Ya existe este insumo asignado al producto (activo).");
                        TempData["Error"] = "Ya existe este insumo asignado al producto (activo).";
                        CargarDependencias(idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto);
                        return View(insumoProducto);
                    }
                }

                // Si no existe ninguna relación, crear una nueva
                string mensaje;
                bool ok = cdInsumoProducto.AgregarInsumoProducto(insumoProducto, out mensaje);

                if (ok)
                {
                    TempData["Exito"] = "Insumo asignado al producto exitosamente.";
                    return RedirectToAction("Index", new { idProducto = insumoProducto.PRODUCTO_id_producto });
                }
                else
                {
                    ModelState.AddModelError("", mensaje);
                    TempData["Error"] = mensaje;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPCIÓN en Create: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                TempData["Error"] = $"Error al crear la relación: {ex.Message}";
            }

            CargarDependencias(idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto);
            return View(insumoProducto);
        }

        // NUEVO MÉTODO: Para reactivar una relación inactiva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReactivarRelacion(int idRelacionInactiva)
        {
            try
            {
                if (!TienePermiso("InsumosProductos"))
                {
                    TempData["Error"] = "No tiene permisos para reactivar relaciones.";
                    return RedirectToAction("Index");
                }

                System.Diagnostics.Debug.WriteLine($"=== ReactivarRelacion ===");
                System.Diagnostics.Debug.WriteLine($"ID relación a reactivar: {idRelacionInactiva}");

                // Obtener los datos del formulario que estaban en TempData
                string insumoProductoJson = TempData["NuevoInsumoProducto"] as string;
                if (string.IsNullOrEmpty(insumoProductoJson))
                {
                    TempData["Error"] = "No se encontraron los datos del formulario.";
                    return RedirectToAction("Create");
                }

                var insumoProducto = Newtonsoft.Json.JsonConvert.DeserializeObject<CMInsumoProducto>(insumoProductoJson);
                insumoProducto.id_insumo_producto = idRelacionInactiva;
                insumoProducto.estado = true; // Reactivar

                // Actualizar la relación existente
                string mensaje;
                bool ok = cdInsumoProducto.ActualizarInsumoProducto(insumoProducto, out mensaje);

                if (ok)
                {
                    TempData["Exito"] = "Relación reactivada exitosamente.";
                    // Obtener el idProducto de TempData para redirigir
                    int? idProductoRedir = TempData["IdProductoFiltro"] as int?;
                    if (idProductoRedir.HasValue)
                    {
                        return RedirectToAction("Index", new { idProducto = idProductoRedir.Value });
                    }
                    else
                    {
                        return RedirectToAction("Index", new { idProducto = insumoProducto.PRODUCTO_id_producto });
                    }
                }
                else
                {
                    TempData["Error"] = $"Error al reactivar: {mensaje}";
                    return RedirectToAction("Create", new { idProducto = insumoProducto.PRODUCTO_id_producto });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en ReactivarRelacion: {ex.Message}");
                TempData["Error"] = $"Error al reactivar: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public ActionResult CostoProduccion(int idProducto)
        {
            if (!TienePermiso("InsumosProductos"))
            {
                TempData["Error"] = "No tiene permisos para ver costos y rentabilidad.";
                return RedirectToAction("Index");
            }

            if (cdPolleria == null) cdPolleria = new CDPolleria();
            var polleriaActiva = cdPolleria.Listar()?.FirstOrDefault(p => p.Estado);
            int idPolleria = polleriaActiva != null ? polleriaActiva.id_polleria : 5;

            var resultado = cdInsumoProducto.CalcularCostoProduccion(idProducto, idPolleria);

            if (resultado.Item1 == null)
            {
                TempData["Error"] = "No se pudieron calcular los costos del producto.";
                return RedirectToAction("Index", new { idProducto = idProducto });
            }

            ViewBag.Detalles = resultado.Item2;
            ViewBag.NombrePolleria = polleriaActiva?.Nombre ?? "El Escondite del Loro";

            return View(resultado.Item1);
        }


        [HttpGet]
        public ActionResult Edit(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== Edit GET - Iniciado ===");
            System.Diagnostics.Debug.WriteLine($"ID recibido: {id}");

            // Verificar permisos
            if (!TienePermiso("InsumosProductos"))
            {
                TempData["Error"] = "No tiene permisos para editar.";
                return RedirectToAction("Index");
            }

            // Obtener la relación por ID
            var insumoProducto = cdInsumoProducto.ObtenerPorId(id);

            if (insumoProducto == null)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: No se encontró relación con ID {id}");
                TempData["Error"] = "Relación insumo-producto no encontrada.";
                return RedirectToAction("Index");
            }

            System.Diagnostics.Debug.WriteLine($"Relación encontrada:");
            System.Diagnostics.Debug.WriteLine($"  - ID: {insumoProducto.id_insumo_producto}");
            System.Diagnostics.Debug.WriteLine($"  - Producto: {insumoProducto.NombreProducto}");
            System.Diagnostics.Debug.WriteLine($"  - Insumo: {insumoProducto.NombreInsumo}");

            // Obtener información del producto
            var producto = cdProducto.ObtenerProductoPorId(insumoProducto.PRODUCTO_id_producto);
            if (producto != null)
            {
                ViewBag.NombreProducto = producto.Nombre;
                ViewBag.ProductoActual = producto;
            }

            // Manejar parámetro idProducto del QueryString
            if (Request.QueryString["idProducto"] != null)
            {
                int idProductoFiltro;
                if (int.TryParse(Request.QueryString["idProducto"], out idProductoFiltro))
                {
                    ViewBag.IdProductoFiltro = idProductoFiltro;
                }
            }
            else
            {
                ViewBag.IdProductoFiltro = insumoProducto.PRODUCTO_id_producto;
            }

            // CORREGIDO: Pasar los parámetros correctamente
            CargarDependencias(
                idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto,
                idInsumoSeleccionado: insumoProducto.INSUMO_id_insumo);

            return View(insumoProducto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CMInsumoProducto insumoProducto)
        {
            System.Diagnostics.Debug.WriteLine($"=== Edit POST - INICIADO ===");
            System.Diagnostics.Debug.WriteLine($"ID Relación: {insumoProducto.id_insumo_producto}");
            System.Diagnostics.Debug.WriteLine($"Producto ID: {insumoProducto.PRODUCTO_id_producto}");
            System.Diagnostics.Debug.WriteLine($"Insumo ID (nuevo): {insumoProducto.INSUMO_id_insumo}");
            System.Diagnostics.Debug.WriteLine($"Cantidad: {insumoProducto.cantidad_requerida}");
            System.Diagnostics.Debug.WriteLine($"Observaciones: {insumoProducto.observaciones}");
            System.Diagnostics.Debug.WriteLine($"Estado: {insumoProducto.estado}");

            // Obtener idProducto del formulario para redirección
            string idProductoForm = Request.Form["idProducto"];
            int? idProductoFiltro = null;

            if (!string.IsNullOrEmpty(idProductoForm))
            {
                int temp;
                if (int.TryParse(idProductoForm, out temp))
                {
                    idProductoFiltro = temp;
                }
            }

            System.Diagnostics.Debug.WriteLine($"idProductoFiltro del formulario: {idProductoFiltro}");

            try
            {
                if (!PuedeEditar("InsumosProductos"))
                {
                    TempData["Error"] = "No tiene permisos para editar relaciones insumo-producto.";
                    return RedirectToAction("Index");
                }

                // Verificar que la relación existe
                var existente = cdInsumoProducto.ObtenerPorId(insumoProducto.id_insumo_producto);
                if (existente == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Relación con ID {insumoProducto.id_insumo_producto} no encontrada");
                    TempData["Error"] = "Relación insumo-producto no encontrada.";
                    return RedirectToAction("Index");
                }

                System.Diagnostics.Debug.WriteLine($"Relación existente encontrada:");
                System.Diagnostics.Debug.WriteLine($"  - Insumo ID (anterior): {existente.INSUMO_id_insumo}");
                System.Diagnostics.Debug.WriteLine($"  - Cantidad anterior: {existente.cantidad_requerida}");
                System.Diagnostics.Debug.WriteLine($"  - Estado anterior: {existente.estado}");

                if (!ModelState.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("ModelState no es válido. Errores:");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {error.ErrorMessage}");
                    }

                    TempData["Error"] = "Por favor, corrija los errores en el formulario.";
                    CargarDependencias(
                        idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto,
                        idInsumoSeleccionado: insumoProducto.INSUMO_id_insumo);
                    ViewBag.IdProductoFiltro = idProductoFiltro ?? insumoProducto.PRODUCTO_id_producto;
                    return View(insumoProducto);
                }

                // Validar que el nuevo insumo exista y esté activo
                var nuevoInsumo = cdInsumo.ObtenerInsumoPorId(insumoProducto.INSUMO_id_insumo);
                if (nuevoInsumo == null || !nuevoInsumo.estado)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Insumo ID {insumoProducto.INSUMO_id_insumo} no existe o está inactivo");
                    ModelState.AddModelError("INSUMO_id_insumo", "El insumo seleccionado no existe o está inactivo.");
                    CargarDependencias(
                        idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto,
                        idInsumoSeleccionado: insumoProducto.INSUMO_id_insumo);
                    ViewBag.IdProductoFiltro = idProductoFiltro ?? insumoProducto.PRODUCTO_id_producto;
                    return View(insumoProducto);
                }

                System.Diagnostics.Debug.WriteLine($"Nuevo insumo validado: {nuevoInsumo.nombre}");

                // VERIFICAR SI CAMBIÓ EL INSUMO
                bool cambioInsumo = existente.INSUMO_id_insumo != insumoProducto.INSUMO_id_insumo;
                System.Diagnostics.Debug.WriteLine($"¿Cambió el insumo? {cambioInsumo}");

                if (cambioInsumo)
                {
                    System.Diagnostics.Debug.WriteLine($"¡CAMBIO DE INSUMO DETECTADO!");
                    System.Diagnostics.Debug.WriteLine($"  Insumo anterior: {existente.INSUMO_id_insumo}");
                    System.Diagnostics.Debug.WriteLine($"  Insumo nuevo: {insumoProducto.INSUMO_id_insumo}");

                    // Verificar si ya existe la nueva relación (para evitar duplicados)
                    var relacionExistente = cdInsumoProducto.ObtenerRelacionExistente(
                        insumoProducto.PRODUCTO_id_producto,
                        insumoProducto.INSUMO_id_insumo);

                    if (relacionExistente != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ya existe una relación con el nuevo insumo:");
                        System.Diagnostics.Debug.WriteLine($"  - ID Relación: {relacionExistente.id_insumo_producto}");
                        System.Diagnostics.Debug.WriteLine($"  - Estado: {relacionExistente.estado}");

                        // Si existe una relación con el nuevo insumo (activa o inactiva)
                        if (!relacionExistente.estado)
                        {
                            // Si existe una relación INACTIVA, preguntar al usuario
                            System.Diagnostics.Debug.WriteLine($"Relación inactiva encontrada, mostrando opción de reactivación");

                            TempData["MensajeCambioInsumo"] = $"Ya existe una asignación inactiva del nuevo insumo al producto. " +
                                                             $"¿Desea reactivarla y eliminar la asignación actual?";
                            TempData["NuevosDatos"] = Newtonsoft.Json.JsonConvert.SerializeObject(insumoProducto);
                            TempData["RelacionInactivaId"] = relacionExistente.id_insumo_producto;
                            TempData["RelacionActualId"] = insumoProducto.id_insumo_producto;
                            TempData["IdProductoFiltro"] = idProductoFiltro ?? insumoProducto.PRODUCTO_id_producto;

                            // Recargar datos para mostrar en la vista
                            var producto = cdProducto.ObtenerProductoPorId(insumoProducto.PRODUCTO_id_producto);
                            if (producto != null)
                            {
                                ViewBag.NombreProducto = producto.Nombre;
                            }

                            CargarDependencias(
                                idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto,
                                idInsumoSeleccionado: insumoProducto.INSUMO_id_insumo);
                            ViewBag.IdProductoFiltro = idProductoFiltro ?? insumoProducto.PRODUCTO_id_producto;

                            System.Diagnostics.Debug.WriteLine($"Redirigiendo a vista con opción de reactivación");
                            return View(insumoProducto);
                        }
                        else
                        {
                            // Si existe una relación ACTIVA, mostrar error
                            System.Diagnostics.Debug.WriteLine($"ERROR: Ya existe una relación ACTIVA con este insumo");
                            ModelState.AddModelError("INSUMO_id_insumo",
                                "Ya existe una asignación activa de este insumo al producto. Seleccione otro insumo.");
                            TempData["Error"] = "Ya existe una asignación activa de este insumo al producto.";

                            var producto = cdProducto.ObtenerProductoPorId(insumoProducto.PRODUCTO_id_producto);
                            if (producto != null)
                            {
                                ViewBag.NombreProducto = producto.Nombre;
                            }

                            CargarDependencias(
                                idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto,
                                idInsumoSeleccionado: insumoProducto.INSUMO_id_insumo);
                            ViewBag.IdProductoFiltro = idProductoFiltro ?? insumoProducto.PRODUCTO_id_producto;
                            return View(insumoProducto);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"No existe relación previa con el nuevo insumo. Actualizando normalmente.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No hubo cambio de insumo. Actualizando normalmente.");
                }

                // Si no hay conflictos, actualizar normalmente
                System.Diagnostics.Debug.WriteLine($"Llamando a ActualizarInsumoProducto...");
                string mensaje;
                bool ok = cdInsumoProducto.ActualizarInsumoProducto(insumoProducto, out mensaje);

                if (ok)
                {
                    TempData["Exito"] = "Relación insumo-producto actualizada exitosamente.";
                    System.Diagnostics.Debug.WriteLine($"Actualización exitosa. Mensaje: {mensaje}");

                    return RedirectToAction("Index", new
                    {
                        idProducto = idProductoFiltro ?? insumoProducto.PRODUCTO_id_producto
                    });
                }
                else
                {
                    ModelState.AddModelError("", mensaje);
                    TempData["Error"] = mensaje;
                    System.Diagnostics.Debug.WriteLine($"Error en actualización: {mensaje}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPCIÓN en Edit POST: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Error al actualizar la relación: {ex.Message}";
            }

            ViewBag.IdProductoFiltro = idProductoFiltro ?? insumoProducto.PRODUCTO_id_producto;

            // Recargar datos del producto
            var productoInfo = cdProducto.ObtenerProductoPorId(insumoProducto.PRODUCTO_id_producto);
            if (productoInfo != null)
            {
                ViewBag.NombreProducto = productoInfo.Nombre;
            }

            CargarDependencias(
                idProductoSeleccionado: insumoProducto.PRODUCTO_id_producto,
                idInsumoSeleccionado: insumoProducto.INSUMO_id_insumo);

            System.Diagnostics.Debug.WriteLine($"=== Edit POST - FINALIZADO ===");
            return View(insumoProducto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarInsumoConReactivacion()
        {
            System.Diagnostics.Debug.WriteLine($"=== CambiarInsumoConReactivacion - INICIADO ===");

            try
            {
                if (!PuedeEditar("InsumosProductos"))
                {
                    TempData["Error"] = "No tiene permisos para realizar esta operación.";
                    return RedirectToAction("Index");
                }

                // Obtener datos de TempData
                string nuevosDatosJson = TempData["NuevosDatos"] as string;
                int? relacionInactivaId = TempData["RelacionInactivaId"] as int?;
                int? relacionActualId = TempData["RelacionActualId"] as int?;
                int? idProductoFiltro = TempData["IdProductoFiltro"] as int?;

                System.Diagnostics.Debug.WriteLine($"Datos recibidos:");
                System.Diagnostics.Debug.WriteLine($"  - NuevosDatos: {(string.IsNullOrEmpty(nuevosDatosJson) ? "VACÍO" : "OK")}");
                System.Diagnostics.Debug.WriteLine($"  - RelacionInactivaId: {relacionInactivaId}");
                System.Diagnostics.Debug.WriteLine($"  - RelacionActualId: {relacionActualId}");
                System.Diagnostics.Debug.WriteLine($"  - IdProductoFiltro: {idProductoFiltro}");

                if (string.IsNullOrEmpty(nuevosDatosJson) || !relacionInactivaId.HasValue || !relacionActualId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Faltan datos en TempData");
                    TempData["Error"] = "No se encontraron los datos para el cambio.";
                    return RedirectToAction("Index");
                }

                var nuevosDatos = Newtonsoft.Json.JsonConvert.DeserializeObject<CMInsumoProducto>(nuevosDatosJson);

                System.Diagnostics.Debug.WriteLine($"Procesando cambio de insumo:");
                System.Diagnostics.Debug.WriteLine($"  - Producto ID: {nuevosDatos.PRODUCTO_id_producto}");
                System.Diagnostics.Debug.WriteLine($"  - Nuevo Insumo ID: {nuevosDatos.INSUMO_id_insumo}");
                System.Diagnostics.Debug.WriteLine($"  - Cantidad: {nuevosDatos.cantidad_requerida}");

                // PASO 1: Reactivar la relación inactiva con los nuevos datos
                var relacionAReactivar = cdInsumoProducto.ObtenerPorId(relacionInactivaId.Value);
                if (relacionAReactivar != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Reactivando relación ID: {relacionAReactivar.id_insumo_producto}");

                    // Actualizar con los nuevos datos
                    relacionAReactivar.INSUMO_id_insumo = nuevosDatos.INSUMO_id_insumo;
                    relacionAReactivar.cantidad_requerida = nuevosDatos.cantidad_requerida;
                    relacionAReactivar.observaciones = nuevosDatos.observaciones;
                    relacionAReactivar.estado = true;
                    relacionAReactivar.fecha_actualizacion = DateTime.Now;

                    string mensajeReactivar;
                    bool okReactivar = cdInsumoProducto.ActualizarInsumoProducto(relacionAReactivar, out mensajeReactivar);

                    System.Diagnostics.Debug.WriteLine($"Resultado reactivación: {(okReactivar ? "ÉXITO" : "FALLÓ")} - {mensajeReactivar}");

                    if (!okReactivar)
                    {
                        TempData["Error"] = $"Error al reactivar: {mensajeReactivar}";
                        return RedirectToAction("Index", new { idProducto = idProductoFiltro });
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: No se encontró relación inactiva con ID: {relacionInactivaId}");
                }

                // PASO 2: Desactivar la relación actual (eliminación lógica)
                System.Diagnostics.Debug.WriteLine($"Desactivando relación actual ID: {relacionActualId}");
                string mensajeEliminar;
                bool okEliminar = cdInsumoProducto.EliminarInsumoProducto(relacionActualId.Value, out mensajeEliminar);

                System.Diagnostics.Debug.WriteLine($"Resultado eliminación: {(okEliminar ? "ÉXITO" : "FALLÓ")} - {mensajeEliminar}");

                if (!okEliminar)
                {
                    TempData["Error"] = $"Error al eliminar relación anterior: {mensajeEliminar}";
                    return RedirectToAction("Index", new { idProducto = idProductoFiltro });
                }

                TempData["Exito"] = "Insumo cambiado exitosamente. La asignación anterior ha sido desactivada.";
                System.Diagnostics.Debug.WriteLine($"Cambio de insumo completado exitosamente");

                return RedirectToAction("Index", new { idProducto = idProductoFiltro });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en CambiarInsumoConReactivacion: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Error al cambiar el insumo: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // --- ELIMINAR ---
        [HttpGet]
        public ActionResult Delete(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== Delete GET - Iniciado ===");
            System.Diagnostics.Debug.WriteLine($"ID recibido: {id}");

            if (!PuedeEliminar("InsumosProductos"))
            {
                TempData["Error"] = "No tiene permisos para eliminar.";
                return RedirectToAction("Index");
            }

            var insumoProducto = cdInsumoProducto.ObtenerPorId(id);

            if (insumoProducto == null)
            {
                TempData["Error"] = "Relación insumo-producto no encontrada.";
                return RedirectToAction("Index");
            }

            System.Diagnostics.Debug.WriteLine($"Relación encontrada para eliminar: ID={insumoProducto.id_insumo_producto}");

            // Obtener información del producto
            var producto = cdProducto.ObtenerProductoPorId(insumoProducto.PRODUCTO_id_producto);
            if (producto != null)
            {
                ViewBag.NombreProducto = producto.Nombre;
            }

            // Manejar parámetro idProducto
            if (Request.QueryString["idProducto"] != null)
            {
                int idProductoFiltro;
                if (int.TryParse(Request.QueryString["idProducto"], out idProductoFiltro))
                {
                    ViewBag.IdProductoFiltro = idProductoFiltro;
                }
            }

            return View(insumoProducto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== DeleteConfirmed - Iniciado ===");
            System.Diagnostics.Debug.WriteLine($"ID a eliminar: {id}");

            if (!PuedeEliminar("InsumosProductos"))
            {
                TempData["Error"] = "No tiene permisos para eliminar.";
                return RedirectToAction("Index");
            }

            try
            {
                // Obtener la relación primero para saber el producto
                var insumoProducto = cdInsumoProducto.ObtenerPorId(id);
                if (insumoProducto == null)
                {
                    TempData["Error"] = "Relación insumo-producto no encontrada.";
                    return RedirectToAction("Index");
                }

                string mensaje;
                bool ok = cdInsumoProducto.EliminarInsumoProducto(id, out mensaje);

                if (ok)
                {
                    TempData["Exito"] = "Relación eliminada exitosamente.";
                }
                else
                {
                    TempData["Error"] = mensaje;
                }

                // Obtener idProducto del formulario para redirección
                string idProductoStr = Request.Form["idProducto"];
                int idProductoRedir;

                if (!string.IsNullOrEmpty(idProductoStr) && int.TryParse(idProductoStr, out idProductoRedir))
                {
                    return RedirectToAction("Index", new { idProducto = idProductoRedir });
                }
                else
                {
                    return RedirectToAction("Index", new { idProducto = insumoProducto.PRODUCTO_id_producto });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en DeleteConfirmed: {ex.Message}");
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public JsonResult EliminarAjax(int id)
        {
            try
            {
                if (!PuedeEliminar("InsumosProductos"))
                {
                    return Json(new { success = false, message = "No tiene permisos para eliminar relaciones insumo-producto." });
                }

                if (id <= 0)
                {
                    return Json(new { success = false, message = "Identificador de asignación inválido." });
                }

                string mensaje;
                bool ok = cdInsumoProducto.EliminarInsumoProducto(id, out mensaje);

                if (ok)
                {
                    return Json(new { success = true, message = "Asignación eliminada correctamente." });
                }
                else
                {
                    return Json(new { success = false, message = string.IsNullOrEmpty(mensaje) ? "No se pudo eliminar la asignación." : mensaje });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateMultiple(int idProducto, string insumosJson)
        {
            try
            {
                if (!TienePermiso("InsumosProductos"))
                {
                    TempData["Error"] = "No tiene permisos para crear relaciones insumo-producto.";
                    return RedirectToAction("Index");
                }

                if (idProducto <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un producto válido.";
                    return RedirectToAction("Create");
                }

                if (string.IsNullOrEmpty(insumosJson) || insumosJson == "[]")
                {
                    TempData["Error"] = "Debe agregar al menos un insumo a la receta.";
                    return RedirectToAction("Create", new { idProducto = idProducto });
                }

                var listaInsumos = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CMInsumoProducto>>(insumosJson);

                if (listaInsumos == null || !listaInsumos.Any())
                {
                    TempData["Error"] = "No se recibieron insumos válidos.";
                    return RedirectToAction("Create", new { idProducto = idProducto });
                }

                string mensaje;
                bool ok = cdInsumoProducto.RegistrarRecetaMultiple(idProducto, listaInsumos, out mensaje);

                if (ok)
                {
                    TempData["Exito"] = mensaje;
                    return RedirectToAction("Index", new { idProducto = idProducto });
                }
                else
                {
                    TempData["Error"] = mensaje;
                    return RedirectToAction("Create", new { idProducto = idProducto });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la receta: " + ex.Message;
                return RedirectToAction("Create", new { idProducto = idProducto });
            }
        }

        private void CargarDependencias(int? idProductoSeleccionado = null, int? idInsumoSeleccionado = null)
        {
            // Filtramos Bebidas (ID != 1) y por seguridad descartamos si el nombre contiene Soda/Gaseosa/Coca
            var productos = cdProducto.ListarProductos(null)
                .Where(p => p.Estado == true &&
                            !p.Nombre.ToLower().StartsWith("soda") &&
                            !p.Nombre.ToLower().Contains("coca-cola") &&
                            !p.Nombre.ToLower().Contains("fanta") &&
                            !p.Nombre.ToLower().Contains("sprite"))
                .OrderBy(p => p.Nombre)
                .ToList();

            ViewBag.ListaProductos = new SelectList(productos, "id_producto", "Nombre", idProductoSeleccionado);

            // Lista de insumos activos
            var insumos = cdInsumo.ListarInsumos()
                .Where(i => i.estado == true)
                .OrderBy(i => i.nombre)
                .Select(i => new {
                    i.id_insumo,
                    Nombre = $"{i.nombre} - {(string.IsNullOrEmpty(i.Abreviatura) ? (i.NombreMedida ?? "Sin medida") : i.Abreviatura)}"
                })
                .ToList();

            ViewBag.ListaInsumos = new SelectList(insumos, "id_insumo", "Nombre", idInsumoSeleccionado);
        }

        // --- MÉTODO PARA PRUEBAS ---
        public ActionResult TestRelacion(int id = 1)
        {
            System.Diagnostics.Debug.WriteLine($"=== TestRelacion ===");

            var resultado = cdInsumoProducto.ObtenerPorId(id);

            ViewBag.Relacion = resultado;
            ViewBag.Mensaje = resultado != null ?
                $"Encontrado: ID={resultado.id_insumo_producto}, Producto={resultado.NombreProducto}" :
                $"No encontrado relación con ID={id}";

            return View();
        }
    }
}