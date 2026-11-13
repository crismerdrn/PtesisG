using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMRespuestaVentaStock
    {
        public bool Resultado { get; set; }
        public string Mensaje { get; set; }
        public int IdVentaGenerada { get; set; }
        public string NumeroVenta { get; set; }
        public decimal TotalVenta { get; set; }
        public DateTime FechaVenta { get; set; }

        public List<CMValidacionStock> ValidacionesStock { get; set; }
        public bool StockSuficiente { get; set; }

        public CMRespuestaVentaStock()
        {
            ValidacionesStock = new List<CMValidacionStock>();
            StockSuficiente = true;
        }
    }
}
