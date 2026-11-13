using CapaModelo;
using System;
using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class CDDashboard
    {
        public CMDashboardViewModel ObtenerMetricasDashboard(int? idPolleria = null)
        {
            var vm = new CMDashboardViewModel();
            DateTime hoyInicio = DateTime.Today;
            DateTime hoyFin = DateTime.Today.AddDays(1);

            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();

                    // 1. KPI: Total Ventas de Hoy
                    string sqlVentas = @"
                        SELECT ISNULL(SUM(Total_venta), 0) 
                        FROM VENTAS 
                        WHERE Fecha_venta >= @hoyInicio AND Fecha_venta < @hoyFin 
                          AND Estado = 'COMPLETADA'
                          AND (@idPolleria IS NULL OR POLLERIA_id_polleria = @idPolleria)";
                    vm.VentasHoy = conexion.ExecuteScalar<decimal>(sqlVentas, new { hoyInicio, hoyFin, idPolleria });

                    // 2. KPI: Cantidad de Ventas / Pedidos Hoy
                    string sqlPedidos = @"
                        SELECT COUNT(1) 
                        FROM VENTAS 
                        WHERE Fecha_venta >= @hoyInicio AND Fecha_venta < @hoyFin 
                          AND Estado = 'COMPLETADA'
                          AND (@idPolleria IS NULL OR POLLERIA_id_polleria = @idPolleria)";
                    vm.PedidosHoy = conexion.ExecuteScalar<int>(sqlPedidos, new { hoyInicio, hoyFin, idPolleria });

                    // 3. KPI: Total Compras de Hoy
                    string sqlCompras = @"
                        SELECT ISNULL(SUM(MontoTotal), 0) 
                        FROM COMPRA 
                        WHERE FechaCompra >= @hoyInicio AND FechaCompra < @hoyFin 
                          AND Estado = 'COMPLETADA'";
                    vm.ComprasHoy = conexion.ExecuteScalar<decimal>(sqlCompras, new { hoyInicio, hoyFin });

                    // 4. KPI: Conteo de Insumos y Productos con Stock Bajo o Agotado
                    string sqlStockBajo = @"
                        SELECT 
                            (SELECT COUNT(1) FROM STOCK_INSUMO WHERE stock_actual <= stock_minimo AND (@idPolleria IS NULL OR POLLERIA_id_polleria = @idPolleria))
                            +
                            (SELECT COUNT(1) FROM STOCK WHERE stock_actual <= stock_minimo AND (@idPolleria IS NULL OR POLLERIA_id_polleria = @idPolleria))";
                    vm.ArticulosStockBajo = conexion.ExecuteScalar<int>(sqlStockBajo, new { idPolleria });

                    // 5. Últimas 5 Ventas
                    string sqlUltimasVentas = @"
                        SELECT TOP 5 
                            v.id_venta AS IdVenta,
                            v.NumeroVenta,
                            v.Fecha_venta AS FechaVenta,
                            v.hora_venta AS HoraVenta,
                            CASE 
                                WHEN v.CLIENTE_id_cliente IS NULL OR v.CLIENTE_id_cliente = 6 THEN 'CLIENTE GENERAL'
                                ELSE LTRIM(RTRIM(CONCAT(c.Nombre, ' ', c.Apellido)))
                            END AS Cliente,
                            ISNULL(tv.nombre, ISNULL(v.Tipo_venta, 'SALÓN')) AS CanalVenta,
                            ISNULL(v.Total_venta, 0) AS TotalVenta,
                            ISNULL(v.Estado, 'COMPLETADA') AS Estado
                        FROM VENTAS v
                        LEFT JOIN CLIENTE c ON v.CLIENTE_id_cliente = c.id_cliente
                        LEFT JOIN TIPO_VENTA tv ON v.TIPO_VENTA_id_tipoVenta = tv.id_tipoVenta
                        WHERE (@idPolleria IS NULL OR v.POLLERIA_id_polleria = @idPolleria)
                        ORDER BY v.Fecha_venta DESC, v.hora_venta DESC";
                    vm.UltimasVentas = conexion.Query<CMReporteVentaItem>(sqlUltimasVentas, new { idPolleria }).ToList();

                    // 6. Últimas 5 Compras
                    string sqlUltimasCompras = @"
                        SELECT TOP 5 
                            c.id_compra AS IdCompra,
                            CASE 
                                WHEN c.Referencia IS NOT NULL AND LTRIM(RTRIM(c.Referencia)) <> '' THEN c.Referencia
                                ELSE CONCAT('#', c.id_compra)
                            END AS NumeroCompra,
                            c.FechaCompra,
                            CASE 
                                WHEN pr.esEmpresa = 1 AND pr.RazonSocial IS NOT NULL THEN pr.RazonSocial
                                ELSE LTRIM(RTRIM(CONCAT(ISNULL(pr.Nombre, ''), ' ', ISNULL(pr.Apellido, ''))))
                            END AS Proveedor,
                            ISNULL(c.MontoTotal, 0) AS TotalCompra,
                            ISNULL(c.Estado, 'COMPLETADA') AS Estado
                        FROM COMPRA c
                        LEFT JOIN PROVEEDOR pr ON c.PROVEEDOR_id_proveedor = pr.id_proveedor
                        ORDER BY c.FechaCompra DESC, c.id_compra DESC";
                    vm.UltimasCompras = conexion.Query<CMReportCompras>(sqlUltimasCompras).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR en CDDashboard]: {ex.Message}");
            }

            return vm;
        }
    }
}
