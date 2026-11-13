using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace CapaDatos
{
    public class CDTipoProducto
    {
        public List<CMTipoProducto> ListarTipoProductos()
        {
            List<CMTipoProducto> lista = new List<CMTipoProducto>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    if (cn == null)
                        throw new ArgumentNullException("La conexión a la base de datos no está configurada.");

                    SqlCommand cmd = new SqlCommand("LISTAR_TipoProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        // Validar valores nulos
                        lista.Add(new CMTipoProducto
                        {
                            id_tipoProducto = dr["id_tipoProducto"] != DBNull.Value ?
                                Convert.ToInt32(dr["id_tipoProducto"]) : 0,
                            descripcion = dr["descripcion"] != DBNull.Value ?
                                dr["descripcion"].ToString() : string.Empty,
                            estado = dr["estado"] != DBNull.Value ?
                                Convert.ToBoolean(dr["estado"]) : false,
                            fecha_registro = dr["fecha_registro"] != DBNull.Value ?
                                Convert.ToDateTime(dr["fecha_registro"]) : DateTime.MinValue
                        });
                    }
                    dr.Close();
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error SQL: {ex.Message}");
                throw new Exception("Error al acceder a la base de datos.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general: {ex.Message}");
                throw;
            }

            return lista;
        }

        public bool AgregarTipoProducto(CMTipoProducto tipoProducto)
        {
            if (tipoProducto == null)
                throw new ArgumentNullException(nameof(tipoProducto), "El tipo de producto no puede ser nulo.");
            if (string.IsNullOrWhiteSpace(tipoProducto.descripcion))
                throw new ArgumentException("La descripción es obligatoria.");

            if (tipoProducto.descripcion.Length > 100)
                throw new ArgumentException("La descripción no puede exceder los 100 caracteres.");

            bool resultado = false;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("ADICIONAR_TipoProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@descripcion", SqlDbType.VarChar, 100).Value =
                        tipoProducto.descripcion ?? (object)DBNull.Value;
                    cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = tipoProducto.estado;

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) 
                    throw new Exception("Ya existe un tipo de producto con esa descripción.");
                else if (ex.Number == 547)
                    throw new Exception("No se puede agregar debido a restricciones de integridad.");
                else
                    throw new Exception($"Error en la base de datos: {ex.Message}");
            }

            return resultado;
        }

        public bool ModificarTipoProducto(CMTipoProducto tipoProducto)
        {
            if (tipoProducto == null)
                throw new ArgumentNullException(nameof(tipoProducto));

            if (tipoProducto.id_tipoProducto <= 0)
                throw new ArgumentException("ID de tipo de producto inválido.");

            if (string.IsNullOrWhiteSpace(tipoProducto.descripcion))
                throw new ArgumentException("La descripción es obligatoria.");

            bool resultado = false;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("MODIFICAR_TipoProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@id_tipoProducto", SqlDbType.Int).Value = tipoProducto.id_tipoProducto;
                    cmd.Parameters.Add("@descripcion", SqlDbType.VarChar, 100).Value = tipoProducto.descripcion;
                    cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = tipoProducto.estado;

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;

                    if (!resultado)
                        throw new Exception("No se encontró el tipo de producto para modificar.");
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                    throw new Exception("Ya existe otro tipo de producto con esa descripción.");
                throw;
            }

            return resultado;
        }

        public bool EliminarTipoProducto(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID inválido para eliminar.");

            bool resultado = false;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("ELIMINAR_TipoProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@id_tipoProducto", SqlDbType.Int).Value = id;

                    cn.Open();
                    resultado = cmd.ExecuteNonQuery() > 0;

                    if (!resultado)
                        throw new Exception("No se encontró el tipo de producto para eliminar.");
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                    throw new Exception("No se puede eliminar porque está siendo utilizado por otros registros.");
                throw;
            }

            return resultado;
        }

        public CMTipoProducto ObtenerTipoProductoPorId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID inválido.");

            CMTipoProducto tipoProducto = null;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    SqlCommand cmd = new SqlCommand("OBTENER_TipoProducto_PorId", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@id_tipoProducto", SqlDbType.Int).Value = id;

                    cn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            tipoProducto = new CMTipoProducto
                            {
                                id_tipoProducto = dr["id_tipoProducto"] != DBNull.Value ?
                                    Convert.ToInt32(dr["id_tipoProducto"]) : 0,
                                descripcion = dr["descripcion"] != DBNull.Value ?
                                    dr["descripcion"].ToString() : string.Empty,
                                estado = dr["estado"] != DBNull.Value ?
                                    Convert.ToBoolean(dr["estado"]) : false,
                                fecha_registro = dr["fecha_registro"] != DBNull.Value ?
                                    Convert.ToDateTime(dr["fecha_registro"]) : DateTime.MinValue
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener tipo de producto: {ex.Message}");
                throw;
            }

            return tipoProducto;
        }
    }
}