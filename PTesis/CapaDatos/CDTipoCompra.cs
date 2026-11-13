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
    public class CDTipoCompra
    {
        public List<CMTipoCompra> ListarTipoCompra()
        {
            List<CMTipoCompra> lista = new List<CMTipoCompra>();

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("LISTAR_TipoCompra", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new CMTipoCompra
                    {
                        id_tipoCompra = Convert.ToInt32(dr["id_tipoCompra"]),
                        Nombre = dr["Nombre"].ToString(),
                        Descripcion = dr["Descripcion"].ToString(),
                        CategoriaBase = dr["CategoriaBase"].ToString(),
                        Estado = Convert.ToBoolean(dr["Estado"]),
                        FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                    });
                }
            }

            return lista;
        }

        public bool AgregarTipoCompra(CMTipoCompra tipoCompra)
        {
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("ADICIONAR_TipoCompra", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Nombre", tipoCompra.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", tipoCompra.Descripcion);
                cmd.Parameters.AddWithValue("@CategoriaBase", tipoCompra.CategoriaBase);
                cmd.Parameters.AddWithValue("@Estado", tipoCompra.Estado);

                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }

            return resultado;
        }

        public bool ModificarTipoCompra(CMTipoCompra tipoCompra)
        {
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("MODIFICAR_TipoCompra", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id_tipoCompra", tipoCompra.id_tipoCompra);
                cmd.Parameters.AddWithValue("@Nombre", tipoCompra.Nombre);
                cmd.Parameters.AddWithValue("@Descripcion", tipoCompra.Descripcion);
                cmd.Parameters.AddWithValue("@CategoriaBase", tipoCompra.CategoriaBase);
                cmd.Parameters.AddWithValue("@Estado", tipoCompra.Estado);

                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }

            return resultado;
        }

        public bool EliminarTipoCompra(int id, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("ELIMINAR_TipoCompra", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_tipoCompra", id);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                    if (!resultado)
                    {
                        mensaje = "No se encontró el registro para eliminar.";
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547) // Código de error de Foreign Key en SQL Server
                    {
                        mensaje = "No se puede eliminar este tipo de compra porque ya está asociado a compras registradas.";
                    }
                    else
                    {
                        mensaje = "Ocurrió un error en la base de datos: " + ex.Message;
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

        public CMTipoCompra ObtenerTipoCompraPorId(int id)
        {
            CMTipoCompra tipo = null;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("OBTENER_TipoCompra_PorId", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_tipoCompra", id);

                cn.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    tipo = new CMTipoCompra
                    {
                        id_tipoCompra = Convert.ToInt32(dr["id_tipoCompra"]),
                        Nombre = dr["Nombre"].ToString(),
                        Descripcion = dr["Descripcion"].ToString(),
                        CategoriaBase = dr["CategoriaBase"].ToString(),
                        Estado = Convert.ToBoolean(dr["Estado"]),
                        FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                    };
                }
            }

            return tipo;
        }
    }
}
