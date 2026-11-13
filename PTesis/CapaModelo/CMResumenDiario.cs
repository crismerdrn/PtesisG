using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMResumenDiario
    {
        public DateTime Fecha { get; set; }
        public int TotalVentas { get; set; }
        public decimal TotalMonto { get; set; }
        public decimal PromedioVenta { get; set; }
        public int ProductosVendidos { get; set; }
        public int ProductosSinStock { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal StockValorTotal { get; set; }
        public TimeSpan PrimeraVenta { get; set; }
        public TimeSpan UltimaVenta { get; set; }
        public string DiaSemana { get; set; }

        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        public string TotalMontoFormateado => TotalMonto.ToString("C2");
        public string PromedioVentaFormateado => PromedioVenta.ToString("C2");
        public string StockValorFormateado => StockValorTotal.ToString("C2");
    }
}
