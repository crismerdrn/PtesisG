using CapaModelo;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static CapaModelo.CMDetalleVentaStockXML;
using static CapaModelo.CMVenta;

namespace CapaDatos

{
    public class CDVenta

    {
        public CMRespuestaVentaStock RegistrarVentaConStockControl(

            int idUsuario,

            int? idCliente,

            int idPolleria,

            string tipoVenta,

            List<CMDetalleVenta> detalles,

            string observaciones = null,

            int? idTipoVenta = null) 

        {

            CMRespuestaVentaStock respuesta = new CMRespuestaVentaStock();
            try

            {

                var validacion = ValidarStockParaVenta(idPolleria, detalles);

                respuesta.ValidacionesStock = validacion.ValidacionesStock;

                respuesta.StockSuficiente = validacion.StockSuficiente;



                if (!validacion.StockSuficiente)

                {

                    respuesta.Resultado = false;

                    respuesta.Mensaje = "Stock insuficiente para completar la venta";

                    respuesta.StockSuficiente = false;

                    return respuesta;

                }



                string xmlDetalles = GenerarXMLDetallesStock(detalles);



                using (SqlConnection cn = new SqlConnection(Conexion.CN))

                {

                    SqlCommand cmd = new SqlCommand("sp_Venta_RegistrarConStock", cn);

                    cmd.CommandType = CommandType.StoredProcedure;



                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    cmd.Parameters.AddWithValue("@IdCliente", idCliente ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@IdPolleria", idPolleria);

                    cmd.Parameters.AddWithValue("@TipoVenta", tipoVenta ?? "EFECTIVO");

                    cmd.Parameters.AddWithValue("@IdTipoVenta", (object)idTipoVenta ?? DBNull.Value); 

                    cmd.Parameters.AddWithValue("@Detalles", xmlDetalles);

                    cmd.Parameters.AddWithValue("@Observaciones", observaciones ?? (object)DBNull.Value);



                    cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@IdVentaGenerada", SqlDbType.Int).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@NumeroVenta", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;



                    cn.Open();

                    cmd.ExecuteNonQuery();



                    respuesta.Resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);

                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value.ToString();

                    respuesta.IdVentaGenerada = cmd.Parameters["@IdVentaGenerada"].Value != DBNull.Value ?

                        Convert.ToInt32(cmd.Parameters["@IdVentaGenerada"].Value) : 0;

                    respuesta.NumeroVenta = cmd.Parameters["@NumeroVenta"].Value.ToString();

                    respuesta.FechaVenta = DateTime.Now;



                    if (respuesta.IdVentaGenerada > 0)

                    {

                        respuesta.TotalVenta = ObtenerTotalVenta(respuesta.IdVentaGenerada);

                    }

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en RegistrarVentaConStockControl: {ex.Message}");

                respuesta.Resultado = false;

                respuesta.Mensaje = "Error al registrar venta: " + ex.Message;

                respuesta.StockSuficiente = false;

            }



            return respuesta;

        }



        public List<CMTipoVenta> ListarTiposVenta()

        {

            List<CMTipoVenta> lista = new List<CMTipoVenta>();

            try

            {

                using (var conexion = new SqlConnection(Conexion.CN))

                {

                    conexion.Open();

                    string query = "SELECT id_tipoVenta, nombre, descripcion, estado FROM TIPO_VENTA WHERE estado = 1 ORDER BY id_tipoVenta ASC";

                    lista = conexion.Query<CMTipoVenta>(query).ToList();

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"Error al listar tipos de venta: {ex.Message}");

            }

            return lista;

        }


        public CMRespuestaVentaStock ValidarStockParaVenta(int idPolleria, List<CMDetalleVenta> detalles)

        {

            CMRespuestaVentaStock respuesta = new CMRespuestaVentaStock();



            try

            {

                if (detalles == null || detalles.Count == 0)

                {

                    respuesta.Resultado = false;

                    respuesta.Mensaje = "No hay detalles para validar";

                    respuesta.StockSuficiente = false;

                    return respuesta;

                }



                // Generar XML para el SP

                string xmlDetalles = GenerarXMLDetallesParaValidacion(detalles);



                using (SqlConnection cn = new SqlConnection(Conexion.CN))

                {

                    SqlCommand cmd = new SqlCommand("sp_Venta_ValidarStock", cn);

                    cmd.CommandType = CommandType.StoredProcedure;



                    cmd.Parameters.AddWithValue("@IdPolleria", idPolleria);

                    cmd.Parameters.AddWithValue("@Detalles", xmlDetalles);



                    // Parámetros de salida

                    cmd.Parameters.Add("@StockSuficiente", SqlDbType.Bit).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 1000).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@DetallesValidados", SqlDbType.Xml).Direction = ParameterDirection.Output;



                    cn.Open();

                    cmd.ExecuteNonQuery();



                    respuesta.StockSuficiente = Convert.ToBoolean(cmd.Parameters["@StockSuficiente"].Value);

                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value.ToString();



                    // Procesar detalles validados desde XML

                    if (cmd.Parameters["@DetallesValidados"].Value != DBNull.Value)

                    {

                        string xmlValidados = cmd.Parameters["@DetallesValidados"].Value.ToString();

                        respuesta.ValidacionesStock = ParsearValidacionesDesdeXML(xmlValidados);

                    }



                    respuesta.Resultado = true;

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en ValidarStockParaVenta: {ex.Message}");

                respuesta.Resultado = false;

                respuesta.Mensaje = "Error al validar stock: " + ex.Message;

                respuesta.StockSuficiente = false;

            }



            return respuesta;

        }



        // 3. ANULAR VENTA Y REVERTIR STOCK

        public CMRespuestaVentaStock AnularVentaConRevertirStock(int idVenta, int idUsuario, string motivo)

        {

            CMRespuestaVentaStock respuesta = new CMRespuestaVentaStock();



            try

            {

                using (SqlConnection cn = new SqlConnection(Conexion.CN))

                {

                    SqlCommand cmd = new SqlCommand("sp_Venta_Anular", cn);

                    cmd.CommandType = CommandType.StoredProcedure;



                    cmd.Parameters.AddWithValue("@IdVenta", idVenta);

                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    cmd.Parameters.AddWithValue("@Motivo", motivo);



                    cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;



                    cn.Open();

                    cmd.ExecuteNonQuery();



                    respuesta.Resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);

                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value.ToString();

                    respuesta.IdVentaGenerada = idVenta;

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en AnularVentaConRevertirStock: {ex.Message}");

                respuesta.Resultado = false;

                respuesta.Mensaje = "Error al anular venta: " + ex.Message;

            }



            return respuesta;

        }



        // 4. OBTENER STOCK DISPONIBLE

        public List<CMProductoStock> ObtenerStockDisponible(int? idPolleria = null, bool soloConStock = true)

        {

            var lista = new List<CMProductoStock>();



            try

            {

                int polleriaId = idPolleria ?? 5;



                using (var conexion = new SqlConnection(Conexion.CN))

                {

                    conexion.Open();



                    string query = @"

            SELECT 

                p.id_producto AS IdProducto,

                p.Nombre AS NombreProducto,

                ISNULL(p.Descripcion, '') AS Descripcion,

                ISNULL(p.PrecioVenta, 0) AS PrecioVenta,

                ISNULL(m.Descripcion, 'Unidad') AS Medida,

                ISNULL(m.Abreviatura, 'Un.') AS Abreviatura,

                tp.descripcion AS TipoProducto,

                ISNULL(s.stock_minimo, 0) AS StockMinimo,

                @IdPolleria AS IdPolleria,

                poll.Nombre AS NombrePolleria,

                -- Si es elaborado, mostramos disponibilidad virtual alta; si no, el stock de almacén

                CASE 

                    WHEN EXISTS (SELECT 1 FROM INSUMO_PRODUCTO ip WHERE ip.PRODUCTO_id_producto = p.id_producto AND ip.estado = 1) THEN 999

                    ELSE ISNULL(s.stock_actual, 0)

                END AS StockActual,

                CASE 

                    WHEN EXISTS (SELECT 1 FROM INSUMO_PRODUCTO ip WHERE ip.PRODUCTO_id_producto = p.id_producto AND ip.estado = 1) THEN 'DISPONIBLE'

                    WHEN ISNULL(s.stock_actual, 0) <= 0 THEN 'SIN STOCK'

                    WHEN ISNULL(s.stock_actual, 0) <= ISNULL(s.stock_minimo, 0) THEN 'BAJO'

                    ELSE 'NORMAL'

                END AS EstadoStock

            FROM PRODUCTO p

            LEFT JOIN STOCK s ON p.id_producto = s.PRODUCTO_id_producto AND s.POLLERIA_id_polleria = @IdPolleria

            LEFT JOIN POLLERIA poll ON poll.id_polleria = @IdPolleria

            LEFT JOIN MEDIDAS m ON p.MEDIDAS_id_medida = m.id_medida

            LEFT JOIN TIPO_PRODUCTO tp ON p.TIPO_PRODUCTO_id_tipoProducto = tp.id_tipoProducto

            WHERE p.Estado = 1

              -- Filtro: si soloConStock = 1, los elaborados SIEMPRE pasan, y los de reventa deben tener stock > 0

              AND (

                  EXISTS (SELECT 1 FROM INSUMO_PRODUCTO ip WHERE ip.PRODUCTO_id_producto = p.id_producto AND ip.estado = 1)

                  OR (@SoloConStock = 0 OR ISNULL(s.stock_actual, 0) > 0)

              )

            ORDER BY p.Nombre ASC";



                    var parametros = new

                    {

                        IdPolleria = polleriaId,

                        SoloConStock = soloConStock ? 1 : 0

                    };



                    lista = conexion.Query<CMProductoStock>(query, parametros).ToList();



                    System.Diagnostics.Debug.WriteLine($"Productos encontrados para venta: {lista.Count}");

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"ERROR ObtenerStockDisponible: {ex.Message}");

                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

            }



            return lista;

        }

        public CMRespuestaVentaStock TransferirStockEntrePollerias(CMTransfersVentaStock transferencia)

        {

            CMRespuestaVentaStock respuesta = new CMRespuestaVentaStock();



            try

            {

                using (SqlConnection cn = new SqlConnection(Conexion.CN))

                {

                    SqlCommand cmd = new SqlCommand("sp_Venta_TransferirEntrePollerias", cn);

                    cmd.CommandType = CommandType.StoredProcedure;



                    cmd.Parameters.AddWithValue("@IdProducto", transferencia.IdProducto);

                    cmd.Parameters.AddWithValue("@IdPolleriaOrigen", transferencia.IdPolleriaOrigen);

                    cmd.Parameters.AddWithValue("@IdPolleriaDestino", transferencia.IdPolleriaDestino);

                    cmd.Parameters.AddWithValue("@Cantidad", transferencia.Cantidad);

                    cmd.Parameters.AddWithValue("@IdUsuario", transferencia.IdUsuario);

                    cmd.Parameters.AddWithValue("@Motivo", transferencia.Motivo);



                    cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;



                    cn.Open();

                    cmd.ExecuteNonQuery();



                    respuesta.Resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);

                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value.ToString();

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en TransferirStockEntrePollerias: {ex.Message}");

                respuesta.Resultado = false;

                respuesta.Mensaje = "Error en transferencia: " + ex.Message;

            }



            return respuesta;

        }



        public CMRespuestaVentaStock AjustarStockManual(CMAjusteStock ajuste)

        {

            CMRespuestaVentaStock respuesta = new CMRespuestaVentaStock();



            try

            {

                using (SqlConnection cn = new SqlConnection(Conexion.CN))

                {

                    SqlCommand cmd = new SqlCommand("sp_Venta_AjustarStock", cn);

                    cmd.CommandType = CommandType.StoredProcedure;



                    cmd.Parameters.AddWithValue("@IdProducto", ajuste.IdProducto);

                    cmd.Parameters.AddWithValue("@IdPolleria", ajuste.IdPolleria);

                    cmd.Parameters.AddWithValue("@NuevoStock", ajuste.NuevoStock);

                    cmd.Parameters.AddWithValue("@IdUsuario", ajuste.IdUsuario);

                    cmd.Parameters.AddWithValue("@Motivo", ajuste.Motivo);



                    cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;

                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;



                    cn.Open();

                    cmd.ExecuteNonQuery();



                    respuesta.Resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);

                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value.ToString();

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en AjustarStockManual: {ex.Message}");

                respuesta.Resultado = false;

                respuesta.Mensaje = "Error al ajustar stock: " + ex.Message;

            }



            return respuesta;

        }


        public List<CMMovimientoStock> ObtenerMovimientosStock(

            int? idPolleria = null,

            DateTime? fechaDesde = null,

            DateTime? fechaHasta = null,

            string tipoItem = null,

            string tipoMovimiento = null,

            int? idItem = null)

        {

            List<CMMovimientoStock> movimientos = new List<CMMovimientoStock>();



            try

            {

                using (SqlConnection cn = new SqlConnection(Conexion.CN))

                {

                    SqlCommand cmd = new SqlCommand("sp_ObtenerHistorialMovimientos", cn);

                    cmd.CommandType = CommandType.StoredProcedure;



                    cmd.Parameters.AddWithValue("@id_polleria", idPolleria ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@fecha_desde", fechaDesde ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@fecha_hasta", fechaHasta ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@tipo_item", string.IsNullOrWhiteSpace(tipoItem) ? (object)DBNull.Value : tipoItem);

                    cmd.Parameters.AddWithValue("@tipo_movimiento", string.IsNullOrWhiteSpace(tipoMovimiento) ? (object)DBNull.Value : tipoMovimiento);

                    cmd.Parameters.AddWithValue("@id_item", idItem ?? (object)DBNull.Value);



                    cn.Open();



                    using (SqlDataReader dr = cmd.ExecuteReader())

                    {

                        while (dr.Read())

                        {

                            var movimiento = new CMMovimientoStock

                            {

                                id_movimiento = Convert.ToInt32(dr["id_movimiento"]),

                                fecha_movimiento = Convert.ToDateTime(dr["fecha_movimiento"]),

                                tipo_movimiento = dr["tipo_movimiento"].ToString(),

                                id_referencia = dr["id_referencia"] != DBNull.Value ? Convert.ToInt32(dr["id_referencia"]) : (int?)null,

                                tipo_referencia = dr["tipo_referencia"].ToString(),

                                id_item = Convert.ToInt32(dr["id_item"]),

                                tipo_item = dr["tipo_item"].ToString(),

                                cantidad = Convert.ToDecimal(dr["cantidad"]),

                                stock_anterior = Convert.ToDecimal(dr["stock_anterior"]),

                                stock_nuevo = Convert.ToDecimal(dr["stock_nuevo"]),

                                id_polleria = Convert.ToInt32(dr["id_polleria"]),

                                id_usuario = Convert.ToInt32(dr["id_usuario"]),

                                observaciones = dr["observaciones"].ToString(),

                                fecha_registro = Convert.ToDateTime(dr["fecha_movimiento"]),

                                nombre_item = dr["item_nombre"].ToString(),

                                nombre_polleria = dr["polleria_nombre"].ToString(),

                                nombre_usuario = dr["usuario_nombre"].ToString()

                            };



                            movimientos.Add(movimiento);

                        }

                    }

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en ObtenerMovimientosStock: {ex.Message}");

            }



            return movimientos;

        }



        #region Métodos Auxiliares

        private string GenerarXMLDetallesStock(List<CMDetalleVenta> detalles)
        {
            try
            {
                if (detalles == null || !detalles.Any())
                    return "<Detalles></Detalles>";

                var xml = new StringBuilder();
                xml.Append("<Detalles>");

                foreach (var d in detalles)
                {
                    // Usar CultureInfo.InvariantCulture evita que los decimales usen coma ',' en lugar de punto '.'
                    string cant = d.cantidad.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    string prec = d.precio_unitario.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    string sub = d.subtotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                    xml.Append("<Detalle ");
                    xml.Append($"IdProducto=\"{d.PRODUCTO_id_producto}\" ");
                    xml.Append($"Cantidad=\"{cant}\" ");
                    xml.Append($"PrecioUnitario=\"{prec}\" ");
                    xml.Append($"Subtotal=\"{sub}\" />");
                }

                xml.Append("</Detalles>");
                return xml.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR en GenerarXMLDetallesStock: {ex.Message}");
                throw new Exception("Error al generar XML de detalles: " + ex.Message);
            }
        }
        private string GenerarXMLDetallesParaValidacion(List<CMDetalleVenta> detalles)

        {

            try

            {

                StringBuilder xml = new StringBuilder();

                xml.Append("<Detalles>");



                foreach (var detalle in detalles)

                {

                    xml.Append("<Detalle ");

                    xml.Append($"IdProducto='{detalle.PRODUCTO_id_producto}' ");

                    xml.Append($"Cantidad='{detalle.cantidad}' ");

                    xml.Append($"PrecioUnitario='{detalle.precio_unitario}' ");

                    xml.Append($"/>");

                }



                xml.Append("</Detalles>");

                return xml.ToString();

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en GenerarXMLDetallesParaValidacion: {ex.Message}");

                throw;

            }

        }



        private List<CMValidacionStock> ParsearValidacionesDesdeXML(string xml)

        {

            List<CMValidacionStock> validaciones = new List<CMValidacionStock>();



            try

            {

                if (string.IsNullOrEmpty(xml))

                    return validaciones;



                XmlDocument doc = new XmlDocument();

                doc.LoadXml(xml);



                XmlNodeList nodes = doc.SelectNodes("/DetallesValidados/Detalle");

                foreach (XmlNode node in nodes)

                {

                    var validacion = new CMValidacionStock

                    {

                        IdProducto = Convert.ToInt32(node.Attributes["IdProducto"]?.Value),

                        NombreProducto = node.Attributes["NombreProducto"]?.Value,

                        CantidadSolicitada = Convert.ToDecimal(node.Attributes["Cantidad"]?.Value),

                        StockActual = Convert.ToDecimal(node.Attributes["StockActual"]?.Value),

                        StockMinimo = Convert.ToDecimal(node.Attributes["StockMinimo"]?.Value),

                        Suficiente = Convert.ToBoolean(node.Attributes["Suficiente"]?.Value),

                        Mensaje = node.Attributes["MensajeValidacion"]?.Value

                    };



                    validaciones.Add(validacion);

                }

            }

            catch (Exception ex)

            {

                Debug.WriteLine($"ERROR en ParsearValidacionesDesdeXML: {ex.Message}");

            }



            return validaciones;

        }



        private decimal ObtenerTotalVenta(int idVenta)

        {

            try

            {

                using (SqlConnection cn = new SqlConnection(Conexion.CN))

                {

                    string query = "SELECT Total_venta FROM VENTAS WHERE id_venta = @IdVenta";

                    SqlCommand cmd = new SqlCommand(query, cn);

                    cmd.Parameters.AddWithValue("@IdVenta", idVenta);



                    cn.Open();

                    object result = cmd.ExecuteScalar();

                    return result != null ? Convert.ToDecimal(result) : 0;

                }

            }

            catch (Exception)

            {

                return 0;

            }

        }

        public List<CMVenta> ListarVentas(string estado, DateTime? fechaInicio, DateTime? fechaFin,
                                          int? idCliente, int? idUsuario, int? idPolleria)
        {
            string query = @"
                SELECT
                    v.id_venta,
                    ISNULL(v.NumeroVenta, '') AS NumeroVenta,
                    ISNULL(v.Total_venta, 0) AS Total_venta,
                    v.Fecha_venta,
                    v.hora_venta,
                    ISNULL(v.Estado, 'COMPLETADA') AS Estado,
                    ISNULL(v.Tipo_venta, '') AS Tipo_venta,
                    v.USUARIOS_id_usuario,
                    v.CLIENTE_id_cliente,
                    v.POLLERIA_id_polleria,
                    v.TIPO_VENTA_id_tipoVenta,
                    ISNULL(tv.nombre, '') AS NombreTipoVenta,
                    ISNULL(CONCAT(u.nombre, ' ', u.apellido), 'Vendedor General') AS NombreVendedor,
                    ISNULL(CONCAT(c.Nombre, ' ', c.Apellido), 'Cliente General') AS NombreCliente,
                    ISNULL(c.Telefono, '') AS TelefonoCliente,
                    ISNULL(p.Nombre, '') AS NombrePolleria
                FROM VENTAS v
                LEFT JOIN USUARIOS u ON v.USUARIOS_id_usuario = u.id_usuario
                LEFT JOIN CLIENTE c ON v.CLIENTE_id_cliente = c.id_cliente
                LEFT JOIN POLLERIA p ON v.POLLERIA_id_polleria = p.id_polleria
                LEFT JOIN TIPO_VENTA tv ON v.TIPO_VENTA_id_tipoVenta = tv.id_tipoVenta
                WHERE (@Estado IS NULL OR v.Estado = @Estado)
                  AND (@FechaInicio IS NULL OR CAST(v.Fecha_venta AS date) >= @FechaInicio)
                  AND (@FechaFin IS NULL OR CAST(v.Fecha_venta AS date) <= @FechaFin)
                  AND (@IdCliente IS NULL OR v.CLIENTE_id_cliente = @IdCliente)
                  AND (@IdUsuario IS NULL OR v.USUARIOS_id_usuario = @IdUsuario)
                  AND (@IdPolleria IS NULL OR v.POLLERIA_id_polleria = @IdPolleria)
                ORDER BY v.Fecha_venta DESC, v.hora_venta DESC, v.id_venta DESC";

            using (var conexion = new SqlConnection(Conexion.CN))
            {
                var lista = conexion.Query<CMVenta>(query, new
                {
                    Estado = string.IsNullOrWhiteSpace(estado) ? null : estado,
                    FechaInicio = fechaInicio?.Date,
                    FechaFin = fechaFin?.Date,
                    IdCliente = idCliente,
                    IdUsuario = idUsuario,
                    IdPolleria = idPolleria
                }).ToList();

                foreach (var venta in lista)
                {
                    venta.FechaVentaFormateada = venta.Fecha_venta.ToString("dd/MM/yyyy");
                    venta.HoraVentaFormateada = venta.hora_venta.ToString(@"hh\:mm");
                    venta.TotalVentaFormateado = venta.Total_venta.ToString("N2");
                }

                return lista;
            }
        }

        public CMVenta ObtenerDetallesVenta(int idVenta)
        {
            CMVenta venta = null;

            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();

                    // 1. Cabecera con LEFT JOINs blindados
                    string queryCabecera = @"
                SELECT 
                    v.id_venta,
                    ISNULL(v.NumeroVenta, '') AS NumeroVenta,
                    ISNULL(v.Total_venta, 0) AS Total_venta,
                    v.Fecha_venta,
                    v.hora_venta,
                    ISNULL(v.Estado, 'COMPLETADA') AS Estado,
                    ISNULL(v.Tipo_venta, 'EFECTIVO') AS Tipo_venta,
                    v.USUARIOS_id_usuario,
                    v.CLIENTE_id_cliente,
                    v.POLLERIA_id_polleria,
                    v.TIPO_VENTA_id_tipoVenta,
                    ISNULL(CONCAT(u.nombre, ' ', u.apellido), 'Vendedor General') AS NombreVendedor,
                    ISNULL(CONCAT(c.Nombre, ' ', c.Apellido), 'Cliente General') AS NombreCliente,
                    ISNULL(c.Telefono, '') AS TelefonoCliente,
                    ISNULL(c.Email, '') AS EmailCliente,
                    ISNULL(p.Nombre, '') AS NombrePolleria,
                    ISNULL(p.Direccion, '') AS DireccionPolleria,
                    ISNULL(tp.nombre, v.Tipo_venta) AS TipoPago
                FROM VENTAS v
                LEFT JOIN USUARIOS u ON v.USUARIOS_id_usuario = u.id_usuario
                LEFT JOIN CLIENTE c ON v.CLIENTE_id_cliente = c.id_cliente
                LEFT JOIN POLLERIA p ON v.POLLERIA_id_polleria = p.id_polleria
                LEFT JOIN PAGO_VENTAS pv ON pv.VENTAS_id_venta = v.id_venta
                LEFT JOIN TIPO_PAGO tp ON pv.TIPO_PAGO_id_tipoPago = tp.id_tipoPago
                WHERE v.id_venta = @IdVenta";

                    venta = conexion.QueryFirstOrDefault<CMVenta>(queryCabecera, new { IdVenta = idVenta });

                    if (venta != null)
                    {
                        // 2. Líneas de detalle
                        string queryDetalles = @"
                    SELECT 
                        dv.cantidad,
                        dv.precio_unitario,
                        dv.subtotal,
                        dv.PRODUCTO_id_producto,
                        ISNULL(p.Nombre, 'Producto sin nombre') AS NombreProducto,
                        ISNULL(p.Descripcion, '') AS DescripcionProducto,
                        ISNULL(m.Descripcion, 'Unidad') AS Medida,
                        ISNULL(m.Abreviatura, 'Un.') AS AbreviaturaMedida
                    FROM DETALLE_VENTAS dv
                    LEFT JOIN PRODUCTO p ON dv.PRODUCTO_id_producto = p.id_producto
                    LEFT JOIN MEDIDAS m ON p.MEDIDAS_id_medida = m.id_medida
                    WHERE dv.VENTAS_id_venta = @IdVenta";

                        venta.Detalles = conexion.Query<CMDetalleVenta>(queryDetalles, new { IdVenta = idVenta }).ToList();

                        // Formateos para la vista
                        venta.FechaVentaFormateada = venta.Fecha_venta.ToString("dd/MM/yyyy");
                        venta.HoraVentaFormateada = venta.hora_venta.ToString(@"hh\:mm");
                        venta.TotalVentaFormateado = venta.Total_venta.ToString("N2");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR en ObtenerDetallesVenta]: {ex.Message}");
                throw new Exception("Error en consulta de detalles: " + ex.Message);
            }

            return venta;
        }
        #endregion

    }



    // Clase auxiliar para productos con stock

    public class CMProductoStock

    {

        public int IdProducto { get; set; }

        public string NombreProducto { get; set; }

        public string Descripcion { get; set; }

        public decimal PrecioVenta { get; set; }

        public string Medida { get; set; }

        public string Abreviatura { get; set; }

        public string TipoProducto { get; set; }

        public int IdPolleria { get; set; }

        public string NombrePolleria { get; set; }

        public decimal StockActual { get; set; }

        public decimal StockMinimo { get; set; }

        public string EstadoStock { get; set; }

        public DateTime FechaUltimoMovimiento { get; set; }

        public bool TieneStockSuficiente { get; set; }



        public string ClaseCSS

        {

            get

            {

                switch (EstadoStock)

                {

                    case "AGOTADO":

                        return "text-danger";



                    case "BAJO":

                        return "text-warning";



                    case "DISPONIBLE":

                        return "text-success";



                    default:

                        return "text-info";

                }

            }

        }



    }

}