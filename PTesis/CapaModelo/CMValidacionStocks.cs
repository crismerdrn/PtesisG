using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMValidacionStock
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public decimal CantidadSolicitada { get; set; }
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public bool Suficiente { get; set; }
        public string Mensaje { get; set; }
        public string AbreviaturaMedida { get; set; }
        public string Estado { get; set; }
        public DateTime FechaValidacion { get; set; }

        public CMValidacionStock()
        {
            FechaValidacion = DateTime.Now;
        }
    }
}
