using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class CDStock
    {
        #region CONSULTAS DE STOCK

        public List<CMStock> ListarStockProductos(int? id_polleria = null)
        {
            List<CMStock> lista = new List<CMStock>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ObtenerStockProductos", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (id_polleria.HasValue)
                        cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    else
                        cmd.Parameters.AddWithValue("@id_polleria", DBNull.Value);

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMStock
                            {
                                id_stock = Convert.ToInt32(dr["id_stock"]),
                                stock_actual = Convert.ToDecimal(dr["stock_actual"]),
                                stock_minimo = Convert.ToDecimal(dr["stock_minimo"]),
                                Fecha_ultimo_movimiento = Convert.ToDateTime(dr["Fecha_ultimo_movimiento"]),
                                PRODUCTO_id_producto = Convert.ToInt32(dr["id_producto"]),
                                POLLERIA_id_polleria = Convert.ToInt32(dr["id_polleria"]),
                                producto_nombre = dr["producto_nombre"].ToString(),
                                producto_descripcion = dr["Descripcion"] != DBNull.Value ? dr["Descripcion"].ToString() : "",
                                precio_venta = Convert.ToDecimal(dr["PrecioVenta"]),
                                tipo_producto = dr["tipo_producto"].ToString(),
                                medida = dr["medida"].ToString(),
                                abreviatura = dr["Abreviatura"].ToString(),
                                polleria_nombre = dr["polleria_nombre"].ToString(),
                                estado_stock = dr["estado_stock"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMStock>();
                // Log.Error(ex, "Error en ListarStockProductos");
            }

            return lista;
        }

        public List<CMStockInsumo> ListarStockInsumos(int? id_polleria = null)
        {
            List<CMStockInsumo> lista = new List<CMStockInsumo>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ObtenerStockInsumos", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (id_polleria.HasValue)
                        cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    else
                        cmd.Parameters.AddWithValue("@id_polleria", DBNull.Value);

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMStockInsumo
                            {
                                id_stock_insumo = Convert.ToInt32(dr["id_stock_insumo"]),
                                stock_actual = Convert.ToDecimal(dr["stock_actual"]),
                                stock_minimo = Convert.ToDecimal(dr["stock_minimo"]),
                                Fecha_ultimo_movimiento = Convert.ToDateTime(dr["Fecha_ultimo_movimiento"]),
                                INSUMO_id_insumo = Convert.ToInt32(dr["id_insumo"]),
                                POLLERIA_id_polleria = Convert.ToInt32(dr["id_polleria"]),
                                insumo_nombre = dr["insumo_nombre"].ToString(),
                                insumo_descripcion = dr["descripcion"] != DBNull.Value ? dr["descripcion"].ToString() : "",
                                medida = dr["medida"].ToString(),
                                abreviatura = dr["Abreviatura"].ToString(),
                                polleria_nombre = dr["polleria_nombre"].ToString(),
                                estado_stock = dr["estado_stock"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMStockInsumo>();
            }

            return lista;
        }

        public List<CMStock> ObtenerProductosStockBajo(int? id_polleria = null)
        {
            List<CMStock> lista = new List<CMStock>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ObtenerProductosStockBajo", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (id_polleria.HasValue)
                        cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    else
                        cmd.Parameters.AddWithValue("@id_polleria", DBNull.Value);

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMStock
                            {
                                id_stock = Convert.ToInt32(dr["id_stock"]),
                                stock_actual = Convert.ToDecimal(dr["stock_actual"]),
                                stock_minimo = Convert.ToDecimal(dr["stock_minimo"]),
                                PRODUCTO_id_producto = Convert.ToInt32(dr["id_producto"]),
                                POLLERIA_id_polleria = Convert.ToInt32(dr["id_polleria"]),
                                producto_nombre = dr["producto_nombre"].ToString(),
                                polleria_nombre = dr["polleria_nombre"].ToString(),
                                precio_venta = dr["PrecioVenta"] != DBNull.Value ? Convert.ToDecimal(dr["PrecioVenta"]) : 0,
                                medida = dr["medida"] != DBNull.Value ? dr["medida"].ToString() : "",
                                estado_stock = dr["estado_stock"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMStock>();
            }

            return lista;
        }

        public List<CMStockInsumo> ObtenerInsumosStockBajo(int? id_polleria = null)
        {
            List<CMStockInsumo> lista = new List<CMStockInsumo>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ObtenerInsumosStockBajo", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (id_polleria.HasValue)
                        cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    else
                        cmd.Parameters.AddWithValue("@id_polleria", DBNull.Value);

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMStockInsumo
                            {
                                id_stock_insumo = Convert.ToInt32(dr["id_stock_insumo"]),
                                stock_actual = Convert.ToDecimal(dr["stock_actual"]),
                                stock_minimo = Convert.ToDecimal(dr["stock_minimo"]),
                                INSUMO_id_insumo = Convert.ToInt32(dr["id_insumo"]),
                                POLLERIA_id_polleria = Convert.ToInt32(dr["id_polleria"]),
                                insumo_nombre = dr["insumo_nombre"].ToString(),
                                insumo_descripcion = dr["descripcion"] != DBNull.Value ? dr["descripcion"].ToString() : "",
                                medida = dr["medida"] != DBNull.Value ? dr["medida"].ToString() : "",
                                abreviatura = dr["Abreviatura"] != DBNull.Value ? dr["Abreviatura"].ToString() : "",
                                polleria_nombre = dr["polleria_nombre"].ToString(),
                                estado_stock = dr["estado_stock"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMStockInsumo>();
            }

            return lista;
        }

        #endregion

        #region AJUSTES DE STOCK

        public bool AjustarStockProducto(CMStockAjuste ajuste, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;
            int id_movimiento = 0;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_AjustarStockProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_producto", ajuste.id_item);
                    cmd.Parameters.AddWithValue("@id_polleria", ajuste.id_polleria);
                    cmd.Parameters.AddWithValue("@nuevo_stock", ajuste.nuevo_stock);
                    cmd.Parameters.AddWithValue("@id_usuario", ajuste.id_usuario);
                    cmd.Parameters.AddWithValue("@observaciones",
                        string.IsNullOrEmpty(ajuste.observaciones) ? (object)DBNull.Value : ajuste.observaciones);

                    SqlParameter paramIdMovimiento = new SqlParameter("@id_movimiento_resultado", SqlDbType.Int);
                    paramIdMovimiento.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramIdMovimiento);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    id_movimiento = Convert.ToInt32(paramIdMovimiento.Value);
                    resultado = true;
                    Mensaje = "Ajuste de stock realizado correctamente. ID Movimiento: " + id_movimiento;
                }
            }
            catch (SqlException sqlEx)
            {
                Mensaje = $"Error de base de datos: {sqlEx.Message}";
                resultado = false;
            }
            catch (Exception ex)
            {
                Mensaje = $"Error: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        public bool AjustarStockInsumo(CMStockAjuste ajuste, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;
            int id_movimiento = 0;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_AjustarStockInsumo", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_insumo", ajuste.id_item);
                    cmd.Parameters.AddWithValue("@id_polleria", ajuste.id_polleria);
                    cmd.Parameters.AddWithValue("@nuevo_stock", ajuste.nuevo_stock);
                    cmd.Parameters.AddWithValue("@id_usuario", ajuste.id_usuario);
                    cmd.Parameters.AddWithValue("@observaciones",
                        string.IsNullOrEmpty(ajuste.observaciones) ? (object)DBNull.Value : ajuste.observaciones);

                    SqlParameter paramIdMovimiento = new SqlParameter("@id_movimiento_resultado", SqlDbType.Int);
                    paramIdMovimiento.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramIdMovimiento);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    id_movimiento = Convert.ToInt32(paramIdMovimiento.Value);
                    resultado = true;
                    Mensaje = "Ajuste de stock realizado correctamente. ID Movimiento: " + id_movimiento;
                }
            }
            catch (Exception ex)
            {
                Mensaje = $"Error: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        #endregion

        #region HISTORIAL Y REPORTES

        public List<CMMovimientoStock> ObtenerHistorialMovimientos(
            int? id_polleria = null,
            DateTime? fecha_desde = null,
            DateTime? fecha_hasta = null,
            string tipo_item = null,
            string tipo_movimiento = null,
            int? id_item = null)
        {
            List<CMMovimientoStock> lista = new List<CMMovimientoStock>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ObtenerHistorialMovimientos", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    AddNullableParameter(cmd, "@id_polleria", id_polleria);
                    AddNullableParameter(cmd, "@fecha_desde", fecha_desde);
                    AddNullableParameter(cmd, "@fecha_hasta", fecha_hasta);
                    AddNullableParameter(cmd, "@tipo_item", tipo_item);
                    AddNullableParameter(cmd, "@tipo_movimiento", tipo_movimiento);
                    AddNullableParameter(cmd, "@id_item", id_item);

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMMovimientoStock
                            {
                                id_movimiento = Convert.ToInt32(dr["id_movimiento"]),
                                fecha_movimiento = Convert.ToDateTime(dr["fecha_movimiento"]),
                                tipo_movimiento = dr["tipo_movimiento"].ToString(),
                                tipo_referencia = dr["tipo_referencia"] != DBNull.Value ? dr["tipo_referencia"].ToString() : "",
                                id_referencia = dr["id_referencia"] != DBNull.Value ? Convert.ToInt32(dr["id_referencia"]) : 0,
                                id_item = Convert.ToInt32(dr["id_item"]),
                                tipo_item = dr["tipo_item"].ToString(),
                                cantidad = Convert.ToDecimal(dr["cantidad"]),
                                stock_anterior = Convert.ToDecimal(dr["stock_anterior"]),
                                stock_nuevo = Convert.ToDecimal(dr["stock_nuevo"]),
                                observaciones = dr["observaciones"] != DBNull.Value ? dr["observaciones"].ToString() : "",
                                id_polleria = Convert.ToInt32(dr["id_polleria"]),
                                id_usuario = Convert.ToInt32(dr["id_usuario"]),
                                fecha_registro = Convert.ToDateTime(dr["fecha_movimiento"]),
                                nombre_item = dr["item_nombre"] != DBNull.Value
                ? dr["item_nombre"].ToString()
                : "Desconocido",

                                nombre_polleria = dr["polleria_nombre"] != DBNull.Value
                ? dr["polleria_nombre"].ToString()
                : "",

                                nombre_usuario = dr["usuario_nombre"] != DBNull.Value
                ? dr["usuario_nombre"].ToString()
                : ""

                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMMovimientoStock>();
            }

            return lista;
        }

        public List<CMMovimientoStock> ObtenerKardexProducto(int id_producto, int id_polleria,
                                                              DateTime? fecha_desde = null, DateTime? fecha_hasta = null)
        {
            List<CMMovimientoStock> lista = new List<CMMovimientoStock>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ObtenerKardexProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_producto", id_producto);
                    cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    AddNullableParameter(cmd, "@fecha_desde", fecha_desde);
                    AddNullableParameter(cmd, "@fecha_hasta", fecha_hasta);

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMMovimientoStock
                            {
                                fecha_movimiento = Convert.ToDateTime(dr["fecha_movimiento"]),
                                tipo_movimiento = dr["tipo_movimiento"].ToString(),
                                tipo_referencia = dr["tipo_referencia"] != DBNull.Value ? dr["tipo_referencia"].ToString() : "",
                                id_referencia = dr["id_referencia"] != DBNull.Value ? Convert.ToInt32(dr["id_referencia"]) : 0,
                                cantidad = Convert.ToDecimal(dr["cantidad"]),
                                stock_anterior = Convert.ToDecimal(dr["stock_anterior"]),
                                stock_nuevo = Convert.ToDecimal(dr["stock_nuevo"]),
                                observaciones = dr["observaciones"] != DBNull.Value ? dr["observaciones"].ToString() : "",
                                nombre_item = dr["producto_nombre"] != DBNull.Value
                ? dr["producto_nombre"].ToString()
                : "Desconocido",

                                nombre_polleria = dr["polleria_nombre"] != DBNull.Value
                ? dr["polleria_nombre"].ToString()
                : "",

                                nombre_usuario = dr["usuario_nombre"] != DBNull.Value
                ? dr["usuario_nombre"].ToString()
                : ""

                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMMovimientoStock>();
            }

            return lista;
        }

        #endregion

        #region CONFIGURACIÓN DE STOCK

        public bool ActualizarStockMinimoProducto(int id_producto, int id_polleria,
                                                  decimal nuevo_stock_minimo, int id_usuario, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ActualizarStockMinimoProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_producto", id_producto);
                    cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    cmd.Parameters.AddWithValue("@nuevo_stock_minimo", nuevo_stock_minimo);
                    cmd.Parameters.AddWithValue("@id_usuario", id_usuario);

                    SqlParameter paramResultado = new SqlParameter("@Resultado", SqlDbType.Bit);
                    paramResultado.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramResultado);

                    SqlParameter paramMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500);
                    paramMensaje.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramMensaje);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(paramResultado.Value);
                    Mensaje = paramMensaje.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                Mensaje = $"Error: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        public bool ActualizarStockMinimoInsumo(int id_insumo, int id_polleria,
                                                decimal nuevo_stock_minimo, int id_usuario, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ActualizarStockMinimoInsumo", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_insumo", id_insumo);
                    cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    cmd.Parameters.AddWithValue("@nuevo_stock_minimo", nuevo_stock_minimo);
                    cmd.Parameters.AddWithValue("@id_usuario", id_usuario);

                    SqlParameter paramResultado = new SqlParameter("@Resultado", SqlDbType.Bit);
                    paramResultado.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramResultado);

                    SqlParameter paramMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500);
                    paramMensaje.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramMensaje);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(paramResultado.Value);
                    Mensaje = paramMensaje.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                Mensaje = $"Error: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        public bool TransferirStockEntrePollerias(CMStockTransferVenta transferencia, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_TransferirStockEntrePollerias", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_producto", transferencia.id_producto);
                    cmd.Parameters.AddWithValue("@id_polleria_origen", transferencia.id_polleria_origen);
                    cmd.Parameters.AddWithValue("@id_polleria_destino", transferencia.id_polleria_destino);
                    cmd.Parameters.AddWithValue("@cantidad", transferencia.cantidad);
                    cmd.Parameters.AddWithValue("@id_usuario", transferencia.id_usuario);
                    cmd.Parameters.AddWithValue("@observaciones",
                        string.IsNullOrEmpty(transferencia.observaciones) ? (object)DBNull.Value : transferencia.observaciones);

                    SqlParameter paramResultado = new SqlParameter("@Resultado", SqlDbType.Bit);
                    paramResultado.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramResultado);

                    SqlParameter paramMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500);
                    paramMensaje.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramMensaje);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(paramResultado.Value);
                    Mensaje = paramMensaje.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                Mensaje = $"Error: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        #endregion

        #region MÉTODOS AUXILIARES

        public decimal ObtenerStockDisponibleProducto(int id_producto, int id_polleria)
        {
            decimal stock = 0;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                        SELECT ISNULL(stock_actual, 0) as stock 
                        FROM STOCK 
                        WHERE PRODUCTO_id_producto = @id_producto 
                        AND POLLERIA_id_polleria = @id_polleria";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@id_producto", id_producto);
                    cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    cmd.CommandType = CommandType.Text;

                    cn.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                        stock = Convert.ToDecimal(result);
                }
            }
            catch (Exception)
            {
                stock = 0;
            }

            return stock;
        }

        public decimal ObtenerStockDisponibleInsumo(int id_insumo, int id_polleria)
        {
            decimal stock = 0;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                        SELECT ISNULL(stock_actual, 0) as stock 
                        FROM STOCK_INSUMO 
                        WHERE INSUMO_id_insumo = @id_insumo 
                        AND POLLERIA_id_polleria = @id_polleria";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@id_insumo", id_insumo);
                    cmd.Parameters.AddWithValue("@id_polleria", id_polleria);
                    cmd.CommandType = CommandType.Text;

                    cn.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                        stock = Convert.ToDecimal(result);
                }
            }
            catch (Exception)
            {
                stock = 0;
            }

            return stock;
        }

        public bool ValidarStockParaVenta(int id_producto, int id_polleria, decimal cantidad, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                decimal stockDisponible = ObtenerStockDisponibleProducto(id_producto, id_polleria);

                if (stockDisponible >= cantidad)
                {
                    resultado = true;
                    Mensaje = "Stock suficiente";
                }
                else
                {
                    Mensaje = $"Stock insuficiente. Disponible: {stockDisponible}, Requerido: {cantidad}";
                }
            }
            catch (Exception ex)
            {
                Mensaje = $"Error al validar stock: {ex.Message}";
                resultado = false;
            }

            return resultado;
        }

        public CMStock ObtenerStockProductoPorId(int id_stock)
        {   
            CMStock stock = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                        SELECT s.id_stock, s.stock_actual, s.stock_minimo, s.Fecha_ultimo_movimiento,
                               s.PRODUCTO_id_producto, s.POLLERIA_id_polleria,
                               p.Nombre AS producto_nombre, p.Descripcion AS producto_descripcion,
                               p.PrecioVenta, pl.Nombre AS polleria_nombre
                        FROM STOCK s
                        INNER JOIN PRODUCTO p ON s.PRODUCTO_id_producto = p.id_producto
                        INNER JOIN POLLERIA pl ON s.POLLERIA_id_polleria = pl.id_polleria
                        WHERE s.id_stock = @id_stock";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@id_stock", id_stock);
                    cmd.CommandType = CommandType.Text;

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            stock = new CMStock
                            {
                                id_stock = Convert.ToInt32(dr["id_stock"]),
                                stock_actual = Convert.ToDecimal(dr["stock_actual"]),
                                stock_minimo = Convert.ToDecimal(dr["stock_minimo"]),
                                Fecha_ultimo_movimiento = Convert.ToDateTime(dr["Fecha_ultimo_movimiento"]),
                                PRODUCTO_id_producto = Convert.ToInt32(dr["PRODUCTO_id_producto"]),
                                POLLERIA_id_polleria = Convert.ToInt32(dr["POLLERIA_id_polleria"]),
                                producto_nombre = dr["producto_nombre"].ToString(),
                                producto_descripcion = dr["producto_descripcion"].ToString(),
                                precio_venta = Convert.ToDecimal(dr["PrecioVenta"]),
                                polleria_nombre = dr["polleria_nombre"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception)
            {
                stock = null;
            }

            return stock;
        }

        private void AddNullableParameter(SqlCommand cmd, string parameterName, object value)
        {
            if (value is string texto && string.IsNullOrWhiteSpace(texto))
                value = null;

            if (value != null)
                cmd.Parameters.AddWithValue(parameterName, value);
            else
                cmd.Parameters.AddWithValue(parameterName, DBNull.Value);
        }

        #endregion
    }
}
