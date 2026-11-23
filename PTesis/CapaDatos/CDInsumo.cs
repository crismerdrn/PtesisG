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

        public List<CMPresentacionInsumo> ListarPresentaciones(int idInsumo, bool soloActivas)
        {
            var lista = new List<CMPresentacionInsumo>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                        SELECT pi.id_presentacion, pi.INSUMO_id_insumo, pi.nombre, pi.factor, pi.estado, pi.fecha_registro,
                               CASE WHEN OBJECT_ID('dbo.PRESENTACION_COMPONENTE') IS NULL THEN NULL ELSE
                                   STUFF((SELECT ', ' + CAST(CAST(pc.factor AS FLOAT) AS VARCHAR(20)) + ' ' + i.nombre
                                          FROM PRESENTACION_COMPONENTE pc
                                          INNER JOIN INSUMO i ON i.id_insumo = pc.INSUMO_id_insumo
                                          WHERE pc.PRESENTACION_id_presentacion = pi.id_presentacion
                                          ORDER BY i.nombre
                                          FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') END AS componentes
                        FROM PRESENTACION_INSUMO pi
                        WHERE pi.INSUMO_id_insumo = @idInsumo AND (@soloActivas = 0 OR pi.estado = 1)
                        ORDER BY pi.nombre";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@idInsumo", idInsumo);
                    cmd.Parameters.AddWithValue("@soloActivas", soloActivas);
                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMPresentacionInsumo
                            {
                                id_presentacion = Convert.ToInt32(dr["id_presentacion"]),
                                INSUMO_id_insumo = Convert.ToInt32(dr["INSUMO_id_insumo"]),
                                nombre = dr["nombre"].ToString(),
                                factor = Convert.ToDecimal(dr["factor"]),
                                componentes = dr["componentes"] == DBNull.Value ? null : dr["componentes"].ToString(),
                                estado = Convert.ToBoolean(dr["estado"]),
                                fecha_registro = Convert.ToDateTime(dr["fecha_registro"])
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                lista = new List<CMPresentacionInsumo>();
            }

            return lista;
        }

        public bool RegistrarPresentacion(CMPresentacionInsumo obj, out string Mensaje)
        {
            Mensaje = string.Empty;
            string nombre = (obj.nombre ?? "").Trim();

            if (nombre.Length == 0 || nombre.Length > 100)
            {
                Mensaje = "Ingrese el nombre de la presentación (máximo 100 caracteres).";
                return false;
            }
            if (obj.factor <= 0)
            {
                Mensaje = "La cantidad que trae la presentación debe ser mayor a 0.";
                return false;
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                        IF EXISTS (SELECT 1 FROM PRESENTACION_INSUMO WHERE INSUMO_id_insumo = @idInsumo AND nombre = @nombre)
                            UPDATE PRESENTACION_INSUMO SET factor = @factor, estado = 1
                            WHERE INSUMO_id_insumo = @idInsumo AND nombre = @nombre;
                        ELSE
                            INSERT INTO PRESENTACION_INSUMO (INSUMO_id_insumo, nombre, factor) VALUES (@idInsumo, @nombre, @factor);";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@idInsumo", obj.INSUMO_id_insumo);
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.Parameters.Add("@factor", SqlDbType.Decimal).Value = obj.factor;
                    cmd.Parameters["@factor"].Precision = 12;
                    cmd.Parameters["@factor"].Scale = 4;
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }

                Mensaje = "Presentación guardada correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Mensaje = "Error al guardar la presentación: " + ex.Message;
                return false;
            }
        }

        public bool CambiarEstadoPresentacion(int idPresentacion, bool estado, out string Mensaje)
        {
            Mensaje = string.Empty;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE PRESENTACION_INSUMO SET estado = @estado WHERE id_presentacion = @id", cn);
                    cmd.Parameters.AddWithValue("@estado", estado);
                    cmd.Parameters.AddWithValue("@id", idPresentacion);
                    cn.Open();
                    bool ok = cmd.ExecuteNonQuery() > 0;
                    Mensaje = ok ? (estado ? "Presentación activada." : "Presentación desactivada.") : "Presentación no encontrada.";
                    return ok;
                }
            }
            catch (Exception ex)
            {
                Mensaje = "Error al cambiar el estado de la presentación: " + ex.Message;
                return false;
            }
        }
    }
}
