using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMReportCompras
    {
        public int IdCompra { get; set; }
        public string NumeroCompra { get; set; }
        public DateTime FechaCompra { get; set; }
        public TimeSpan HoraCompra { get; set; }
        public string Proveedor { get; set; }
        public string Comprador { get; set; }
        public string Polleria { get; set; }
        public decimal TotalCompra { get; set; }
        public string Estado { get; set; }
        public decimal TotalItemsComprados { get; set; }
    }

    public class CMReporteCompraViewModel
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int? IdPolleria { get; set; }
        public int? IdProveedor { get; set; }
        public string Estado { get; set; }

        public int CantidadCompras { get; set; }
        public decimal TotalInvertido { get; set; }
        public decimal CompraPromedio { get; set; }
        public decimal TotalItemsComprados { get; set; }

        public List<CMReportCompras> Compras { get; set; }

        public CMReporteCompraViewModel()
        {
            Compras = new List<CMReportCompras>();
        }
    }

}
