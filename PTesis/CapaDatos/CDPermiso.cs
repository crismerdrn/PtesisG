using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CapaDatos
{
    public class CDPermiso
    {

        public List<CMPermisos> ListarPermisos()
        {
            List<CMPermisos> lista = new List<CMPermisos>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand(@"
                SELECT p.id_permiso,
                       p.Nombre AS NombreMenu,
                       p.Modulo,
                       p.Tipo,
                       p.Padre_id,
                       p.Orden,
                       p.Descripcion,
                       p.FechaRegistro,
                       padre.Nombre AS PadreNombre
                FROM PERMISOS p
                LEFT JOIN PERMISOS padre ON p.Padre_id = padre.id_permiso
                ORDER BY ISNULL(p.Orden, 999), p.Nombre", cn);
                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        lista.Add(new CMPermisos
                        {
                            id_permiso = Convert.ToInt32(dr["id_permiso"]),
                            NombreMenu = dr["NombreMenu"]?.ToString() ?? "",
                            Modulo = dr["Modulo"]?.ToString(),
                            Tipo = dr["Tipo"]?.ToString() ?? "MENU",
                            Padre_id = dr["Padre_id"] != DBNull.Value ? Convert.ToInt32(dr["Padre_id"]) : (int?)null,
                            PadreNombre = dr["PadreNombre"]?.ToString(),
                            Orden = dr["Orden"] != DBNull.Value ? Convert.ToInt32(dr["Orden"]) : (int?)null,
                            Descripcion = dr["Descripcion"]?.ToString(),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar permisos: " + ex.Message);
            }
            return lista;
        }

        public List<CMPermisos> ListarPermisosConJerarquia(int? idRol = null)
        {
            List<CMPermisos> listaCompleta = new List<CMPermisos>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("LISTAR_PERMISOS_UI", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdRol", idRol.HasValue ? (object)idRol.Value : DBNull.Value);

                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    Dictionary<int, CMPermisos> dicPermisos = new Dictionary<int, CMPermisos>();

                    while (dr.Read())
                    {
                        CMPermisos permiso = new CMPermisos
                        {
                            id_permiso = Convert.ToInt32(dr["Id"]),
                            NombreMenu = dr["Nombre"].ToString(),
                            Modulo = dr["Modulo"]?.ToString(),
                            Tipo = dr["Tipo"]?.ToString(),
                            Padre_id = dr["PadreId"] != DBNull.Value ? Convert.ToInt32(dr["PadreId"]) : (int?)null,
                            Orden = dr["Orden"] != DBNull.Value ? Convert.ToInt32(dr["Orden"]) : (int?)null,
                            Descripcion = dr["Descripcion"]?.ToString(),
                            Nivel = Convert.ToInt32(dr["Nivel"]),
                            Asignado = Convert.ToBoolean(dr["Asignado"])
                        };

                        dicPermisos[permiso.id_permiso] = permiso;

                        if (permiso.Nivel == 1)
                        {
                            listaCompleta.Add(permiso);
                        }
                    }

                    if (dr.NextResult())
                    {
                        while (dr.Read())
                        {
                            CMPermisos permiso = new CMPermisos
                            {
                                id_permiso = Convert.ToInt32(dr["Id"]),
                                NombreMenu = dr["Nombre"].ToString(),
                                Modulo = dr["Modulo"]?.ToString(),
                                Tipo = dr["Tipo"]?.ToString(),
                                Padre_id = dr["PadreId"] != DBNull.Value ? Convert.ToInt32(dr["PadreId"]) : (int?)null,
                                Orden = dr["Orden"] != DBNull.Value ? Convert.ToInt32(dr["Orden"]) : (int?)null,
                                Descripcion = dr["Descripcion"]?.ToString(),
                                PadreNombre = dr["PadreNombre"]?.ToString(),
                                Nivel = Convert.ToInt32(dr["Nivel"]),
                                Asignado = Convert.ToBoolean(dr["Asignado"])
                            };

                            dicPermisos[permiso.id_permiso] = permiso;

                            if (permiso.Padre_id.HasValue && dicPermisos.ContainsKey(permiso.Padre_id.Value))
                            {
                                dicPermisos[permiso.Padre_id.Value].Hijos.Add(permiso);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar permisos con jerarquía: " + ex.Message);
            }

            return listaCompleta;
        }

        public CMPermisos ObtenerPermisoPorId(int id)
        {
            if (id <= 0)
                return null;

            CMPermisos permiso = null;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand(@"
                SELECT p.id_permiso,
                       p.Nombre AS NombreMenu,
                       p.Modulo,
                       p.Tipo,
                       p.Padre_id,
                       p.Orden,
                       p.Descripcion,
                       p.FechaRegistro,
                       padre.Nombre AS PadreNombre
                FROM PERMISOS p
                LEFT JOIN PERMISOS padre ON p.Padre_id = padre.id_permiso
                WHERE p.id_permiso = @id_permiso", cn);
                    cmd.Parameters.AddWithValue("@id_permiso", id);
                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        permiso = new CMPermisos
                        {
                            id_permiso = Convert.ToInt32(dr["id_permiso"]),
                            NombreMenu = dr["NombreMenu"]?.ToString() ?? "",
                            Modulo = dr["Modulo"]?.ToString(),
                            Tipo = dr["Tipo"]?.ToString() ?? "MENU",
                            Padre_id = dr["Padre_id"] != DBNull.Value ? Convert.ToInt32(dr["Padre_id"]) : (int?)null,
                            PadreNombre = dr["PadreNombre"]?.ToString(),
                            Orden = dr["Orden"] != DBNull.Value ? Convert.ToInt32(dr["Orden"]) : (int?)null,
                            Descripcion = dr["Descripcion"]?.ToString(),
                            FechaRegistro = Convert.ToDateTime(dr["FechaRegistro"])
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener permiso por ID: " + ex.Message);
            }
            return permiso;
        }

        public bool ExistePermiso(string nombreMenu, int? padreId = null, int? idExcluir = null)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = "SELECT COUNT(1) FROM PERMISOS WHERE Nombre = @Nombre";

                    if (padreId.HasValue)
                        query += " AND Padre_id = @Padre_id";
                    else
                        query += " AND Padre_id IS NULL";

                    if (idExcluir.HasValue)
                        query += " AND id_permiso != @id_permiso";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@Nombre", nombreMenu.Trim());

                    if (padreId.HasValue)
                        cmd.Parameters.AddWithValue("@Padre_id", padreId.Value);

                    if (idExcluir.HasValue)
                        cmd.Parameters.AddWithValue("@id_permiso", idExcluir.Value);

                    cn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al verificar permiso existente: " + ex.Message);
            }
        }

        public List<CMRolPermiso> ListarPermisosPorRol(int idRol)
        {
            List<CMRolPermiso> lista = new List<CMRolPermiso>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand(@"
                SELECT rp.id_rol_permiso,
                       rp.ROL_id_rol,
                       rp.permisos_id_permiso,
                       rp.FechaAsignacion,
                       r.Nombre AS RolNombre, 
                       p.Nombre AS PermisoNombre
                FROM ROL_PERMISO rp
                INNER JOIN ROL r ON rp.ROL_id_rol = r.id_rol
                INNER JOIN PERMISOS p ON rp.permisos_id_permiso = p.id_permiso
                WHERE rp.ROL_id_rol = @id_rol
                ORDER BY p.Orden, p.Nombre", cn);
                    cmd.Parameters.AddWithValue("@id_rol", idRol);
                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        lista.Add(new CMRolPermiso
                        {
                            id_rol_permiso = Convert.ToInt32(dr["id_rol_permiso"]),
                            ROL_id_rol = Convert.ToInt32(dr["ROL_id_rol"]),
                            RolNombre = dr["RolNombre"]?.ToString() ?? "",
                            permisos_id_permiso = Convert.ToInt32(dr["permisos_id_permiso"]),
                            PermisoNombre = dr["PermisoNombre"]?.ToString() ?? "",
                            FechaAsignacion = Convert.ToDateTime(dr["FechaAsignacion"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al listar permisos por rol: " + ex.Message);
            }
            return lista;
        }

        public List<int> ObtenerIdsPermisosPorRol(int idRol)
        {
            List<int> ids = new List<int>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand(@"
                        SELECT permisos_id_permiso
                        FROM ROL_PERMISO
                        WHERE ROL_id_rol = @id_rol", cn);
                    cmd.Parameters.AddWithValue("@id_rol", idRol);
                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        ids.Add(Convert.ToInt32(dr["permisos_id_permiso"]));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener IDs de permisos por rol: " + ex.Message);
            }
            return ids;
        }

        public bool AsignarPermisosARol(int idRol, List<int> permisosIds, int idUsuario, out string mensaje)
        {
            mensaje = "";
            bool resultado = false;

            try
            {
                if (permisosIds == null || permisosIds.Count == 0)
                {
                    mensaje = "No se recibió ningún permiso para asignar.";
                    System.Diagnostics.Debug.WriteLine("[CDPermiso] Lista de permisos VACÍA");
                    return false;
                }

                XElement xmlPermisos = new XElement("Permisos");
                foreach (int id in permisosIds)
                {
                    xmlPermisos.Add(new XElement("Id", id));
                }

                string xmlString = xmlPermisos.ToString();

                System.Diagnostics.Debug.WriteLine($"[CDPermiso] XML: {xmlString}");
                System.Diagnostics.Debug.WriteLine($"[CDPermiso] IDs: {string.Join(",", permisosIds)}");

                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("ASIGNAR_PERMISOS_A_ROL_UI", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@IdRol", idRol);

                    SqlParameter paramXml = new SqlParameter("@PermisosAsignados", SqlDbType.Xml);
                    paramXml.Value = xmlString;
                    cmd.Parameters.Add(paramXml);

                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    SqlParameter paramResultado = new SqlParameter("@Resultado", SqlDbType.Bit);
                    paramResultado.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramResultado);

                    SqlParameter paramMensaje = new SqlParameter("@Mensaje", SqlDbType.VarChar, 500);
                    paramMensaje.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(paramMensaje);

                    cn.Open();
                    cmd.ExecuteNonQuery();

                    resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                    mensaje = cmd.Parameters["@Mensaje"].Value?.ToString() ?? "";

                    System.Diagnostics.Debug.WriteLine($"[CDPermiso] SP → Resultado={resultado} | Mensaje={mensaje}");
                }
            }
            catch (Exception ex)
            {
                mensaje = "Error al asignar permisos: " + ex.Message;
                resultado = false;
                System.Diagnostics.Debug.WriteLine($"[CDPermiso] EXCEPCIÓN: {ex}");
            }

            return resultado;
        }

        public bool VerificarPermiso(int idUsuario, string nombreMenu)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(1)
                        FROM USUARIOS u
                        INNER JOIN ROL_PERMISO rp ON u.ROL_id_rol = rp.ROL_id_rol
                        INNER JOIN PERMISOS p ON rp.permisos_id_permiso = p.id_permiso
                        WHERE u.id_usuario = @id_usuario
                        AND p.NombreMenu = @nombreMenu", cn);
                    cmd.Parameters.AddWithValue("@id_usuario", idUsuario);
                    cmd.Parameters.AddWithValue("@nombreMenu", nombreMenu);
                    cn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al verificar permiso: " + ex.Message);
            }
        }

        public bool VerificarAccion(int idUsuario, string accion, string modulo)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand(@"
                        SELECT COUNT(1)
                        FROM USUARIOS u
                        INNER JOIN ROL_PERMISO rp ON u.ROL_id_rol = rp.ROL_id_rol
                        INNER JOIN PERMISOS p ON rp.permisos_id_permiso = p.id_permiso
                        WHERE u.id_usuario = @id_usuario
                        AND p.NombreMenu = @accion
                        AND (p.Modulo = @modulo OR @modulo IS NULL)", cn);
                    cmd.Parameters.AddWithValue("@id_usuario", idUsuario);
                    cmd.Parameters.AddWithValue("@accion", accion);
                    cmd.Parameters.AddWithValue("@modulo", modulo ?? (object)DBNull.Value);
                    cn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al verificar acción: " + ex.Message);
            }
        }

        public List<CMPermisos> ObtenerPermisosPadre()
        {
            List<CMPermisos> lista = new List<CMPermisos>();
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                SELECT id_permiso, Nombre AS NombreMenu, Modulo, Tipo 
                FROM PERMISOS 
                WHERE Tipo = 'MENU' 
                ORDER BY Orden, Nombre";

                    cn.Open();
                    SqlCommand cmd = new SqlCommand(query, cn);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        lista.Add(new CMPermisos
                        {
                            id_permiso = Convert.ToInt32(dr["id_permiso"]),
                            NombreMenu = dr["NombreMenu"].ToString(),
                            Modulo = dr["Modulo"]?.ToString(),
                            Tipo = dr["Tipo"]?.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener permisos padre: " + ex.Message);
            }
            return lista;
        }

        public bool AgregarPermiso(CMPermisos permiso)
        {
            if (permiso == null)
                throw new ArgumentNullException(nameof(permiso), "El permiso no puede ser nulo");

            if (string.IsNullOrWhiteSpace(permiso.NombreMenu))
                throw new ArgumentException("El nombre del menú es obligatorio");

            if (string.IsNullOrWhiteSpace(permiso.Modulo))
                throw new ArgumentException("El módulo es obligatorio");

            if (ExistePermiso(permiso.NombreMenu, permiso.Padre_id, null))
                throw new InvalidOperationException($"Ya existe un permiso con el nombre '{permiso.NombreMenu}' en este nivel");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                INSERT INTO PERMISOS 
                (Nombre, Modulo, Tipo, Padre_id, Orden, Descripcion, FechaRegistro)
                VALUES 
                (@Nombre, @Modulo, @Tipo, @Padre_id, @Orden, @Descripcion, GETDATE())";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@Nombre", permiso.NombreMenu.Trim());
                    cmd.Parameters.AddWithValue("@Modulo", permiso.Modulo.Trim());
                    cmd.Parameters.AddWithValue("@Tipo", permiso.Tipo ?? "MENU");
                    cmd.Parameters.AddWithValue("@Padre_id", permiso.Padre_id.HasValue ? (object)permiso.Padre_id.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Orden", permiso.Orden.HasValue ? (object)permiso.Orden.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(permiso.Descripcion) ? DBNull.Value : (object)permiso.Descripcion);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                    throw new InvalidOperationException($"Ya existe un permiso con el nombre '{permiso.NombreMenu}'");
                else
                    throw new Exception("Error de base de datos al agregar permiso: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar permiso: " + ex.Message);
            }
            return resultado;
        }

        public bool ModificarPermiso(CMPermisos permiso)
        {
            if (permiso == null)
                throw new ArgumentNullException(nameof(permiso), "El permiso no puede ser nulo");

            if (permiso.id_permiso <= 0)
                throw new ArgumentException("ID de permiso inválido");

            if (string.IsNullOrWhiteSpace(permiso.NombreMenu))
                throw new ArgumentException("El nombre del menú es obligatorio");

            if (ExistePermiso(permiso.NombreMenu, permiso.Padre_id, permiso.id_permiso))
                throw new InvalidOperationException($"Ya existe otro permiso con el nombre '{permiso.NombreMenu}' en este nivel");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                UPDATE PERMISOS 
                SET Nombre = @Nombre,
                    Modulo = @Modulo,
                    Tipo = @Tipo,
                    Padre_id = @Padre_id,
                    Orden = @Orden,
                    Descripcion = @Descripcion
                WHERE id_permiso = @id_permiso";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@id_permiso", permiso.id_permiso);
                    cmd.Parameters.AddWithValue("@Nombre", permiso.NombreMenu.Trim());
                    cmd.Parameters.AddWithValue("@Modulo", permiso.Modulo.Trim());
                    cmd.Parameters.AddWithValue("@Tipo", permiso.Tipo ?? "MENU");
                    cmd.Parameters.AddWithValue("@Padre_id", permiso.Padre_id.HasValue ? (object)permiso.Padre_id.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Orden", permiso.Orden.HasValue ? (object)permiso.Orden.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(permiso.Descripcion) ? DBNull.Value : (object)permiso.Descripcion);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                    throw new InvalidOperationException($"Ya existe otro permiso con el nombre '{permiso.NombreMenu}'");
                else
                    throw new Exception("Error de base de datos al modificar permiso: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al modificar permiso: " + ex.Message);
            }
            return resultado;
        }

        public bool EliminarPermiso(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID de permiso inválido");

            if (TieneHijos(id))
                throw new InvalidOperationException("No se puede eliminar el permiso porque tiene submenús o acciones asociadas");

            if (EstaAsignadoARol(id))
                throw new InvalidOperationException("No se puede eliminar el permiso porque está asignado a uno o más roles");

            bool resultado = false;
            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = "DELETE FROM PERMISOS WHERE id_permiso = @id_permiso";
                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@id_permiso", id);

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al eliminar permiso: " + ex.Message);
            }
            return resultado;
        }

        private bool TieneHijos(int idPermiso)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                string query = "SELECT COUNT(1) FROM PERMISOS WHERE Padre_id = @Padre_id";
                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.AddWithValue("@Padre_id", idPermiso);

                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private bool EstaAsignadoARol(int idPermiso)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                string query = "SELECT COUNT(1) FROM ROL_PERMISO WHERE permisos_id_permiso = @id_permiso";
                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.AddWithValue("@id_permiso", idPermiso);

                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public List<CMGrupoMatrizPermiso> ObtenerMatrizPermisos(int? idRol = null)
        {
            List<CMGrupoMatrizPermiso> grupos = new List<CMGrupoMatrizPermiso>();

            try
            {
                List<CMPermisos> todos = ListarPermisos();

                HashSet<int> asignados = new HashSet<int>();
                if (idRol.HasValue && idRol.Value > 0)
                {
                    asignados = new HashSet<int>(ObtenerIdsPermisosPorRol(idRol.Value));
                }
                var modulos = todos
                    .Where(p => p.Padre_id == null
                             && string.Equals(p.Tipo, "MENU", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.Orden ?? 999)
                    .ThenBy(p => p.NombreMenu)
                    .ToList();

                foreach (var modulo in modulos)
                {
                    var submenus = todos
                        .Where(p => p.Padre_id == modulo.id_permiso
                                 && string.Equals(p.Tipo, "MENU", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.Orden ?? 999)
                        .ThenBy(p => p.NombreMenu)
                        .ToList();

                    if (submenus.Count == 0)
                        continue;

                    var grupo = new CMGrupoMatrizPermiso
                    {
                        IdModuloPadre = modulo.id_permiso,
                        NombreModuloPadre = modulo.NombreMenu,
                    };

                    foreach (var sub in submenus)
                    {
                        var fila = new CMMatrizPermiso
                        {
                            IdSubmodulo = sub.id_permiso,
                            NombreSubmodulo = sub.NombreMenu,
                            Modulo = sub.Modulo
                        };

                        var acciones = todos
                            .Where(p => p.Padre_id == sub.id_permiso
                                     && string.Equals(p.Tipo, "ACCION", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        foreach (var accion in acciones)
                        {
                            string nombre = (accion.NombreMenu ?? "").ToLowerInvariant().Trim();

                            if (nombre.StartsWith("ver "))
                            {
                                fila.IdVer = accion.id_permiso;
                                fila.Ver = asignados.Contains(accion.id_permiso);
                            }
                            else if (nombre.StartsWith("agregar ") || nombre.StartsWith("crear "))
                            {
                                fila.IdCrear = accion.id_permiso;
                                fila.Crear = asignados.Contains(accion.id_permiso);
                            }
                            else if (nombre.StartsWith("editar ") || nombre.StartsWith("modificar "))
                            {
                                fila.IdEditar = accion.id_permiso;
                                fila.Editar = asignados.Contains(accion.id_permiso);
                            }
                            else if (nombre.StartsWith("eliminar ") || nombre.StartsWith("borrar "))
                            {
                                fila.IdEliminar = accion.id_permiso;
                                fila.Eliminar = asignados.Contains(accion.id_permiso);
                            }
                        }

                        if (asignados.Contains(sub.id_permiso) && fila.IdVer == null)
                        {
                            fila.IdVer = sub.id_permiso;
                            fila.Ver = true;
                        }

                        grupo.Submodulos.Add(fila);
                    }

                    if (grupo.Submodulos.Count > 0)
                        grupos.Add(grupo);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al construir matriz de permisos: " + ex.Message);
            }

            return grupos;
        }

        public List<int> ObtenerPermisosConPadres(List<int> idsSeleccionados)
        {
            if (idsSeleccionados == null || idsSeleccionados.Count == 0)
                return new List<int>();

            HashSet<int> resultado = new HashSet<int>(idsSeleccionados);

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    cn.Open();

                    string query = "SELECT id_permiso, Padre_id FROM PERMISOS";
                    SqlCommand cmd = new SqlCommand(query, cn);
                    SqlDataReader dr = cmd.ExecuteReader();

                    Dictionary<int, int?> mapaPadres = new Dictionary<int, int?>();
                    while (dr.Read())
                    {
                        int id = Convert.ToInt32(dr["id_permiso"]);
                        int? padreId = dr["Padre_id"] != DBNull.Value
                            ? Convert.ToInt32(dr["Padre_id"])
                            : (int?)null;
                        mapaPadres[id] = padreId;
                    }
                    dr.Close();

                    foreach (int id in idsSeleccionados)
                    {
                        int? actual = id;
                        int saltos = 0;

                        while (actual.HasValue && mapaPadres.ContainsKey(actual.Value) && saltos < 10)
                        {
                            resultado.Add(actual.Value);
                            actual = mapaPadres[actual.Value];
                            saltos++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener permisos con padres: " + ex.Message);
            }

            return resultado.ToList();
        }
    }
}