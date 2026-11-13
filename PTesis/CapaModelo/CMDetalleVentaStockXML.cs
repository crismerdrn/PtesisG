using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CapaModelo;

namespace CapaModelo
{
    public class CMDetalleVentaStockXML
    {
        [XmlRoot("Detalles")]
        public class DetallesVentaStockXML
        {
            [XmlElement("Detalle")]
            public List<DetalleVentaStockXML> Detalles { get; set; }

            public DetallesVentaStockXML()
            {
                Detalles = new List<DetalleVentaStockXML>();
            }
        }

        [XmlType("Detalle")]
        public class DetalleVentaStockXML   // 👈 nombre distinto
        {
            [XmlAttribute("IdProducto")]
            public int IdProducto { get; set; }

            [XmlAttribute("Cantidad")]
            public decimal Cantidad { get; set; }

            [XmlAttribute("PrecioUnitario")]
            public decimal PrecioUnitario { get; set; }

            [XmlAttribute("Subtotal")]
            public decimal Subtotal { get; set; }

            [XmlAttribute("ValidadoStock")]
            public bool ValidadoStock { get; set; }

            [XmlAttribute("StockDisponible")]
            public decimal StockDisponible { get; set; }

            [XmlAttribute("NombreProducto")]
            public string NombreProducto { get; set; } = "";
        }
    }
}
