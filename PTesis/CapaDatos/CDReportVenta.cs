using CapaModelo;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace CapaDatos
{
    public class CDReportVenta
    {
        public CMReporteVentaViewModel ObtenerReporteVentas(
            DateTime fechaInicio,
            DateTime fechaFin,
            int? idPolleria = null,
            int? idTipoVenta = null,
            int? idTipoPago = null)
        {
            var vm = new CMReporteVentaViewModel
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                IdPolleria = idPolleria,
                IdTipoVenta = idTipoVenta,
                IdTipoPago = idTipoPago
            };

            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();

                    // Parámetros exactos que espera el SP [sp_Reporte_Ventas]
                    var parametros = new
                    {
                        FechaInicio = fechaInicio.Date,
                        FechaFin = fechaFin.Date,
                        IdPolleria = idPolleria,
                        IdTipoVenta = idTipoVenta,
                        IdTipoPago = idTipoPago
                    };

                    var items = conexion.Query<CMReporteVentaItem>(
                        "sp_Reporte_Ventas",
                        parametros,
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    vm.Ventas = items;

                    var completadas = items.Where(v => v.Estado == "COMPLETADA").ToList();
                    vm.CantidadVentas = completadas.Count;
                    vm.TotalRecaudado = completadas.Sum(v => v.TotalVenta);
                    vm.TicketPromedio = vm.CantidadVentas > 0 ? (vm.TotalRecaudado / vm.CantidadVentas) : 0;
                    vm.TotalItemsVendidos = completadas.Sum(v => v.TotalItemsVendidos);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR en CDReportVenta.ObtenerReporteVentas]: {ex.Message}");
            }

            return vm;
        }
    }
}