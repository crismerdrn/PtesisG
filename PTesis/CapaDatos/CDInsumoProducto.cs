using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaDatos.Utilidades;

namespace CapaDatos
{
    public class CDInsumoProducto
    {
        #region Métodos Helper
        private CMInsumoProducto CrearInsumoProductoDesdeLector(SqlDataReader dr)
        {
            var insumoProducto = new CMInsumoProducto
            {
                id_insumo_producto = Convert.ToInt32(dr["id_insumo_producto"]),
                PRODUCTO_id_producto = Convert.ToInt32(dr["PRODUCTO_id_producto"]),
                INSUMO_id_insumo = Convert.ToInt32(dr["INSUMO_id_insumo"]),
                cantidad_requerida = Convert.ToDecimal(dr["cantidad_requerida"]),
                observaciones = dr["observaciones"] != DBNull.Value ? dr["observaciones"].ToString() : null,
                estado = Convert.ToBoolean(dr["estado"]),
                fecha_registro = Convert.ToDateTime(dr["fecha_registro"]),
                fecha_actualizacion = dr["fecha_actualizacion"] != DBNull.Value ?
                    Convert.ToDateTime(dr["fecha_actualizacion"]) : (DateTime?)null
            };

            if (dr.HasColumn("NombreProducto"))
                insumoProducto.NombreProducto = dr["NombreProducto"].ToString();

            if (dr.HasColumn("NombreInsumo"))
                insumoProducto.NombreInsumo = dr["NombreInsumo"].ToString();

            if (dr.HasColumn("DescripcionInsumo"))
                insumoProducto.DescripcionInsumo = dr["DescripcionInsumo"].ToString();

            if (dr.HasColumn("MedidaInsumo"))
                insumoProducto.MedidaInsumo = dr["MedidaInsumo"].ToString();

            // Solo asignar AbreviaturaMedida si la columna existe en el DataReader
            if (dr.HasColumn("AbreviaturaMedida"))
                insumoProducto.AbreviaturaMedida = dr["AbreviaturaMedida"].ToString();

            if (dr.HasColumn("PrecioVenta"))
                insumoProducto.PrecioVentaProducto = Convert.ToDecimal(dr["PrecioVenta"]);

            if (dr.HasColumn("StockActual") && dr["StockActual"] != DBNull.Value)
                insumoProducto.StockActual = Convert.ToDecimal(dr["StockActual"]);

            if (dr.HasColumn("StockMinimo") && dr["StockMinimo"] != DBNull.Value)
                insumoProducto.StockMinimo = Convert.ToDecimal(dr["StockMinimo"]);

            return insumoProducto;
        }

        private CMDetalleCostoProduccion CrearDetalleCostoDesdeLector(SqlDataReader dr)
        {
            return new CMDetalleCostoProduccion
            {
                id_insumo_producto = Convert.ToInt32(dr["id_insumo_producto"]),
                NombreInsumo = dr["NombreInsumo"].ToString(),
                cantidad_requerida = Convert.ToDecimal(dr["cantidad_requerida"]),
                MedidaInsumo = dr["MedidaInsumo"].ToString(),
                AbreviaturaMedida = dr["AbreviaturaMedida"].ToString(),
                UltimoPrecioCompra = Convert.ToDecimal(dr["UltimoPrecioCompra"]),
                PrecioPromedio3Meses = Convert.ToDecimal(dr["PrecioPromedio3Meses"]),
                CostoInsumo = Convert.ToDecimal(dr["CostoInsumo"]),
                StockActual = dr["StockActual"] != DBNull.Value ?
                    Convert.ToDecimal(dr["StockActual"]) : (decimal?)null,
                UnidadesProducibles = dr["UnidadesProducibles"] != DBNull.Value ?
                    Convert.ToInt32(dr["UnidadesProducibles"]) : (int?)null
            };
        }

        private CMValidacionStocks CrearValidacionStockDesdeLector(SqlDataReader dr)
        {
            return new CMValidacionStocks
            {
                IdInsumo = Convert.ToInt32(dr["IdInsumo"]),
                NombreInsumo = dr["NombreInsumo"].ToString(),
                CantidadRequerida = Convert.ToDecimal(dr["CantidadRequerida"]),
                StockActual = Convert.ToDecimal(dr["StockActual"]),
                CantidadNecesaria = Convert.ToDecimal(dr["CantidadNecesaria"]),
                Suficiente = Convert.ToBoolean(dr["Suficiente"]),
                UnidadesPosibles = Convert.ToInt32(dr["UnidadesPosibles"])
            };
        }
        #endregion

        #region CRUD Básico
        public bool AgregarInsumoProducto(CMInsumoProducto insumoProducto, out string mensaje)
        {
            mensaje = "";
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_InsumoProducto_Agregar", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                // Parámetros de entrada
                cmd.Parameters.AddWithValue("@IdProducto", insumoProducto.PRODUCTO_id_producto);
                cmd.Parameters.AddWithValue("@IdInsumo", insumoProducto.INSUMO_id_insumo);
                cmd.Parameters.AddWithValue("@CantidadRequerida", insumoProducto.cantidad_requerida);
                cmd.Parameters.AddWithValue("@Observaciones",
                    string.IsNullOrEmpty(insumoProducto.observaciones) ? (object)DBNull.Value : insumoProducto.observaciones);

                // Parámetros de salida
                cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                mensaje = cmd.Parameters["@Mensaje"].Value.ToString();
            }

            return resultado;
        }

        public bool RegistrarRecetaMultiple(int idProducto, List<CMInsumoProducto> listaInsumos, out string mensaje)
        {
            mensaje = "";
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                cn.Open();
                SqlTransaction transaccion = cn.BeginTransaction();

                try
                {
                    foreach (var item in listaInsumos)
                    {
                        string queryVerificar = @"SELECT id_insumo_producto 
                                         FROM INSUMO_PRODUCTO 
                                         WHERE PRODUCTO_id_producto = @IdProducto AND INSUMO_id_insumo = @IdInsumo";

                        int idExistente = 0;

                        using (SqlCommand cmdVerif = new SqlCommand(queryVerificar, cn, transaccion))
                        {
                            cmdVerif.Parameters.AddWithValue("@IdProducto", idProducto);
                            cmdVerif.Parameters.AddWithValue("@IdInsumo", item.INSUMO_id_insumo);

                            object result = cmdVerif.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                idExistente = Convert.ToInt32(result);
                            }
                        }

                        if (idExistente > 0)
                        {
                            // Si ya existía la relación previa, se actualiza cantidad, observación y se reactiva
                            string queryUpdate = @"UPDATE INSUMO_PRODUCTO 
                                           SET cantidad_requerida = @Cantidad, 
                                               observaciones = @Obs, 
                                               estado = 1, 
                                               fecha_actualizacion = GETDATE() 
                                           WHERE id_insumo_producto = @Id";

                            using (SqlCommand cmdUpd = new SqlCommand(queryUpdate, cn, transaccion))
                            {
                                cmdUpd.Parameters.AddWithValue("@Cantidad", item.cantidad_requerida);
                                cmdUpd.Parameters.AddWithValue("@Obs", string.IsNullOrEmpty(item.observaciones) ? (object)DBNull.Value : item.observaciones);
                                cmdUpd.Parameters.AddWithValue("@Id", idExistente);
                                cmdUpd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Si es nuevo en la receta, se inserta
                            string queryInsert = @"INSERT INTO INSUMO_PRODUCTO 
                                           (PRODUCTO_id_producto, INSUMO_id_insumo, cantidad_requerida, observaciones, estado, fecha_registro) 
                                           VALUES (@IdProducto, @IdInsumo, @Cantidad, @Obs, 1, GETDATE())";

                            using (SqlCommand cmdIns = new SqlCommand(queryInsert, cn, transaccion))
                            {
                                cmdIns.Parameters.AddWithValue("@IdProducto", idProducto);
                                cmdIns.Parameters.AddWithValue("@IdInsumo", item.INSUMO_id_insumo);
                                cmdIns.Parameters.AddWithValue("@Cantidad", item.cantidad_requerida);
                                cmdIns.Parameters.AddWithValue("@Obs", string.IsNullOrEmpty(item.observaciones) ? (object)DBNull.Value : item.observaciones);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }

                    transaccion.Commit();
                    resultado = true;
                    mensaje = "Receta registrada exitosamente.";
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();
                    resultado = false;
                    mensaje = "Error al registrar la receta: " + ex.Message;
                }
            }

            return resultado;
        }

        public bool ActualizarInsumoProducto(CMInsumoProducto insumoProducto, out string mensaje)
        {
            System.Diagnostics.Debug.WriteLine($"=== ActualizarInsumoProducto ===");
            System.Diagnostics.Debug.WriteLine($"ID: {insumoProducto.id_insumo_producto}");
            System.Diagnostics.Debug.WriteLine($"Insumo ID: {insumoProducto.INSUMO_id_insumo}");
            System.Diagnostics.Debug.WriteLine($"Cantidad: {insumoProducto.cantidad_requerida}");
            System.Diagnostics.Debug.WriteLine($"Estado: {insumoProducto.estado}");

            mensaje = "";
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_InsumoProducto_Actualizar", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                // Parámetros de entrada
                cmd.Parameters.AddWithValue("@IdInsumoProducto", insumoProducto.id_insumo_producto);
                cmd.Parameters.AddWithValue("@CantidadRequerida", insumoProducto.cantidad_requerida);
                cmd.Parameters.AddWithValue("@Observaciones",
                    string.IsNullOrEmpty(insumoProducto.observaciones) ? (object)DBNull.Value : insumoProducto.observaciones);
                cmd.Parameters.AddWithValue("@Estado", insumoProducto.estado);
                cmd.Parameters.AddWithValue("@IdInsumo", insumoProducto.INSUMO_id_insumo); // NUEVO PARÁMETRO

                cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                mensaje = cmd.Parameters["@Mensaje"].Value.ToString();

                System.Diagnostics.Debug.WriteLine($"Resultado SP: {resultado}");
                System.Diagnostics.Debug.WriteLine($"Mensaje SP: {mensaje}");
            }

            return resultado;
        }                       

        public bool EliminarInsumoProducto(int idInsumoProducto, out string mensaje)
        {
            mensaje = "";
            bool resultado = false;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_InsumoProducto_Eliminar", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdInsumoProducto", idInsumoProducto);

                cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                cn.Open();
                cmd.ExecuteNonQuery();

                resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                mensaje = cmd.Parameters["@Mensaje"].Value.ToString();
            }

            return resultado;
        }
        #endregion

        #region Consultas - MÉTODO CORREGIDO
        public CMInsumoProducto ObtenerPorId(int idInsumoProducto)
        {
            System.Diagnostics.Debug.WriteLine($"=== CDInsumoProducto.ObtenerPorId ===");
            System.Diagnostics.Debug.WriteLine($"Buscando ID: {idInsumoProducto}");

            CMInsumoProducto insumoProducto = null;

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                SELECT 
                    ip.*,
                    p.Nombre AS NombreProducto,
                    i.nombre AS NombreInsumo,
                    i.descripcion AS DescripcionInsumo,
                    COALESCE(m.Descripcion, 'Sin medida') AS MedidaInsumo,
                    COALESCE(m.Abreviatura, '') AS AbreviaturaMedida,
                    p.PrecioVenta AS PrecioVenta,
                    p.Descripcion AS DescripcionProducto
                FROM INSUMO_PRODUCTO ip
                INNER JOIN PRODUCTO p ON ip.PRODUCTO_id_producto = p.id_producto
                INNER JOIN INSUMO i ON ip.INSUMO_id_insumo = i.id_insumo
                LEFT JOIN MEDIDAS m ON i.MEDIDAS_id_medida = m.id_medida
                WHERE ip.id_insumo_producto = @IdInsumoProducto";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.Parameters.AddWithValue("@IdInsumoProducto", idInsumoProducto);

                    cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        System.Diagnostics.Debug.WriteLine($"Registro encontrado en la base de datos");

                        // CREAR OBJETO CON TODOS LOS CAMPOS
                        insumoProducto = new CMInsumoProducto
                        {
                            // Campos principales
                            id_insumo_producto = Convert.ToInt32(dr["id_insumo_producto"]),
                            PRODUCTO_id_producto = Convert.ToInt32(dr["PRODUCTO_id_producto"]),
                            INSUMO_id_insumo = Convert.ToInt32(dr["INSUMO_id_insumo"]),
                            cantidad_requerida = Convert.ToDecimal(dr["cantidad_requerida"]),
                            observaciones = dr["observaciones"] != DBNull.Value ? dr["observaciones"].ToString() : "",
                            estado = Convert.ToBoolean(dr["estado"]),
                            fecha_registro = Convert.ToDateTime(dr["fecha_registro"]),
                            fecha_actualizacion = dr["fecha_actualizacion"] != DBNull.Value ?
                                Convert.ToDateTime(dr["fecha_actualizacion"]) : (DateTime?)null,

                            // Campos de JOIN
                            NombreProducto = dr["NombreProducto"].ToString(),
                            NombreInsumo = dr["NombreInsumo"].ToString(),
                            DescripcionInsumo = dr["DescripcionInsumo"].ToString(),
                            MedidaInsumo = dr["MedidaInsumo"].ToString(),
                            AbreviaturaMedida = dr["AbreviaturaMedida"].ToString(),
                            PrecioVentaProducto = Convert.ToDecimal(dr["PrecioVenta"]),
                            DescripcionProducto = dr["DescripcionProducto"].ToString()
                        };

                        System.Diagnostics.Debug.WriteLine($"Objeto creado exitosamente:");
                        System.Diagnostics.Debug.WriteLine($"  - ID: {insumoProducto.id_insumo_producto}");
                        System.Diagnostics.Debug.WriteLine($"  - Producto: {insumoProducto.NombreProducto}");
                        System.Diagnostics.Debug.WriteLine($"  - Insumo: {insumoProducto.NombreInsumo}");
                        System.Diagnostics.Debug.WriteLine($"  - Cantidad: {insumoProducto.cantidad_requerida}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"NO se encontró registro con ID: {idInsumoProducto}");
                    }

                    dr.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en ObtenerPorId: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            return insumoProducto;
        }

        public List<CMInsumoProducto> ObtenerPorProducto(int idProducto)
        {
            System.Diagnostics.Debug.WriteLine($"CDInsumoProducto.ObtenerPorProducto({idProducto})");

            List<CMInsumoProducto> lista = new List<CMInsumoProducto>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    // Verifica el nombre correcto del stored procedure
                    SqlCommand cmd = new SqlCommand("sp_InsumoProducto_ObtenerPorProducto", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);

                    System.Diagnostics.Debug.WriteLine($"Ejecutando SP con IdProducto: {idProducto}");

                    cn.Open();

                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        var item = CrearInsumoProductoDesdeLector(dr);

                        System.Diagnostics.Debug.WriteLine($"  - Mapeado: ID={item.id_insumo_producto}, " +
                                                          $"Producto={item.NombreProducto ?? "NULO"}, " +
                                                          $"Insumo={item.NombreInsumo ?? "NULO"}");

                        lista.Add(item);
                    }

                    dr.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en ObtenerPorProducto: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"Total obtenido: {lista.Count}");
            return lista;
        }

        public List<CMInsumoProducto> ObtenerPorInsumo(int idInsumo)
        {
            List<CMInsumoProducto> lista = new List<CMInsumoProducto>();

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_InsumoProducto_ObtenerPorInsumo", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdInsumo", idInsumo);

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(CrearInsumoProductoDesdeLector(dr));
                }
            }

            return lista;
        }
        #endregion

        #region Cálculos y Análisis
        public Tuple<CMCostoProduccion, List<CMDetalleCostoProduccion>> CalcularCostoProduccion(int idProducto, int? idPolleria)
        {
            CMCostoProduccion resumen = null;
            List<CMDetalleCostoProduccion> detalles = new List<CMDetalleCostoProduccion>();

            using (SqlConnection conexion = new SqlConnection(Conexion.CN))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsumoProducto_CalcularCostoProduccion", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                    cmd.Parameters.AddWithValue("@IdPolleria", (object)idPolleria ?? DBNull.Value);

                    conexion.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // Result Set 1: Detalles de Insumos
                        while (dr.Read())
                        {
                            detalles.Add(new CMDetalleCostoProduccion()
                            {
                                id_insumo_producto = Convert.ToInt32(dr["id_insumo_producto"]),
                                NombreInsumo = dr["NombreInsumo"].ToString(),
                                cantidad_requerida = Convert.ToDecimal(dr["cantidad_requerida"]),
                                MedidaInsumo = dr["MedidaInsumo"].ToString(),
                                AbreviaturaMedida = dr["AbreviaturaMedida"].ToString(),
                                UltimoPrecioCompra = Convert.ToDecimal(dr["UltimoPrecioCompra"]),
                                PrecioPromedio3Meses = Convert.ToDecimal(dr["PrecioPromedio3Meses"]),
                                CostoInsumo = Convert.ToDecimal(dr["CostoInsumo"]),
                                StockActual = dr["StockActual"] != DBNull.Value ? Convert.ToDecimal(dr["StockActual"]) : (decimal?)null,
                                UnidadesProducibles = dr["UnidadesProducibles"] != DBNull.Value ? Convert.ToInt32(dr["UnidadesProducibles"]) : (int?)null
                            });
                        }

                        // Result Set 2: Resumen del Producto
                        if (dr.NextResult() && dr.Read())
                        {
                            resumen = new CMCostoProduccion()
                            {
                                id_producto = Convert.ToInt32(dr["IdProducto"]),
                                NombreProducto = dr["NombreProducto"].ToString(),
                                PrecioVenta = Convert.ToDecimal(dr["PrecioVenta"]),
                                CostoTotalProduccion = Convert.ToDecimal(dr["CostoTotalProduccion"]),
                                GananciaUnitaria = Convert.ToDecimal(dr["GananciaUnitaria"]),
                                PorcentajeMargen = Convert.ToDecimal(dr["PorcentajeMargen"]),
                                TotalInsumos = Convert.ToInt32(dr["TotalInsumos"]),
                                UnidadesMaximasProducibles = dr["UnidadesMaximasProducibles"] != DBNull.Value ? Convert.ToInt32(dr["UnidadesMaximasProducibles"]) : (int?)null
                            };
                        }
                    }
                }
            }

            return Tuple.Create(resumen, detalles);
        }

        public Tuple<bool, string, int, List<CMValidacionStocks>, CMResumenValidacionStock> ValidarStockProduccion(
            int idProducto, int cantidadProducir, int idPolleria)
        {
            bool resultado = false;
            string mensaje = "";
            int unidadesPosibles = 0;
            List<CMValidacionStocks> detalles = new List<CMValidacionStocks>();
            CMResumenValidacionStock resumen = null;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand("sp_InsumoProducto_ValidarStockProduccion", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                cmd.Parameters.AddWithValue("@CantidadProducir", cantidadProducir);
                cmd.Parameters.AddWithValue("@IdPolleria", idPolleria);

                // Parámetros de salida
                cmd.Parameters.Add("@Resultado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@Mensaje", SqlDbType.VarChar, 1000).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@UnidadesPosibles", SqlDbType.Int).Direction = ParameterDirection.Output;

                cn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    // Leer detalles de validación
                    while (dr.Read())
                    {
                        detalles.Add(CrearValidacionStockDesdeLector(dr));
                    }

                    // Leer resumen
                    if (dr.NextResult() && dr.Read())
                    {
                        resumen = new CMResumenValidacionStock
                        {
                            StockSuficiente = Convert.ToBoolean(dr["StockSuficiente"]),
                            MensajeValidacion = dr["MensajeValidacion"].ToString(),
                            UnidadesMaximasProducibles = Convert.ToInt32(dr["UnidadesMaximasProducibles"]),
                            CantidadSolicitada = Convert.ToInt32(dr["CantidadSolicitada"]),
                            Recomendacion = dr["Recomendacion"].ToString()
                        };
                    }
                }

                // Obtener valores de salida
                resultado = Convert.ToBoolean(cmd.Parameters["@Resultado"].Value);
                mensaje = cmd.Parameters["@Mensaje"].Value?.ToString() ?? "";
                unidadesPosibles = Convert.ToInt32(cmd.Parameters["@UnidadesPosibles"].Value);
            }

            return Tuple.Create(resultado, mensaje, unidadesPosibles, detalles, resumen);
        }
        #endregion

        #region Métodos de Extensión (Helpers)
        public bool ExisteRelacion(int idProducto, int idInsumo)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                string query = @"
            SELECT COUNT(*) 
            FROM INSUMO_PRODUCTO 
            WHERE PRODUCTO_id_producto = @IdProducto 
            AND INSUMO_id_insumo = @IdInsumo";
                // Quitamos "AND estado = 1" para buscar también relaciones inactivas

                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                cmd.Parameters.AddWithValue("@IdInsumo", idInsumo);

                cn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                System.Diagnostics.Debug.WriteLine($"ExisteRelacion - Producto:{idProducto}, Insumo:{idInsumo}, Count:{count}");

                return count > 0;
            }
        }

        // NUEVO MÉTODO: Para verificar si existe una relación ACTIVA
        public bool ExisteRelacionActiva(int idProducto, int idInsumo)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                string query = @"
            SELECT COUNT(*) 
            FROM INSUMO_PRODUCTO 
            WHERE PRODUCTO_id_producto = @IdProducto 
            AND INSUMO_id_insumo = @IdInsumo 
            AND estado = 1";

                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                cmd.Parameters.AddWithValue("@IdInsumo", idInsumo);

                cn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                return count > 0;
            }
        }

        // NUEVO MÉTODO: Para obtener una relación existente (activa o inactiva)
        public CMInsumoProducto ObtenerRelacionExistente(int idProducto, int idInsumo)
        {
            CMInsumoProducto insumoProducto = null;

            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                string query = @"
            SELECT TOP 1 * 
            FROM INSUMO_PRODUCTO 
            WHERE PRODUCTO_id_producto = @IdProducto 
            AND INSUMO_id_insumo = @IdInsumo";

                SqlCommand cmd = new SqlCommand(query, cn);
                cmd.Parameters.AddWithValue("@IdProducto", idProducto);
                cmd.Parameters.AddWithValue("@IdInsumo", idInsumo);

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    insumoProducto = new CMInsumoProducto
                    {
                        id_insumo_producto = Convert.ToInt32(dr["id_insumo_producto"]),
                        PRODUCTO_id_producto = Convert.ToInt32(dr["PRODUCTO_id_producto"]),
                        INSUMO_id_insumo = Convert.ToInt32(dr["INSUMO_id_insumo"]),
                        cantidad_requerida = Convert.ToDecimal(dr["cantidad_requerida"]),
                        observaciones = dr["observaciones"] != DBNull.Value ? dr["observaciones"].ToString() : "",
                        estado = Convert.ToBoolean(dr["estado"]),
                        fecha_registro = Convert.ToDateTime(dr["fecha_registro"]),
                        fecha_actualizacion = dr["fecha_actualizacion"] != DBNull.Value ?
                            Convert.ToDateTime(dr["fecha_actualizacion"]) : (DateTime?)null
                    };
                }

                dr.Close();
            }

            return insumoProducto;
        }

        public int ContarInsumosPorProducto(int idProducto)
        {
            using (SqlConnection cn = new SqlConnection(Conexion.CN))
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM INSUMO_PRODUCTO " +
                    "WHERE PRODUCTO_id_producto = @IdProducto " +
                    "AND estado = 1", cn);
                cmd.Parameters.AddWithValue("@IdProducto", idProducto);

                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<CMInsumoProducto> ObtenerTodos()
        {
            List<CMInsumoProducto> lista = new List<CMInsumoProducto>();

            try
            {
                using (SqlConnection cn = new SqlConnection(Conexion.CN))
                {
                    string query = @"
                SELECT 
                    ip.id_insumo_producto,
                    ip.PRODUCTO_id_producto,
                    ip.INSUMO_id_insumo,
                    ip.cantidad_requerida,
                    ip.observaciones,
                    ip.estado,
                    ip.fecha_registro,
                    ip.fecha_actualizacion,
                    p.Nombre AS NombreProducto,
                    p.PrecioVenta AS PrecioVenta,
                    i.nombre AS NombreInsumo,
                    i.descripcion AS DescripcionInsumo,
                    COALESCE(m.Descripcion, 'Sin medida') AS MedidaInsumo,
                    COALESCE(m.Abreviatura, '') AS AbreviaturaMedida
                FROM INSUMO_PRODUCTO ip
                INNER JOIN PRODUCTO p ON ip.PRODUCTO_id_producto = p.id_producto
                INNER JOIN INSUMO i ON ip.INSUMO_id_insumo = i.id_insumo
                LEFT JOIN MEDIDAS m ON i.MEDIDAS_id_medida = m.id_medida
                ORDER BY p.Nombre ASC, i.nombre ASC";

                    SqlCommand cmd = new SqlCommand(query, cn);
                    cmd.CommandType = CommandType.Text;

                    cn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new CMInsumoProducto
                            {
                                id_insumo_producto = Convert.ToInt32(dr["id_insumo_producto"]),
                                PRODUCTO_id_producto = Convert.ToInt32(dr["PRODUCTO_id_producto"]),
                                INSUMO_id_insumo = Convert.ToInt32(dr["INSUMO_id_insumo"]),
                                cantidad_requerida = Convert.ToDecimal(dr["cantidad_requerida"]),
                                observaciones = dr["observaciones"] != DBNull.Value ? dr["observaciones"].ToString() : "",
                                estado = Convert.ToBoolean(dr["estado"]),
                                fecha_registro = Convert.ToDateTime(dr["fecha_registro"]),
                                fecha_actualizacion = dr["fecha_actualizacion"] != DBNull.Value ? Convert.ToDateTime(dr["fecha_actualizacion"]) : (DateTime?)null,
                                NombreProducto = dr["NombreProducto"].ToString(),
                                NombreInsumo = dr["NombreInsumo"].ToString(),
                                DescripcionInsumo = dr["DescripcionInsumo"] != DBNull.Value ? dr["DescripcionInsumo"].ToString() : "",
                                MedidaInsumo = dr["MedidaInsumo"].ToString(),
                                AbreviaturaMedida = dr["AbreviaturaMedida"].ToString(),
                                PrecioVentaProducto = Convert.ToDecimal(dr["PrecioVenta"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en ObtenerTodos: {ex.Message}");
            }

            return lista;
        }
        #endregion
    }
}