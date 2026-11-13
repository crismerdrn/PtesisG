using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMDashboardViewModel
    {
        // KPIs del día
        public decimal VentasHoy { get; set; }
        public decimal ComprasHoy { get; set; }
        public int ArticulosStockBajo { get; set; }
        public int PedidosHoy { get; set; }

        // Listados recientes
        public List<CMReporteVentaItem> UltimasVentas { get; set; }
        public List<CMReportCompras> UltimasCompras { get; set; }

        public CMDashboardViewModel()
        {
            UltimasVentas = new List<CMReporteVentaItem>();
            UltimasCompras = new List<CMReportCompras>();
        }
    }
}
