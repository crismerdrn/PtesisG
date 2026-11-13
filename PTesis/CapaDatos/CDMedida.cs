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
    public class CDMedida
    {
        private CMMedida CrearMedidaDesdeLector(SqlDataReader dr)
        {
            return new CMMedida
            {
                id_medida = Convert.ToInt32(dr["id_medida"]),
                Descripcion = dr["Descripcion"].ToString(),
                Abreviatura = dr["Abreviatura"].ToString(), 
                Estado = Convert.ToBoolean(dr["Estado"]),
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
            };
        }

        public List<CMMedida> ListarMedidas()
        {
            List<CMMedida> lista = new List<CMMedida>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("LISTAR_Medidas", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(CrearMedidaDesdeLector(dr));
                }
            }
            return lista;
        }

        public bool AgregarMedida(CMMedida medida)
        {
            bool resultado = false;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("ADICIONAR_Medida", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Descripcion", medida.Descripcion);
                cmd.Parameters.AddWithValue("@Abreviatura", medida.Abreviatura); 
                cmd.Parameters.AddWithValue("@Estado", medida.Estado);

                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool ModificarMedida(CMMedida medida)
        {
            bool resultado = false;
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("MODIFICAR_Medida", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id_medida", medida.id_medida);
                cmd.Parameters.AddWithValue("@Descripcion", medida.Descripcion);
                cmd.Parameters.AddWithValue("@Abreviatura", medida.Abreviatura); 
                cmd.Parameters.AddWithValue("@Estado", medida.Estado);

                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool EliminarMedida(int id, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("ELIMINAR_Medida", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_medida", id);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;

                    if (!resultado)
                    {
                        mensaje = "No se encontró la medida para eliminar.";
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547) 
                    {
                        mensaje = "No se puede eliminar esta medida porque ya está asociada a productos registrados.";
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

        public CMMedida ObtenerMedidaPorId(int id)
        {
            CMMedida medida = null;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("OBTENER_Medida_PorId", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_medida", id);

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    medida = CrearMedidaDesdeLector(dr);
                }
            }
            return medida;
        }

        public List<CMMedida> ListarMedidasActivas()
        {
            List<CMMedida> lista = new List<CMMedida>();

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("LISTAR_Medidas_Activas", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(CrearMedidaDesdeLector(dr));
                }
            }
            return lista;
        }

        public bool CambiarEstadoMedida(int id, bool estado)
        {
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("CAMBIAR_ESTADO_Medida", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id_medida", id);
                cmd.Parameters.AddWithValue("@Estado", estado);

                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }

            return resultado;
        }
    }
}