using CapaModelo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace CapaDatos
{
    public class CDReportCompra
    {
        public CMReporteCompraViewModel ObtenerReporteCompras(
            DateTime fechaInicio,
            DateTime fechaFin,
            int? idPolleria = null,
            int? idProveedor = null,
            string estado = null)
        {
            var vm = new CMReporteCompraViewModel
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                IdPolleria = idPolleria,
                IdProveedor = idProveedor,
                Estado = estado
            };

            try
            {
                using (var conexion = new SqlConnection(Conexion.CN))
                {
                    conexion.Open();
                    var parametros = new
                    {
                        FechaInicio = fechaInicio.Date,
                        FechaFin = fechaFin.Date,
                        IdPolleria = idPolleria,
                        IdProveedor = idProveedor,
                        Estado = string.IsNullOrEmpty(estado) ? null : estado
                    };

                    var items = conexion.Query<CMReportCompras>(
                        "sp_Reporte_Compras",
                        parametros,
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    vm.Compras = items;

                    var completadas = items.Where(c => c.Estado.ToUpper() == "COMPLETADA" || c.Estado.ToUpper() == "COMPLETADO").ToList();
                    vm.CantidadCompras = completadas.Count;
                    vm.TotalInvertido = completadas.Sum(c => c.TotalCompra);
                    vm.CompraPromedio = vm.CantidadCompras > 0 ? (vm.TotalInvertido / vm.CantidadCompras) : 0;
                    vm.TotalItemsComprados = completadas.Sum(c => c.TotalItemsComprados);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR en CDReportCompra.ObtenerReporteCompras]: {ex.Message}");
            }

            return vm;
        }

        
    }
}
