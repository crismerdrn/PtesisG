using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CapaDatos;

namespace CapaDatos
{
    public class CDProducto
    {
        private CMProducto CrearProductoDesdeLector(SqlDataReader dr, bool incluirFK_IDs = false)
        {
            var producto = new CMProducto
            {
                id_producto = Convert.ToInt32(dr["id_producto"]),
                Nombre = dr["Nombre"].ToString(),
                Descripcion = dr["Descripcion"].ToString(),
                PrecioVenta = Convert.ToDecimal(dr["PrecioVenta"]),
                Estado = Convert.ToBoolean(dr["Estado"]),
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"]),
                Medida = dr["Medida"].ToString(),
                AbreviaturaMedida = dr["AbreviaturaMedida"].ToString(),
                TipoProducto = dr["TipoProducto"].ToString()
            };

            if (dr.HasColumn("CantidadInsumos"))
            {
                producto.CantidadInsumos = Convert.ToInt32(dr["CantidadInsumos"]);
            }

            if (incluirFK_IDs)
            {
                producto.MEDIDAS_id_medida = Convert.ToInt32(dr["MEDIDAS_id_medida"]);
                producto.TIPO_PRODUCTO_id_tipoProducto = Convert.ToInt32(dr["TIPO_PRODUCTO_id_tipoProducto"]);
            }

            return producto;
        }

        public List<CMProducto> ListarProductos(int? idTipoProducto = null)
        {
            List<CMProducto> lista = new List<CMProducto>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Producto_Listar", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdTipoProducto", idTipoProducto ?? (object)DBNull.Value);
                cn.Open();

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(CrearProductoDesdeLector(dr));
                }
            }
            return lista;
        }

        public CMProducto ObtenerProductoPorId(int id)
        {
            Console.WriteLine($"CDProducto.ObtenerProductoPorId - Buscando ID: {id}");

            CMProducto producto = null;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Producto_ObtenerPorId", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdProducto", id);

                Console.WriteLine($"Ejecutando sp_Producto_ObtenerPorId con parámetro: {id}");

                cn.Open();

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    producto = CrearProductoDesdeLector(dr, true);
                    Console.WriteLine($"Producto encontrado: ID={producto.id_producto}, Nombre={producto.Nombre}");
                }
                else
                {
                    Console.WriteLine($"No se encontró producto con ID: {id}");
                }
            }
            return producto;
        }


        public bool RegistrarProducto(CMProducto producto, out int idGenerado, out string mensaje)
        {
            idGenerado = 0;
            mensaje = "";
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Producto_Registrar", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                // Parámetros de entrada
                cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
                cmd.Parameters.AddWithValue("@IdMedida", producto.MEDIDAS_id_medida);
                cmd.Parameters.AddWithValue("@IdTipoProducto", producto.TIPO_PRODUCTO_id_tipoProducto);

                // Parámetros de salida
                cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@IdGenerado", SqlDbType.Int).Direction = ParameterDirection.Output;

                cn.Open();
                cmd.ExecuteNonQuery();

                // Obtener valores de salida
                resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                mensaje = cmd.Parameters["@Mensaje"].Value.ToString();
                idGenerado = resultado ? Convert.ToInt32(cmd.Parameters["@IdGenerado"].Value) : 0;
            }

            return resultado;
        }

        public bool ActualizarProducto(CMProducto producto, out string mensaje)
        {
            mensaje = "";
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Producto_Actualizar", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                // Parámetros de entrada
                cmd.Parameters.AddWithValue("@IdProducto", producto.id_producto);
                cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
                cmd.Parameters.AddWithValue("@IdMedida", producto.MEDIDAS_id_medida);
                cmd.Parameters.AddWithValue("@IdTipoProducto", producto.TIPO_PRODUCTO_id_tipoProducto);
                cmd.Parameters.AddWithValue("@Estado", producto.Estado);

                // Parámetros de salida
                cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                cn.Open();
                cmd.ExecuteNonQuery();

                // Obtener valores de salida
                resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                mensaje = cmd.Parameters["@Mensaje"].Value.ToString();
            }

            return resultado;
        }

        public bool EliminarProducto(int id, out string mensaje)
        {
            mensaje = "";
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Producto_Eliminar", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdProducto", id);

                cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                mensaje = cmd.Parameters["@Mensaje"].Value.ToString();
            }

            return resultado;
        }

        public List<CMProducto> ListarProductosParaCompra(string term = null)
        {
            List<CMProducto> lista = new List<CMProducto>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Producto_ListarParaCompra", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Term", string.IsNullOrWhiteSpace(term) ? (object)DBNull.Value : term.Trim());

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(CrearProductoDesdeLector(dr));
                }
            }
            return lista;
        }
    }
}