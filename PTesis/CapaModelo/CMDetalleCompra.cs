using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMDetalleCompra
    {
        public int id_detalleCompra { get; set; }
        public decimal precioUnitario { get; set; }
        public decimal cantidad { get; set; }
        public decimal subtotal { get; set; }
        public int COMPRA_id_compra { get; set; }
        public int? PRODUCTO_id_producto { get; set; }
        public int? INSUMO_id_insumo { get; set; }

        public string TipoItem { get; set; } 
        public int IdItemSeleccionado { get; set; } 
        public string NombreItem { get; set; }
        public string DescripcionItem { get; set; }
        public string MedidaItem { get; set; }
        public string AbreviaturaMedida { get; set; }
        public decimal StockActual { get; set; }
    }
}
