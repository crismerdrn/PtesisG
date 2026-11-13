using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMProductoStock
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public string Medida { get; set; }
        public string Abreviatura { get; set; }
        public string TipoProducto { get; set; }
        public int IdPolleria { get; set; }
        public string NombrePolleria { get; set; }
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public string EstadoStock { get; set; }
        public DateTime FechaUltimoMovimiento { get; set; }
        public bool TieneStockSuficiente { get; set; }
        public string ClaseCSS
        {
            get
            {
                switch (EstadoStock)
                {
                    case "AGOTADO":
                        return "text-danger";

                    case "BAJO":
                        return "text-warning";

                    case "DISPONIBLE":
                        return "text-success";

                    default:
                        return "text-info";
                }
            }
        }

        public string IconoEstado
        {
            get
            {
                switch (EstadoStock)
                {
                    case "AGOTADO":
                        return "text-danger";

                    case "BAJO":
                        return "text-warning";

                    case "DISPONIBLE":
                        return "text-success";

                    default:
                        return "text-info";
                }
            }
        }

        public bool PuedeVenderse => StockActual > 0;
        public string TextoStock => $"{StockActual} {Abreviatura}";
        public string TextoAlerta
        {
            get
            {
                if (StockActual <= 0) return "AGOTADO - Reponer urgentemente";
                if (StockActual <= StockMinimo) return "Stock bajo - Considerar reponer";
                return "Stock suficiente";
            }
        }
    }
}
