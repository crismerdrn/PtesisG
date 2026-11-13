using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class CDUsuario
    {
        public int LoginUsuario(string usuario, string contrasenaHash)
        {
            int idUsuario = 0;

            try
            {
                using (SqlConnection conexion = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("LoginUsuario", conexion);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@password", contrasenaHash);

                    SqlParameter paramId = new SqlParameter("@id_usuario", SqlDbType.Int);
                    paramId.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramId);

                    conexion.Open();
                    cmd.ExecuteNonQuery();
                    idUsuario = Convert.ToInt32(cmd.Parameters["@id_usuario"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en LoginUsuario: " + ex.Message);
                idUsuario = -1;
            }

            return idUsuario;
        }

        public Usuario ObtenerUsuarioConPermisos(int idUsuario)
        {
            Usuario usuarioSesion = null;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("OBTENER_USUARIO_CON_PERMISOS", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@id_usuario", idUsuario);

                cn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        usuarioSesion = new Usuario
                        {
                            id_usuario = Convert.ToInt32(dr["id_usuario"]),
                            nombre = dr["nombre"].ToString(),
                            apellido = dr["apellido"].ToString(),
                            correo = dr["correo"].ToString(),
                            usuario = dr["usuario"].ToString(),
                            ROL_id_rol = Convert.ToInt32(dr["ROL_id_rol"]),
                            Rol = dr["RolNombre"].ToString()
                        };
                    }

                    if (usuarioSesion != null)
                    {
                        dr.NextResult(); 

                        while (dr.Read())
                        {
                            usuarioSesion.Permisos.Add(dr["NombreMenu"].ToString());
                        }
                    }
                }
            }

            return usuarioSesion;
        }

        public List<Usuario> ListarUsuarios()
        {
            List<Usuario> lista = new List<Usuario>();

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("LISTAR_Usuarios", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Usuario usuario = new Usuario
                    {
                        id_usuario = Convert.ToInt32(dr["id_usuario"]),
                        nombre = dr["nombre"].ToString(),
                        apellido = dr["apellido"].ToString(),
                        correo = dr["correo"].ToString(),
                        telefono = dr["telefono"].ToString(),
                        direccion = dr["direccion"].ToString(),
                        usuario = dr["usuario"].ToString(),
                        estado = Convert.ToBoolean(dr["estado"]),
                        Rol = dr["Rol"].ToString()
                    };

                    if (ColumnExists(dr, "ROL_id_rol"))
                    {
                        usuario.ROL_id_rol = dr["ROL_id_rol"] != DBNull.Value
                            ? Convert.ToInt32(dr["ROL_id_rol"])
                            : 0;
                    }
                    else
                    {
                        usuario.ROL_id_rol = ObtenerIdRolPorNombre(usuario.Rol);
                    }

                    lista.Add(usuario);
                }
            }
            return lista;
        }

        public Usuario ObtenerUsuarioPorId(int idUsuario)
        {
            Usuario usuario = null;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM USUARIOS WHERE id_usuario = @id", cn);
                cmd.Parameters.AddWithValue("@id", idUsuario);

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    usuario = new Usuario
                    {
                        id_usuario = Convert.ToInt32(dr["id_usuario"]),
                        nombre = dr["nombre"].ToString(),
                        apellido = dr["apellido"].ToString(),
                        correo = dr["correo"].ToString(),
                        telefono = dr["telefono"].ToString(),
                        direccion = dr["direccion"].ToString(),
                        usuario = dr["usuario"].ToString(),
                        password = dr["password"].ToString(),
                        estado = Convert.ToBoolean(dr["estado"]),
                        ROL_id_rol = Convert.ToInt32(dr["ROL_id_rol"])
                    };
                }
            }
            return usuario;
        }

        private int ObtenerIdRolPorNombre(string nombreRol)
        {
            if (string.IsNullOrEmpty(nombreRol)) return 0;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("SELECT id_rol FROM ROL WHERE Nombre = @nombre", cn);
                    cmd.Parameters.AddWithValue("@nombre", nombreRol);

                    cn.Open();
                    object result = cmd.ExecuteScalar();

                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private bool ColumnExists(SqlDataReader reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName) >= 0;
            }
            catch
            {
                return false;
            }
        }
        public bool AgregarUsuario(Usuario u)
        {
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("ADICIONAR_Usuario", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@nombre", u.nombre);
                cmd.Parameters.AddWithValue("@apellido", u.apellido);
                cmd.Parameters.AddWithValue("@correo", u.correo);
                cmd.Parameters.AddWithValue("@telefono", u.telefono);
                cmd.Parameters.AddWithValue("@direccion", u.direccion);
                cmd.Parameters.AddWithValue("@usuario", u.usuario);
                cmd.Parameters.AddWithValue("@password", u.password);
                cmd.Parameters.AddWithValue("@ROL_id_rol", u.ROL_id_rol);

                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool ModificarUsuario(Usuario u)
        {
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("MODIFICAR_Usuario_PasswordOpcional", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@id_usuario", u.id_usuario);
                cmd.Parameters.AddWithValue("@nombre", u.nombre);
                cmd.Parameters.AddWithValue("@apellido", u.apellido);
                cmd.Parameters.AddWithValue("@correo", u.correo);
                cmd.Parameters.AddWithValue("@telefono", u.telefono);
                cmd.Parameters.AddWithValue("@direccion", u.direccion);
                cmd.Parameters.AddWithValue("@usuario", u.usuario);
                cmd.Parameters.AddWithValue("@password", u.password ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", u.estado);
                cmd.Parameters.AddWithValue("@ROL_id_rol", u.ROL_id_rol);

                cn.Open();
                resultado = cmd.ExecuteNonQuery() > 0;
            }
            return resultado;
        }

        public bool EliminarUsuario(int id, out string mensaje)
        {
            bool resultado = false;
            mensaje = string.Empty;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("ELIMINAR_Usuario", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                  
                    cmd.Parameters.AddWithValue("@id_usuario", id);

                    SqlParameter paramResultado = new SqlParameter("@resultado", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    SqlParameter paramMensaje = new SqlParameter("@mensaje", SqlDbType.VarChar, 300)
                    {
                        Direction = ParameterDirection.Output
                    };

                    cmd.Parameters.Add(paramResultado);
                    cmd.Parameters.Add(paramMensaje);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(cmd.Parameters["@resultado"].Value);
                    mensaje = cmd.Parameters["@mensaje"].Value != DBNull.Value
                                ? cmd.Parameters["@mensaje"].Value.ToString()
                                : string.Empty;
                }
            }
            catch (Exception ex)
            {
                resultado = false;
                mensaje = "Error en la capa de datos: " + ex.Message;
            }

            return resultado;
        }

        public bool ExisteUsuario(string usuario)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM USUARIOS WHERE usuario = @usuario", cn);
                cmd.Parameters.AddWithValue("@usuario", usuario);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public bool ExisteUsuario(string usuario, int idUsuario)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM USUARIOS WHERE usuario = @usuario AND id_usuario <> @id", cn);
                cmd.Parameters.AddWithValue("@usuario", usuario);
                cmd.Parameters.AddWithValue("@id", idUsuario);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public bool ExisteCorreo(string correo)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM USUARIOS WHERE correo = @correo", cn);
                cmd.Parameters.AddWithValue("@correo", correo);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        public bool ExisteCorreo(string correo, int idUsuario)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM USUARIOS WHERE correo = @correo AND id_usuario <> @id", cn);
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@id", idUsuario);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }
    }
}