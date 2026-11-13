using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMReporteVentaItem
    {
        public int IdVenta { get; set; }
        public string NumeroVenta { get; set; }
        public DateTime FechaVenta { get; set; }
        public TimeSpan HoraVenta { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        public string CanalVenta { get; set; }
        public string FormaPago { get; set; }
        public decimal TotalVenta { get; set; }
        public string Estado { get; set; }
        public decimal TotalItemsVendidos { get; set; }
    }

    public class CMReporteVentaViewModel
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int? IdPolleria { get; set; }
        public int? IdTipoVenta { get; set; }
        public int? IdTipoPago { get; set; }

        public int CantidadVentas { get; set; }
        public decimal TotalRecaudado { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal TotalItemsVendidos { get; set; }

        public List<CMReporteVentaItem> Ventas { get; set; }

        public CMReporteVentaViewModel()
        {
            Ventas = new List<CMReporteVentaItem>();
        }
    }
}