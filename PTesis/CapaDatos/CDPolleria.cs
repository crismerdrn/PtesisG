using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace CapaDatos
{
    public class CDPolleria
    {
        private CMPolleria CrearDesdeLector(SqlDataReader dr)
        {
            var obj = new CMPolleria
            {
                id_polleria = Convert.ToInt32(dr["id_polleria"]),
                Nombre = dr["Nombre"].ToString(),
                Direccion = dr["Direccion"].ToString(),
                Telefono = dr["Telefono"].ToString(),
                Estado = Convert.ToBoolean(dr["Estado"]),
                FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
            };

            return obj;
        }

        public List<CMPolleria> Listar()
        {
            List<CMPolleria> lista = new List<CMPolleria>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                // Tu SP sp_Polleria_Listar no recibe parámetros
                SqlCommand cmd = new SqlCommand("sp_Polleria_Listar", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(CrearDesdeLector(dr));
                }
            }
            return lista;
        }

        public int Registrar(CMPolleria obj, out string Mensaje)
        {
            int idGenerado = 0;
            Mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_Polleria_Registrar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Nombre", obj.Nombre);
                    cmd.Parameters.AddWithValue("@Direccion", obj.Direccion);
                    cmd.Parameters.AddWithValue("@Telefono", obj.Telefono);
                    cmd.Parameters.AddWithValue("@Estado", obj.Estado);

                    SqlParameter resParam = new SqlParameter("@Resultado", SqlDbType.Bit)
                    { Direction = ParameterDirection.Output };
                    SqlParameter msgParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                    { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(resParam);
                    cmd.Parameters.Add(msgParam);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    bool resultado = Convert.ToBoolean(resParam.Value);
                    Mensaje = msgParam.Value.ToString();

                    if (resultado)
                    {
                        idGenerado = 1;
                    }
                }
                catch (Exception ex)
                {
                    idGenerado = 0;
                    Mensaje = ex.Message;
                }
            }
            return idGenerado;
        }

        public bool Editar(CMPolleria obj, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    // IMPORTANTE: El SP se llama sp_Polleria_Editar (no sp_Polleria_Modificar)
                    SqlCommand cmd = new SqlCommand("sp_Polleria_Editar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdPolleria", obj.id_polleria);
                    cmd.Parameters.AddWithValue("@Nombre", obj.Nombre);
                    cmd.Parameters.AddWithValue("@Direccion", obj.Direccion);
                    cmd.Parameters.AddWithValue("@Telefono", obj.Telefono);
                    cmd.Parameters.AddWithValue("@Estado", obj.Estado);

                    SqlParameter resParam = new SqlParameter("@Resultado", SqlDbType.Bit)
                    { Direction = ParameterDirection.Output };
                    SqlParameter msgParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                    { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(resParam);
                    cmd.Parameters.Add(msgParam);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(resParam.Value);
                    Mensaje = msgParam.Value.ToString();
                }
                catch (Exception ex)
                {
                    resultado = false;
                    Mensaje = ex.Message;
                }
            }
            return resultado;
        }

        public bool Eliminar(int id, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_Polleria_Eliminar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdPolleria", id);

                    SqlParameter resParam = new SqlParameter("@Resultado", SqlDbType.Bit)
                    { Direction = ParameterDirection.Output };
                    SqlParameter msgParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500)
                    { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(resParam);
                    cmd.Parameters.Add(msgParam);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(resParam.Value);
                    Mensaje = msgParam.Value.ToString();
                }
                catch (Exception ex)
                {
                    resultado = false;
                    Mensaje = ex.Message;
                }
            }
            return resultado;
        }

        public CMPolleria ObtenerPorId(int id)
        {
            var lista = Listar();
            return lista.FirstOrDefault(p => p.id_polleria == id);
        }
    }
}