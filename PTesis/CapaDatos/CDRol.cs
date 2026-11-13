using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class CDRol
    {
        public List<CMRol> ListarRoles()
        {
            List<CMRol> lista = new List<CMRol>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("LISTAR_Rol", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        lista.Add(new CMRol
                        {
                            id_rol = Convert.ToInt32(dr["id_rol"]),
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Descripcion = dr["Descripcion"]?.ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar roles: " + ex.Message);
            }
            return lista;
        }

        public bool ExisteRol(string nombre, int? idExcluir = null)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = "SELECT COUNT(1) FROM ROL WHERE Nombre = @Nombre";
                    if (idExcluir.HasValue)
                        query += " AND id_rol != @id_rol";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@Nombre", nombre.Trim().ToUpper());

                    if (idExcluir.HasValue)
                        cmd.Parameters.AddWithValue("@id_rol", idExcluir.Value);

                    cn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al verificar rol existente: " + ex.Message);
            }
        }

        public bool AgregarRol(CMRol rol)
        {
            if (rol == null)
                throw new ArgumentNullException(nameof(rol), "El rol no puede ser nulo");

            if (string.IsNullOrWhiteSpace(rol.Nombre))
                throw new ArgumentException("El nombre del rol es obligatorio");

            if (string.IsNullOrWhiteSpace(rol.Descripcion))
                throw new ArgumentException("La descripción del rol es obligatoria");

            if (ExisteRol(rol.Nombre))
                throw new InvalidOperationException($"Ya existe un rol con el nombre '{rol.Nombre}'");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("ADICIONAR_Rol", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Nombre", rol.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Descripcion", rol.Descripcion.Trim());
                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) 
                    throw new InvalidOperationException($"Ya existe un rol con el nombre '{rol.Nombre}'");
                else
                    throw new Exception("Error de base de datos al agregar rol: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar rol: " + ex.Message);
            }
            return resultado;
        }

        public bool ModificarRol(CMRol rol)
        {
            if (rol == null)
                throw new ArgumentNullException(nameof(rol), "El rol no puede ser nulo");

            if (rol.id_rol <= 0)
                throw new ArgumentException("ID de rol inválido");

            if (string.IsNullOrWhiteSpace(rol.Nombre))
                throw new ArgumentException("El nombre del rol es obligatorio");

            if (string.IsNullOrWhiteSpace(rol.Descripcion))
                throw new ArgumentException("La descripción del rol es obligatoria");

            if (ExisteRol(rol.Nombre, rol.id_rol))
                throw new InvalidOperationException($"Ya existe otro rol con el nombre '{rol.Nombre}'");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("MODIFICAR_Rol", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_rol", rol.id_rol);
                    cmd.Parameters.AddWithValue("@Nombre", rol.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Descripcion", rol.Descripcion.Trim());
                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) 
                    throw new InvalidOperationException($"Ya existe otro rol con el nombre '{rol.Nombre}'");
                else
                    throw new Exception("Error de base de datos al modificar rol: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al modificar rol: " + ex.Message);
            }
            return resultado;
        }

        public bool EliminarRol(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID de rol inválido");

            var rol = ObtenerRolPorId(id);
            if (rol == null)
                throw new ArgumentException("El rol no existe");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("ELIMINAR_Rol", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_rol", id);

                    SqlParameter returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                    returnParameter.Direction = ParameterDirection.ReturnValue;

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = (int)returnParameter.Value == 1;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                {
                    throw new InvalidOperationException(ex.Message);
                }
                else if (ex.Number == 547)
                {
                    throw new InvalidOperationException("No se puede eliminar el rol porque está siendo utilizado en el sistema.");
                }
                else
                {
                    throw new Exception("Error de base de datos al eliminar rol: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al eliminar rol: " + ex.Message);
            }

            return resultado;
        }

        public CMRol ObtenerRolPorId(int id)
        {
            if (id <= 0)
                return null;

            CMRol rol = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("SELECT * FROM ROL WHERE id_rol = @id_rol", cn);
                    cmd.Parameters.AddWithValue("@id_rol", id);
                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        rol = new CMRol
                        {
                            id_rol = Convert.ToInt32(dr["id_rol"]),
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Descripcion = dr["Descripcion"]?.ToString() ?? ""
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener rol por ID: " + ex.Message);
            }
            return rol;
        }
    }

}
