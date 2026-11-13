using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMReportStock
    {
        public string TipoItem { get; set; } // INSUMO o PRODUCTO
        public int IdItem { get; set; }
        public string NombreItem { get; set; }
        public string Medida { get; set; }
        public string Abreviatura { get; set; }
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public string EstadoStock { get; set; }
        public DateTime? UltimoMovimiento { get; set; }
    }

    public class CMKardexMovimientoItem
    {
        public int IdMovimiento { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string TipoMovimiento { get; set; }
        public string TipoItem { get; set; }
        public string NombreItem { get; set; }
        public decimal Cantidad { get; set; }
        public decimal StockAnterior { get; set; }
        public decimal StockNuevo { get; set; }
        public string Observaciones { get; set; }
        public string Usuario { get; set; }
    }

    public class CMReporteStockViewModel
    {
        public int TotalItems { get; set; }
        public int TotalAgotados { get; set; }
        public int TotalBajos { get; set; }
        public int TotalOptimos { get; set; }

        public List<CMReportStock> Inventario { get; set; }
        public List<CMKardexMovimientoItem> Movimientos { get; set; }

        public CMReporteStockViewModel()
        {
            Inventario = new List<CMReportStock>();
            Movimientos = new List<CMKardexMovimientoItem>();
        }
    }

}
