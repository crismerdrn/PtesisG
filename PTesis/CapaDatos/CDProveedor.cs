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
    public class CDProveedor
    {
        public List<CMProveedor> ListarProveedores()
        {
            List<CMProveedor> lista = new List<CMProveedor>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("LISTAR_Proveedores", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        lista.Add(new CMProveedor
                        {
                            id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
                            esEmpresa = Convert.ToBoolean(dr["esEmpresa"]),
                            RazonSocial = dr["RazonSocial"]?.ToString() ?? "",
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Apellido = dr["Apellido"]?.ToString() ?? "",
                            CI = dr["CI"]?.ToString() ?? "",
                            NIT = dr["NIT"]?.ToString() ?? "",
                            Telefono = dr["Telefono"]?.ToString() ?? "",
                            Direccion = dr["Direccion"]?.ToString() ?? "",
                            Email = dr["Email"]?.ToString() ?? "",
                            Estado = Convert.ToBoolean(dr["Estado"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar proveedores: " + ex.Message);
            }
            return lista;
        }

        public List<CMProveedor> ListarProveedoresActivos()
        {
            List<CMProveedor> lista = new List<CMProveedor>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("LISTAR_Proveedores_Activos", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        lista.Add(new CMProveedor
                        {
                            id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
                            esEmpresa = Convert.ToBoolean(dr["esEmpresa"]),
                            RazonSocial = dr["RazonSocial"]?.ToString() ?? "",
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Apellido = dr["Apellido"]?.ToString() ?? "",
                            CI = dr["CI"]?.ToString() ?? "",
                            NIT = dr["NIT"]?.ToString() ?? "",
                            Telefono = dr["Telefono"]?.ToString() ?? "",
                            Direccion = dr["Direccion"]?.ToString() ?? "",
                            Email = dr["Email"]?.ToString() ?? "",
                            Estado = Convert.ToBoolean(dr["Estado"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar proveedores activos: " + ex.Message);
            }
            return lista;
        }

        public CMProveedor ObtenerProveedorPorId(int idProveedor)
        {
            CMProveedor proveedor = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("OBTENER_Proveedor_PorId", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_proveedor", idProveedor);
                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        proveedor = new CMProveedor
                        {
                            id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
                            esEmpresa = Convert.ToBoolean(dr["esEmpresa"]),
                            RazonSocial = dr["RazonSocial"]?.ToString() ?? "",
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Apellido = dr["Apellido"]?.ToString() ?? "",
                            CI = dr["CI"]?.ToString() ?? "",
                            NIT = dr["NIT"]?.ToString() ?? "",
                            Telefono = dr["Telefono"]?.ToString() ?? "",
                            Direccion = dr["Direccion"]?.ToString() ?? "",
                            Email = dr["Email"]?.ToString() ?? "",
                            Estado = Convert.ToBoolean(dr["Estado"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener proveedor por ID: " + ex.Message);
            }
            return proveedor;
        }

        public CMProveedor ObtenerProveedorPorCI(string ci)
        {
            CMProveedor proveedor = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("OBTENER_Proveedor_PorCI", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CI", ci);
                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        proveedor = new CMProveedor
                        {
                            id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
                            esEmpresa = Convert.ToBoolean(dr["esEmpresa"]),
                            RazonSocial = dr["RazonSocial"]?.ToString() ?? "",
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Apellido = dr["Apellido"]?.ToString() ?? "",
                            CI = dr["CI"]?.ToString() ?? "",
                            NIT = dr["NIT"]?.ToString() ?? "",
                            Telefono = dr["Telefono"]?.ToString() ?? "",
                            Direccion = dr["Direccion"]?.ToString() ?? "",
                            Email = dr["Email"]?.ToString() ?? "",
                            Estado = Convert.ToBoolean(dr["Estado"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener proveedor por CI: " + ex.Message);
            }
            return proveedor;
        }

        public List<CMProveedor> BuscarProveedor(string busqueda)
        {
            List<CMProveedor> lista = new List<CMProveedor>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("BUSCAR_Proveedor", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@busqueda", busqueda);
                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        lista.Add(new CMProveedor
                        {
                            id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
                            esEmpresa = Convert.ToBoolean(dr["esEmpresa"]),
                            RazonSocial = dr["RazonSocial"]?.ToString() ?? "",
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Apellido = dr["Apellido"]?.ToString() ?? "",
                            CI = dr["CI"]?.ToString() ?? "",
                            NIT = dr["NIT"]?.ToString() ?? "",
                            Telefono = dr["Telefono"]?.ToString() ?? "",
                            Direccion = dr["Direccion"]?.ToString() ?? "",
                            Email = dr["Email"]?.ToString() ?? "",
                            Estado = Convert.ToBoolean(dr["Estado"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al buscar proveedores: " + ex.Message);
            }
            return lista;
        }

        public bool ExisteProveedor(string valor, bool esEmpresa, int? idExcluir = null)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                cn.Open();
                string sql = "SELECT COUNT(*) FROM PROVEEDOR WHERE ";
                sql += esEmpresa ? "NIT = @valor" : "CI = @valor";

                if (idExcluir.HasValue)
                    sql += " AND id_proveedor <> @idExcluir";

                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@valor", valor);
                if (idExcluir.HasValue)
                    cmd.Parameters.AddWithValue("@idExcluir", idExcluir.Value);

                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }


        public bool ExisteTelefono(string telefono, int? idExcluir = null)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = "SELECT COUNT(1) FROM PROVEEDOR WHERE Telefono = @Telefono";
                    if (idExcluir.HasValue)
                        query += " AND id_proveedor != @id_proveedor";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@Telefono", telefono.Trim());

                    if (idExcluir.HasValue)
                        cmd.Parameters.AddWithValue("@id_proveedor", idExcluir.Value);

                    cn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al verificar teléfono existente: " + ex.Message);
            }
        }

        public bool AgregarProveedor(CMProveedor proveedor)
        {
            // Validaciones previas
            if (proveedor == null)
                throw new ArgumentNullException(nameof(proveedor), "El proveedor no puede ser nulo");

            if (string.IsNullOrWhiteSpace(proveedor.Telefono))
                throw new ArgumentException("El teléfono del proveedor es obligatorio");

            // Validaciones específicas según tipo
            if (proveedor.esEmpresa)
            {
                if (string.IsNullOrWhiteSpace(proveedor.RazonSocial))
                    throw new ArgumentException("La razón social es obligatoria para empresas");

                if (string.IsNullOrWhiteSpace(proveedor.NIT))
                    throw new ArgumentException("El NIT es obligatorio para empresas");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(proveedor.Nombre))
                    throw new ArgumentException("El nombre es obligatorio para personas");

                if (string.IsNullOrWhiteSpace(proveedor.Apellido))
                    throw new ArgumentException("El apellido es obligatorio para personas");

                if (string.IsNullOrWhiteSpace(proveedor.CI))
                    throw new ArgumentException("El CI es obligatorio para personas");
            }

            // Verificar si ya existe un proveedor con la misma identificación
            string identificacion = proveedor.esEmpresa ? proveedor.NIT : proveedor.CI;
            // Nota: Asegúrate de que el método ExisteProveedor maneje bien el paso de parámetros
            if (ExisteProveedor(identificacion, proveedor.esEmpresa))
            {
                string mensaje = proveedor.esEmpresa ?
                    $"Ya existe una empresa con el NIT '{proveedor.NIT}'" :
                    $"Ya existe una persona con el CI '{proveedor.CI}'";
                throw new InvalidOperationException(mensaje);
            }

            // Verificar si ya existe un proveedor con el mismo teléfono
            if (ExisteTelefono(proveedor.Telefono))
                throw new InvalidOperationException($"Ya existe un proveedor con el teléfono '{proveedor.Telefono}'");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    // Asegúrate que el nombre coincida con tu SP en SQL Server
                    SqlCommand cmd = new SqlCommand("ADICIONAR_Proveedor", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@esEmpresa", proveedor.esEmpresa);

                    // Lógica ternaria para enviar NULL si el string está vacío o no corresponde al tipo
                    cmd.Parameters.AddWithValue("@RazonSocial",
                        proveedor.esEmpresa && !string.IsNullOrWhiteSpace(proveedor.RazonSocial)
                            ? (object)proveedor.RazonSocial.Trim()
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue("@Nombre",
                        !proveedor.esEmpresa && !string.IsNullOrWhiteSpace(proveedor.Nombre)
                            ? (object)proveedor.Nombre.Trim()
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue("@Apellido",
                        !proveedor.esEmpresa && !string.IsNullOrWhiteSpace(proveedor.Apellido)
                            ? (object)proveedor.Apellido.Trim()
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue("@CI",
                        !proveedor.esEmpresa && !string.IsNullOrWhiteSpace(proveedor.CI)
                            ? (object)proveedor.CI.Trim()
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue("@NIT",
                        proveedor.esEmpresa && !string.IsNullOrWhiteSpace(proveedor.NIT)
                            ? (object)proveedor.NIT.Trim()
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue("@Telefono", proveedor.Telefono.Trim());

                    cmd.Parameters.AddWithValue("@Direccion",
                        !string.IsNullOrWhiteSpace(proveedor.Direccion)
                            ? (object)proveedor.Direccion.Trim()
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue("@Email",
                        !string.IsNullOrWhiteSpace(proveedor.Email)
                            ? (object)proveedor.Email.Trim()
                            : DBNull.Value);

                    // Asumiendo que el SP tiene un parámetro @Estado
                    cmd.Parameters.AddWithValue("@Estado", proveedor.Estado);

                    cn.Open();

                    // --- CORRECCIÓN CRÍTICA ---
                    // Usamos ExecuteScalar porque tu SP devuelve un SELECT SCOPE_IDENTITY()
                    // y tiene SET NOCOUNT ON, por lo que ExecuteNonQuery devolvería -1.
                    object idGenerado = cmd.ExecuteScalar();

                    if (idGenerado != null && Convert.ToInt32(idGenerado) > 0)
                    {
                        resultado = true;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar proveedor: " + ex.Message);
            }
            return resultado;
        }

        public bool ModificarProveedor(CMProveedor proveedor)
        {
            // Validaciones previas
            if (proveedor == null)
                throw new ArgumentNullException(nameof(proveedor), "El proveedor no puede ser nulo");

            if (proveedor.id_proveedor <= 0)
                throw new ArgumentException("ID de proveedor inválido");

            if (string.IsNullOrWhiteSpace(proveedor.Telefono))
                throw new ArgumentException("El teléfono del proveedor es obligatorio");

            // Validaciones específicas según tipo
            if (proveedor.esEmpresa)
            {
                if (string.IsNullOrWhiteSpace(proveedor.RazonSocial))
                    throw new ArgumentException("La razón social es obligatoria para empresas");

                if (string.IsNullOrWhiteSpace(proveedor.NIT))
                    throw new ArgumentException("El NIT es obligatorio para empresas");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(proveedor.Nombre))
                    throw new ArgumentException("El nombre es obligatorio para personas");

                if (string.IsNullOrWhiteSpace(proveedor.Apellido))
                    throw new ArgumentException("El apellido es obligatorio para personas");

                if (string.IsNullOrWhiteSpace(proveedor.CI))
                    throw new ArgumentException("El CI es obligatorio para personas");
            }

            // Verificar si ya existe otro proveedor con la misma identificación
            string identificacion = proveedor.esEmpresa ? proveedor.NIT : proveedor.CI;
            if (ExisteProveedor(identificacion, proveedor.esEmpresa, proveedor.id_proveedor))
            {
                string mensaje = proveedor.esEmpresa ?
                    $"Ya existe otra empresa con el NIT '{proveedor.NIT}'" :
                    $"Ya existe otra persona con el CI '{proveedor.CI}'";
                throw new InvalidOperationException(mensaje);
            }

            // Verificar si ya existe otro proveedor con el mismo teléfono
            if (ExisteTelefono(proveedor.Telefono, proveedor.id_proveedor))
                throw new InvalidOperationException($"Ya existe otro proveedor con el teléfono '{proveedor.Telefono}'");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("MODIFICAR_Proveedor", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id_proveedor", proveedor.id_proveedor);
                    cmd.Parameters.AddWithValue("@esEmpresa", proveedor.esEmpresa);
                    cmd.Parameters.AddWithValue("@RazonSocial",
                        string.IsNullOrWhiteSpace(proveedor.RazonSocial) ? (object)DBNull.Value : proveedor.RazonSocial.Trim());
                    cmd.Parameters.AddWithValue("@Nombre",
                        string.IsNullOrWhiteSpace(proveedor.Nombre) ? (object)DBNull.Value : proveedor.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Apellido",
                        string.IsNullOrWhiteSpace(proveedor.Apellido) ? (object)DBNull.Value : proveedor.Apellido.Trim());
                    cmd.Parameters.AddWithValue("@CI",
                        string.IsNullOrWhiteSpace(proveedor.CI) ? (object)DBNull.Value : proveedor.CI.Trim());
                    cmd.Parameters.AddWithValue("@NIT",
                        string.IsNullOrWhiteSpace(proveedor.NIT) ? (object)DBNull.Value : proveedor.NIT.Trim());
                    cmd.Parameters.AddWithValue("@Telefono", proveedor.Telefono.Trim());
                    cmd.Parameters.AddWithValue("@Direccion",
                        string.IsNullOrWhiteSpace(proveedor.Direccion) ? (object)DBNull.Value : proveedor.Direccion.Trim());
                    cmd.Parameters.AddWithValue("@Email",
                        string.IsNullOrWhiteSpace(proveedor.Email) ? (object)DBNull.Value : proveedor.Email.Trim());
                    cmd.Parameters.AddWithValue("@Estado", proveedor.Estado);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                // UNIQUE KEY violation
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    string msg = ex.Message.ToUpper();

                    if (msg.Contains("CI"))
                        throw new InvalidOperationException($"Ya existe un proveedor con el CI '{proveedor.CI}'");

                    if (msg.Contains("NIT"))
                        throw new InvalidOperationException($"Ya existe un proveedor con el NIT '{proveedor.NIT}'");

                    throw new InvalidOperationException("Error: dato duplicado en un campo único.");
                }

                // NOT NULL violation (Telefono, Estado, FechaRegistro, esEmpresa)
                if (ex.Number == 515)
                {
                    if (ex.Message.ToUpper().Contains("TELEFONO"))
                        throw new InvalidOperationException("El campo Teléfono es obligatorio.");

                    throw new InvalidOperationException("Falta un dato obligatorio en la tabla Proveedor.");
                }

                throw new Exception("Error SQL al modificar proveedor: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error general al modificar proveedor: " + ex.Message);
            }

            return resultado;
        }

        public bool EliminarProveedor(int idProveedor)
        {
            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("ELIMINAR_Proveedor", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_proveedor", idProveedor);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547) 
                {
                    throw new InvalidOperationException("No se puede eliminar el proveedor porque tiene registros relacionados (compras, productos)");
                }
                throw new Exception("Error al eliminar proveedor: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al eliminar proveedor: " + ex.Message);
            }
            return resultado;
        }

        public bool CambiarEstadoProveedor(int idProveedor, bool estado)
        {
            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("CAMBIAR_ESTADO_Proveedor", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id_proveedor", idProveedor);
                    cmd.Parameters.AddWithValue("@Estado", estado);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cambiar estado del proveedor: " + ex.Message);
            }
            return resultado;
        }

        public bool TieneComprasAsociadas(int idProveedor)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = "SELECT COUNT(1) FROM COMPRA WHERE PROVEEDOR_id_proveedor = @id_proveedor";
                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@id_proveedor", idProveedor);

                    cn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al verificar compras asociadas: " + ex.Message);
            }
        }
        public List<CMProveedor> ObtenerProveedoresPorTipo(bool esEmpresa)
        {
            List<CMProveedor> lista = new List<CMProveedor>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = "SELECT * FROM PROVEEDOR WHERE esEmpresa = @esEmpresa AND Estado = 1 ORDER BY RazonSocial, Nombre";
                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@esEmpresa", esEmpresa);

                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        lista.Add(new CMProveedor
                        {
                            id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
                            esEmpresa = Convert.ToBoolean(dr["esEmpresa"]),
                            RazonSocial = dr["RazonSocial"]?.ToString() ?? "",
                            Nombre = dr["Nombre"]?.ToString() ?? "",
                            Apellido = dr["Apellido"]?.ToString() ?? "",
                            CI = dr["CI"]?.ToString() ?? "",
                            NIT = dr["NIT"]?.ToString() ?? "",
                            Telefono = dr["Telefono"]?.ToString() ?? "",
                            Direccion = dr["Direccion"]?.ToString() ?? "",
                            Email = dr["Email"]?.ToString() ?? "",
                            Estado = Convert.ToBoolean(dr["Estado"]),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener proveedores por tipo: " + ex.Message);
            }
            return lista;
        }
    }
}