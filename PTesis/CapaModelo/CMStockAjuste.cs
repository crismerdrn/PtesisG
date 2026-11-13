using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMStockAjuste
    {
        public int id_item { get; set; }
        public string tipo_item { get; set; } // "PRODUCTO" o "INSUMO"
        public int id_polleria { get; set; }
        public decimal nuevo_stock { get; set; }
        public int id_usuario { get; set; }
        public string observaciones { get; set; }
        public decimal stock_actual { get; set; }

        // Propiedades para mostrar
        public string item_nombre { get; set; }
        public string polleria_nombre { get; set; }
    }
}
