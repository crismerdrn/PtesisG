using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Xml.Linq;

namespace CapaDatos
{
    public class CDCompra
    {
        public List<CMCompra> ListarCompras(string estado = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? idProveedor = null)
        {
            List<CMCompra> lista = new List<CMCompra>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_Compra_ListarConStock", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@FechaDesde", (object)fechaDesde?.Date ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaHasta", (object)fechaHasta?.Date ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdProveedor", (object)idProveedor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado", string.IsNullOrWhiteSpace(estado) ? (object)DBNull.Value : estado.Trim());

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMCompra
                            {
                                id_compra = Convert.ToInt32(dr["id_compra"]),
                                Referencia = dr["Referencia"] != DBNull.Value ? dr["Referencia"].ToString() : "",
                                MontoTotal = Convert.ToDecimal(dr["MontoTotal"]),
                                FechaCompra = Convert.ToDateTime(dr["FechaCompra"]),
                                Estado = dr["Estado"].ToString(),
                                PROVEEDOR_id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
                                NombreProveedor = dr["Proveedor"].ToString(),
                                DocumentoProveedor = dr["DocumentoProveedor"] != DBNull.Value ? dr["DocumentoProveedor"].ToString() : "",
                                NombreUsuario = dr["Usuario"].ToString(),
                                TotalItems = Convert.ToInt32(dr["TotalItems"]),
                                IdTipoPago = dr["IdTipoPago"] != DBNull.Value ? Convert.ToInt32(dr["IdTipoPago"]) : (int?)1,
                                TipoPago = dr["TipoPago"] != DBNull.Value ? dr["TipoPago"].ToString() : ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMCompra>();
                Debug.WriteLine($"ERROR en ListarCompras con SP: {ex.Message}");
            }

            return lista;
        }

        public CMRespuesta RegistrarCompraConSP(CMCompra compra, List<CMDetalleCompra> detalles, int idPolleria)
        {
            CMRespuesta respuesta = new CMRespuesta();

            try
            {
                string xmlDetalles = GenerarXmlDetalles(detalles);

                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_Compra_Registrar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdUsuario", compra.USUARIOS_id_usuario);
                    cmd.Parameters.AddWithValue("@IdProveedor", compra.PROVEEDOR_id_proveedor);
                    cmd.Parameters.AddWithValue("@IdPolleria", idPolleria);
                    cmd.Parameters.AddWithValue("@IdTipoCompra", (object)compra.TIPO_COMPRA_id_tipoCompra ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdTipoPago", (object)compra.IdTipoPago ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Referencia", string.IsNullOrEmpty(compra.Referencia) ? (object)DBNull.Value : compra.Referencia);
                    cmd.Parameters.AddWithValue("@MontoTotal", compra.MontoTotal);
                    cmd.Parameters.AddWithValue("@Estado", compra.Estado);
                    cmd.Parameters.AddWithValue("@Detalles", xmlDetalles);

                    cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@IdCompraGenerada", SqlDbType.Int).Direction = ParameterDirection.Output;

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    respuesta.Resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value?.ToString() ?? "Sin mensaje";

                    if (respuesta.Resultado)
                    {
                        respuesta.IdGenerado = Convert.ToInt32(cmd.Parameters["@IdCompraGenerada"].Value);
                        Debug.WriteLine($"Compra registrada exitosamente. ID: {respuesta.IdGenerado}");
                    }
                    else
                    {
                        Debug.WriteLine($"Error al registrar compra: {respuesta.Mensaje}");
                    }
                }
            }
            catch (Exception ex)
            {
                respuesta.Resultado = false;
                respuesta.Mensaje = "Error al registrar compra: " + ex.Message;
                Debug.WriteLine($"ERROR en RegistrarCompraConSP: {ex.Message}");
            }

            return respuesta;
        }

        private string GenerarXmlDetalles(List<CMDetalleCompra> detalles)
        {
            XElement detallesXml = new XElement("Detalles");

            foreach (var detalle in detalles)
            {
                XElement detalleXml = new XElement("Detalle",
                    new XAttribute("IdItem", detalle.IdItemSeleccionado),
                    new XAttribute("Tipo", detalle.TipoItem),
                    new XAttribute("Cantidad", detalle.cantidad),
                    new XAttribute("PrecioUnitario", detalle.precioUnitario),
                    new XAttribute("Subtotal", detalle.subtotal)
                );

                detallesXml.Add(detalleXml);
            }

            return detallesXml.ToString();
        }

        public CMCompraDetalles ObtenerDetallesCompra(int idCompra)
        {
            CMCompraDetalles resultado = new CMCompraDetalles();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_Compra_ObtenerDetalles", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdCompra", idCompra);

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            resultado.Cabecera = new CMCabeceraCompra
                            {
                                id_compra = Convert.ToInt32(dr["id_compra"]),
                                Referencia = dr["Referencia"] != DBNull.Value ? dr["Referencia"].ToString() : "",
                                MontoTotal = Convert.ToDecimal(dr["MontoTotal"]),
                                FechaCompra = Convert.ToDateTime(dr["FechaCompra"]),
                                Estado = dr["Estado"].ToString(),
                                Usuario = dr["Usuario"].ToString(),
                                Proveedor = dr["Proveedor"].ToString(),
                                ProveedorCI = dr["ProveedorCI"] != DBNull.Value ? dr["ProveedorCI"].ToString() : "",
                                ProveedorTelefono = dr["ProveedorTelefono"] != DBNull.Value ? dr["ProveedorTelefono"].ToString() : ""
                            };
                        }

                        if (dr.NextResult())
                        {
                            while (dr.Read())
                            {
                                resultado.DetallesProductos.Add(new CMDetalleCompraConsulta
                                {
                                    Tipo = dr["Tipo"].ToString(),
                                    cantidad = Convert.ToDecimal(dr["cantidad"]),
                                    precioUnitario = Convert.ToDecimal(dr["precioUnitario"]),
                                    subtotal = Convert.ToDecimal(dr["subtotal"]),
                                    ItemNombre = dr["ItemNombre"].ToString(),
                                    ItemDescripcion = dr["ItemDescripcion"] != DBNull.Value ? dr["ItemDescripcion"].ToString() : "",
                                    Medida = dr["Medida"] != DBNull.Value ? dr["Medida"].ToString() : "",
                                    AbreviaturaMedida = dr["AbreviaturaMedida"] != DBNull.Value ? dr["AbreviaturaMedida"].ToString() : ""
                                });
                            }
                        }

                        if (dr.NextResult())
                        {
                            while (dr.Read())
                            {
                                resultado.DetallesInsumos.Add(new CMDetalleCompraConsulta
                                {
                                    Tipo = dr["Tipo"].ToString(),
                                    cantidad = Convert.ToDecimal(dr["cantidad"]),
                                    precioUnitario = Convert.ToDecimal(dr["precioUnitario"]),
                                    subtotal = Convert.ToDecimal(dr["subtotal"]),
                                    ItemNombre = dr["ItemNombre"].ToString(),
                                    ItemDescripcion = dr["ItemDescripcion"] != DBNull.Value ? dr["ItemDescripcion"].ToString() : "",
                                    Medida = dr["Medida"] != DBNull.Value ? dr["Medida"].ToString() : "",
                                    AbreviaturaMedida = dr["AbreviaturaMedida"] != DBNull.Value ? dr["AbreviaturaMedida"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR en ObtenerDetallesCompra: {ex.Message}");
                resultado = new CMCompraDetalles();
            }

            return resultado;
        }

        public CMCompra ObtenerCompraPorId(int id)
        {
            CMCompra compra = null;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    // Lectura normalizada: se obtiene el tipo de pago desde PAGO_COMPRA
                    string queryCabecera = @"
                SELECT 
                    c.id_compra,
                    c.Referencia,
                    c.MontoTotal,
                    c.FechaCompra,
                    c.FechaRegistro,
                    c.Estado,
                    c.USUARIOS_id_usuario,
                    c.PROVEEDOR_id_proveedor,
                    c.TIPO_COMPRA_id_tipoCompra,
                    ISNULL(pc.TIPO_PAGO_id_tipoPago, 1) AS IdTipoPago,
                    ISNULL(tp.nombre, 'Sin Pago') AS NombreTipoPago,
                    CASE 
                        WHEN p.esEmpresa = 1 THEN p.RazonSocial 
                        ELSE p.Nombre + ' ' + p.Apellido 
                    END AS NombreProveedor,
                    CASE WHEN p.esEmpresa = 1 THEN p.NIT ELSE p.CI END AS DocumentoProveedor,
                    u.nombre + ' ' + u.apellido AS NombreUsuario,
                    tc.Nombre AS NombreTipoCompra
                FROM COMPRA c
                INNER JOIN PROVEEDOR p ON c.PROVEEDOR_id_proveedor = p.id_proveedor
                INNER JOIN USUARIOS u ON c.USUARIOS_id_usuario = u.id_usuario
                LEFT JOIN TIPO_COMPRA tc ON c.TIPO_COMPRA_id_tipoCompra = tc.id_tipoCompra
                LEFT JOIN PAGO_COMPRA pc ON pc.COMPRA_id_compra = c.id_compra AND pc.estado = 1
                LEFT JOIN TIPO_PAGO tp ON tp.id_tipoPago = pc.TIPO_PAGO_id_tipoPago
                WHERE c.id_compra = @id";

                    SqlCommand cmdCabecera = new SqlCommand(queryCabecera, cn);
                    cmdCabecera.Parameters.AddWithValue("@id", id);

                    cn.Open();

                    using (SqlDataReader dr = cmdCabecera.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            compra = new CMCompra
                            {
                                id_compra = Convert.ToInt32(dr["id_compra"]),
                                Referencia = dr["Referencia"] != DBNull.Value ? dr["Referencia"].ToString() : "",
                                MontoTotal = Convert.ToDecimal(dr["MontoTotal"]),
                                FechaCompra = Convert.ToDateTime(dr["FechaCompra"]),
                                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                                Estado = dr["Estado"].ToString(),
                                IdTipoPago = Convert.ToInt32(dr["IdTipoPago"]),
                                TipoPago = dr["NombreTipoPago"] != DBNull.Value ? dr["NombreTipoPago"].ToString() : "",
                                USUARIOS_id_usuario = Convert.ToInt32(dr["USUARIOS_id_usuario"]),
                                PROVEEDOR_id_proveedor = Convert.ToInt32(dr["PROVEEDOR_id_proveedor"]),
                                TIPO_COMPRA_id_tipoCompra = dr["TIPO_COMPRA_id_tipoCompra"] != DBNull.Value ?
                                    Convert.ToInt32(dr["TIPO_COMPRA_id_tipoCompra"]) : (int?)null,
                                NombreProveedor = dr["NombreProveedor"].ToString(),
                                DocumentoProveedor = dr["DocumentoProveedor"].ToString(),
                                NombreUsuario = dr["NombreUsuario"].ToString(),
                                NombreTipoCompra = dr["NombreTipoCompra"] != DBNull.Value ?
                                    dr["NombreTipoCompra"].ToString() : "SIN TIPO"
                            };
                        }
                    }

                    if (compra != null)
                    {
                        string queryDetalles = @"
                    SELECT 
                        dc.id_detalleCompra,
                        dc.precioUnitario,
                        dc.cantidad,
                        dc.subtotal,
                        dc.COMPRA_id_compra,
                        dc.PRODUCTO_id_producto,
                        dc.INSUMO_id_insumo,
                        CASE 
                            WHEN dc.PRODUCTO_id_producto IS NOT NULL THEN 'PRODUCTO'
                            WHEN dc.INSUMO_id_insumo IS NOT NULL THEN 'INSUMO'
                        END AS TipoItem,
                        COALESCE(p.Nombre, i.nombre) AS NombreItem,
                        COALESCE(p.Descripcion, i.descripcion) AS DescripcionItem,
                        COALESCE(mp.Descripcion, mi.Descripcion) AS MedidaItem,
                        COALESCE(mp.Abreviatura, mi.Abreviatura) AS AbreviaturaMedida,
                        CASE 
                            WHEN dc.PRODUCTO_id_producto IS NOT NULL THEN 
                                ISNULL(s.stock_actual, 0)
                            WHEN dc.INSUMO_id_insumo IS NOT NULL THEN 
                                ISNULL(si.stock_actual, 0)
                            ELSE 0
                        END AS StockActual
                    FROM DETALLE_COMPRA dc
                    LEFT JOIN PRODUCTO p ON dc.PRODUCTO_id_producto = p.id_producto
                    LEFT JOIN INSUMO i ON dc.INSUMO_id_insumo = i.id_insumo
                    LEFT JOIN MEDIDAS mp ON p.MEDIDAS_id_medida = mp.id_medida
                    LEFT JOIN MEDIDAS mi ON i.MEDIDAS_id_medida = mi.id_medida
                    LEFT JOIN STOCK s ON p.id_producto = s.PRODUCTO_id_producto AND s.POLLERIA_id_polleria = 
                        (SELECT TOP 1 id_polleria FROM POLLERIA WHERE Estado = 1)
                    LEFT JOIN STOCK_INSUMO si ON i.id_insumo = si.INSUMO_id_insumo AND si.POLLERIA_id_polleria = 
                        (SELECT TOP 1 id_polleria FROM POLLERIA WHERE Estado = 1)
                    WHERE dc.COMPRA_id_compra = @id";

                        SqlCommand cmdDetalles = new SqlCommand(queryDetalles, cn);
                        cmdDetalles.Parameters.AddWithValue("@id", id);

                        using (SqlDataReader dr = cmdDetalles.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                int? idProd = dr["PRODUCTO_id_producto"] != DBNull.Value ? Convert.ToInt32(dr["PRODUCTO_id_producto"]) : (int?)null;
                                int? idIns = dr["INSUMO_id_insumo"] != DBNull.Value ? Convert.ToInt32(dr["INSUMO_id_insumo"]) : (int?)null;

                                compra.Detalles.Add(new CMDetalleCompra
                                {
                                    id_detalleCompra = Convert.ToInt32(dr["id_detalleCompra"]),
                                    precioUnitario = Convert.ToDecimal(dr["precioUnitario"]),
                                    cantidad = Convert.ToDecimal(dr["cantidad"]),
                                    subtotal = Convert.ToDecimal(dr["subtotal"]),
                                    COMPRA_id_compra = Convert.ToInt32(dr["COMPRA_id_compra"]),
                                    PRODUCTO_id_producto = idProd,
                                    INSUMO_id_insumo = idIns,
                                    IdItemSeleccionado = idProd ?? idIns ?? 0,
                                    TipoItem = dr["TipoItem"].ToString(),
                                    NombreItem = dr["NombreItem"].ToString(),
                                    DescripcionItem = dr["DescripcionItem"] != DBNull.Value ? dr["DescripcionItem"].ToString() : "",
                                    MedidaItem = dr["MedidaItem"] != DBNull.Value ? dr["MedidaItem"].ToString() : "",
                                    AbreviaturaMedida = dr["AbreviaturaMedida"] != DBNull.Value ? dr["AbreviaturaMedida"].ToString() : "",
                                    StockActual = Convert.ToDecimal(dr["StockActual"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR en ObtenerCompraPorId: {ex.Message}");
                throw new Exception(ex.Message, ex);
            }

            return compra;
        }

        public CMRespuesta AnularCompraConSP(int idCompra, string motivo, int idUsuario)
        {
            CMRespuesta respuesta = new CMRespuesta();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_Compra_Anular", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdCompra", idCompra);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.Parameters.AddWithValue("@Motivo", motivo);

                    cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    respuesta.Resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value?.ToString() ?? "Sin mensaje";
                }
            }
            catch (Exception ex)
            {
                respuesta.Resultado = false;
                respuesta.Mensaje = "Error al anular compra: " + ex.Message;
                Debug.WriteLine($"ERROR en AnularCompraConSP: {ex.Message}");
            }

            return respuesta;
        }

        public CMRespuesta CompletarCompraConSP(int idCompra, int idPolleria, int idUsuario)
        {
            CMRespuesta respuesta = new CMRespuesta();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_Compra_Completar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdCompra", idCompra);
                    cmd.Parameters.AddWithValue("@IdPolleria", idPolleria);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    respuesta.Resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                    respuesta.Mensaje = cmd.Parameters["@Mensaje"].Value?.ToString() ?? "Sin mensaje";
                }
            }
            catch (Exception ex)
            {
                respuesta.Resultado = false;
                respuesta.Mensaje = "Error al completar compra: " + ex.Message;
                Debug.WriteLine($"ERROR en CompletarCompraConSP: {ex.Message}");
            }

            return respuesta;
        }

        public CMRespuesta CambiarEstadoCompra(int idCompra, string nuevoEstado, int idUsuario, int idPolleria)
        {
            if (nuevoEstado == "COMPLETADA")
            {
                return CompletarCompraConSP(idCompra, idPolleria, idUsuario);
            }
            else if (nuevoEstado == "ANULADA")
            {
                return AnularCompraConSP(idCompra, "Cambio de estado por usuario", idUsuario);
            }
            else
            {
                CMRespuesta respuesta = new CMRespuesta();
                try
                {
                    using (SqlConnection cn = new SqlConnection(Conexion.CN))
                    {
                        string query = "UPDATE COMPRA SET Estado = @Estado WHERE id_compra = @id";
                        SqlCommand cmd = new SqlCommand(query, cn);
                        cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
                        cmd.Parameters.AddWithValue("@id", idCompra);

                        cn.Open();
                        int filas = cmd.ExecuteNonQuery();

                        respuesta.Resultado = filas > 0;
                        respuesta.Mensaje = filas > 0 ? "Estado de compra actualizado exitosamente" : "No se encontró la compra";
                    }
                }
                catch (Exception ex)
                {
                    respuesta.Resultado = false;
                    respuesta.Mensaje = "Error al cambiar estado: " + ex.Message;
                    Debug.WriteLine($"ERROR en CambiarEstadoCompra: {ex.Message}");
                }

                return respuesta;
            }
        }

        public List<CMTipoPago> ListarTiposPago()
        {
            List<CMTipoPago> lista = new List<CMTipoPago>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = "SELECT id_tipoPago, nombre, descripcion, Estado FROM TIPO_PAGO WHERE Estado = 1 ORDER BY id_tipoPago ASC";
                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.CommandType = CommandType.Text;

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMTipoPago
                            {
                                id_tipoPago = Convert.ToInt32(dr["id_tipoPago"]),
                                nombre = dr["nombre"].ToString(),
                                descripcion = dr["descripcion"] != DBNull.Value ? dr["descripcion"].ToString() : "",
                                Estado = Convert.ToBoolean(dr["Estado"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR en ListarTiposPago: {ex.Message}");
            }

            return lista;
        }

        public class CMCompraDetalles
        {
            public CMCabeceraCompra Cabecera { get; set; }
            public List<CMDetalleCompraConsulta> DetallesProductos { get; set; }
            public List<CMDetalleCompraConsulta> DetallesInsumos { get; set; }

            public CMCompraDetalles()
            {
                Cabecera = new CMCabeceraCompra();
                DetallesProductos = new List<CMDetalleCompraConsulta>();
                DetallesInsumos = new List<CMDetalleCompraConsulta>();
            }
        }

        public class CMCabeceraCompra
        {
            public int id_compra { get; set; }
            public string Referencia { get; set; }
            public decimal MontoTotal { get; set; }
            public DateTime FechaCompra { get; set; }
            public string Estado { get; set; }
            public string Usuario { get; set; }
            public string Proveedor { get; set; }
            public string ProveedorCI { get; set; }
            public string ProveedorTelefono { get; set; }
        }

        public class CMDetalleCompraConsulta
        {
            public string Tipo { get; set; }
            public decimal cantidad { get; set; }
            public decimal precioUnitario { get; set; }
            public decimal subtotal { get; set; }
            public string ItemNombre { get; set; }
            public string ItemDescripcion { get; set; }
            public string Medida { get; set; }
            public string AbreviaturaMedida { get; set; }
        }
    }
}