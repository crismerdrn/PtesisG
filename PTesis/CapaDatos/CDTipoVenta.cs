using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaModelo;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class CDTipoVenta
    {
        public List<CMTipoVenta> ListarTipoVentas()
        {
            List<CMTipoVenta> lista = new List<CMTipoVenta>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("LISTAR_TipoVenta", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new CMTipoVenta
                    {
                        id_tipoVenta = Convert.ToInt32(dr["id_tipoVenta"]),
                        nombre = dr["nombre"].ToString(),
                        descripcion = dr["descripcion"] != DBNull.Value ? dr["descripcion"].ToString() : "",
                        estado = dr["estado"] != DBNull.Value ? Convert.ToBoolean(dr["estado"]) : true
                    });
                }
            }
            return lista;
        }

        public bool AgregarTipoVenta(CMTipoVenta tipoVenta)
        {
            bool resultado = false;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("ADICIONAR_TipoVenta", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@nombre", tipoVenta.nombre);
                cmd.Parameters.AddWithValue("@descripcion", string.IsNullOrEmpty(tipoVenta.descripcion) ? (object)DBNull.Value : tipoVenta.descripcion);
                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool ModificarTipoVenta(CMTipoVenta tipoVenta)
        {
            bool resultado = false;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("MODIFICAR_TipoVenta", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_tipoVenta", tipoVenta.id_tipoVenta);
                cmd.Parameters.AddWithValue("@nombre", tipoVenta.nombre);
                cmd.Parameters.AddWithValue("@descripcion", string.IsNullOrEmpty(tipoVenta.descripcion) ? (object)DBNull.Value : tipoVenta.descripcion);
                cmd.Parameters.AddWithValue("@estado", tipoVenta.estado); // Envía el estado (true/false)
                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool EliminarTipoVenta(int id, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("ELIMINAR_TipoVenta", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_tipoVenta", id);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;

                    if (!resultado)
                    {
                        mensaje = "No se encontró el tipo de venta para eliminar.";
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547) 
                    {
                        mensaje = "No se puede eliminar este tipo de venta porque ya está asociado a ventas registradas.";
                    }
                    else
                    {
                        mensaje = "Error en la base de datos: " + ex.Message;
                    }
                    resultado = false;
                }
                catch (Exception ex)
                {
                    mensaje = "Error inesperado: " + ex.Message;
                    resultado = false;
                }
            }

            return resultado;
        }

        public CMTipoVenta ObtenerTipoVentaPorId(int id)
        {
            CMTipoVenta tipoVenta = null;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("OBTENER_TipoVenta_PorId", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_tipoVenta", id);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    tipoVenta = new CMTipoVenta
                    {
                        id_tipoVenta = Convert.ToInt32(dr["id_tipoVenta"]),
                        nombre = dr["nombre"].ToString(),
                        descripcion = dr["descripcion"] != DBNull.Value ? dr["descripcion"].ToString() : "",
                        estado = dr["estado"] != DBNull.Value ? Convert.ToBoolean(dr["estado"]) : true
                    };
                }
            }
            return tipoVenta;
        }
    }
}