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
    public class CDInsumo
    {
        public List<CMInsumo> ListarInsumos()
        {
            List<CMInsumo> lista = new List<CMInsumo>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_ListarInsumos", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMInsumo
                            {
                                id_insumo = Convert.ToInt32(dr["id_insumo"]),
                                nombre = dr["nombre"].ToString(),
                                descripcion = dr["descripcion"].ToString(),
                                estado = Convert.ToBoolean(dr["estado"]),
                                fecha_registro = Convert.ToDateTime(dr["fecha_registro"]),
                                MEDIDAS_id_medida = Convert.ToInt32(dr["MEDIDAS_id_medida"]),
                                NombreMedida = dr["NombreMedida"].ToString(),
                                Abreviatura = dr["Abreviatura"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lista = new List<CMInsumo>();
            }

            return lista;
        }

        public bool RegistrarInsumo(CMInsumo obj, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_RegistrarInsumo", cn);
                    cmd.Parameters.AddWithValue("@Nombre", obj.nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", obj.descripcion);
                    cmd.Parameters.AddWithValue("@IdMedida", obj.MEDIDAS_id_medida);
                    cmd.Parameters.AddWithValue("@Estado", obj.estado);
                    cmd.Parameters.Add("Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.CommandType = CommandType.StoredProcedure;

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                resultado = false;
                Mensaje = ex.Message;
            }

            return resultado;
        }
        public bool EditarInsumo(CMInsumo obj, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_EditarInsumo", cn);
                    cmd.Parameters.AddWithValue("@IdInsumo", obj.id_insumo);
                    cmd.Parameters.AddWithValue("@Nombre", obj.nombre);
                    cmd.Parameters.AddWithValue("@Descripcion", obj.descripcion);
                    cmd.Parameters.AddWithValue("@IdMedida", obj.MEDIDAS_id_medida);
                    cmd.Parameters.AddWithValue("@Estado", obj.estado);
                    cmd.Parameters.Add("Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.CommandType = CommandType.StoredProcedure;

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                resultado = false;
                Mensaje = ex.Message;
            }

            return resultado;
        }
        public bool EliminarInsumo(int id, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("sp_EliminarInsumo", cn);
                    cmd.Parameters.AddWithValue("@IdInsumo", id);
                    cmd.Parameters.Add("Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;
                    cmd.CommandType = CommandType.StoredProcedure;

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(cmd.Parameters["Resultado"].Value);
                    Mensaje = cmd.Parameters["Mensaje"].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                resultado = false;
                Mensaje = ex.Message;
            }

            return resultado;
        }
        public CMInsumo ObtenerInsumoPorId(int id)
        {
            CMInsumo insumo = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                        SELECT i.id_insumo, i.nombre, i.descripcion, i.estado, i.fecha_registro, 
                               i.MEDIDAS_id_medida, m.Descripcion AS NombreMedida, m.Abreviatura
                        FROM INSUMO i
                        INNER JOIN MEDIDAS m ON i.MEDIDAS_id_medida = m.id_medida
                        WHERE i.id_insumo = @id";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.CommandType = CommandType.Text;

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            insumo = new CMInsumo
                            {
                                id_insumo = Convert.ToInt32(dr["id_insumo"]),
                                nombre = dr["nombre"].ToString(),
                                descripcion = dr["descripcion"].ToString(),
                                estado = Convert.ToBoolean(dr["estado"]),
                                fecha_registro = Convert.ToDateTime(dr["fecha_registro"]),
                                MEDIDAS_id_medida = Convert.ToInt32(dr["MEDIDAS_id_medida"]),
                                NombreMedida = dr["NombreMedida"].ToString(),
                                Abreviatura = dr["Abreviatura"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception)
            {
                insumo = null;
            }

            return insumo;
        }
    }
}