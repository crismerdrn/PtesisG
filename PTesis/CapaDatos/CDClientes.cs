using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class CDClientes
    {
        private CMClientes CrearCliente(SqlDataReader dr)
        {
            return new CMClientes
            {
                id_cliente = Convert.ToInt32(dr["id_cliente"]),
                Nombre = dr["Nombre"].ToString(),
                Apellido = dr["Apellido"].ToString(),
                Telefono = dr["Telefono"].ToString(),
                Direccion = dr["Direccion"] != DBNull.Value ? dr["Direccion"].ToString() : "",
                Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "",
                Estado = Convert.ToBoolean(dr["Estado"]),
                FechaRegistro = Convert.ToDateTime(dr["Fecha_Registro"])
            };
        }

        public List<CMClientes> Listar(bool? estado = null)
        {
            List<CMClientes> lista = new List<CMClientes>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Cliente_Listar", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Estado", (object)estado ?? DBNull.Value);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(CrearCliente(dr));
                    }
                }
            }
            return lista;
        }

        public int Registrar(CMClientes obj, out string Mensaje)
        {
            int idGenerado = 0;
            Mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_Cliente_Registrar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Nombre", obj.Nombre);
                    cmd.Parameters.AddWithValue("@Apellido", obj.Apellido);
                    cmd.Parameters.AddWithValue("@Telefono", obj.Telefono);
                    cmd.Parameters.AddWithValue("@Direccion", (object)obj.Direccion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)obj.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado", obj.Estado);

                    // Parámetros de salida según tu SP
                    SqlParameter resParam = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                    SqlParameter msgParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                    SqlParameter idParam = new SqlParameter("@IdClienteGenerado", SqlDbType.Int) { Direction = ParameterDirection.Output };

                    cmd.Parameters.Add(resParam);
                    cmd.Parameters.Add(msgParam);
                    cmd.Parameters.Add(idParam);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    idGenerado = Convert.ToInt32(idParam.Value);
                    Mensaje = msgParam.Value.ToString();
                }
                catch (Exception ex)
                {
                    idGenerado = 0;
                    Mensaje = ex.Message;
                }
            }
            return idGenerado;
        }

        public bool Editar(CMClientes obj, out string Mensaje)
        {
            bool resultado = false;
            Mensaje = string.Empty;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_Cliente_Editar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdCliente", obj.id_cliente);
                    cmd.Parameters.AddWithValue("@Nombre", obj.Nombre);
                    cmd.Parameters.AddWithValue("@Apellido", obj.Apellido);
                    cmd.Parameters.AddWithValue("@Telefono", obj.Telefono);
                    cmd.Parameters.AddWithValue("@Direccion", (object)obj.Direccion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)obj.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado", obj.Estado);

                    SqlParameter resParam = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                    SqlParameter msgParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };

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
                    SqlCommand cmd = new SqlCommand("sp_Cliente_Eliminar", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdCliente", id);

                    SqlParameter resParam = new SqlParameter("@Resultado", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                    SqlParameter msgParam = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };

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

        public List<CMClientes> Buscar(string textoBusqueda)
        {
            List<CMClientes> lista = new List<CMClientes>();
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_Cliente_Buscar", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Busqueda", (object)textoBusqueda ?? DBNull.Value);

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(CrearCliente(dr));
                    }
                }
            }
            return lista;
        }
    }
}