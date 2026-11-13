using CapaModelo;
using Dapper;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace CapaDatos
{
    public class CDReportStock
    {
        public CMReporteStockViewModel ObtenerReporteStock(
            int? idPolleria = null,
            string tipoFiltro = "TODOS",
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            var vm = new CMReporteStockViewModel();

            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();
                    var parametros = new
                    {
                        IdPolleria = idPolleria,
                        TipoFiltro = tipoFiltro ?? "TODOS",
                        FechaDesde = fechaDesde,
                        FechaHasta = fechaHasta
                    };

                    using (var multi = conexion.QueryMultiple("sp_Reporte_Stock", parametros, commandType: CommandType.StoredProcedure))
                    {
                        vm.Inventario = multi.Read<CMReportStock>().ToList();
                        vm.Movimientos = multi.Read<CMKardexMovimientoItem>().ToList();
                    }

                    vm.TotalItems = vm.Inventario.Count;
                    vm.TotalAgotados = vm.Inventario.Count(x => x.EstadoStock == "AGOTADO");
                    vm.TotalBajos = vm.Inventario.Count(x => x.EstadoStock == "STOCK BAJO");
                    vm.TotalOptimos = vm.Inventario.Count(x => x.EstadoStock == "OPTIMO");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR en CDReportStock.ObtenerReporteStock]: {ex.Message}");
            }

            return vm;
        }
    }
}