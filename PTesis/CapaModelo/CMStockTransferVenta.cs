using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMStockTransferVenta
    {
        public int id_producto { get; set; }
        public int id_polleria_origen { get; set; }
        public int id_polleria_destino { get; set; }
        public decimal cantidad { get; set; }
        public int id_usuario { get; set; }
        public string observaciones { get; set; }

        // Propiedades para mostrar
        public string producto_nombre { get; set; }
        public string polleria_origen_nombre { get; set; }
        public string polleria_destino_nombre { get; set; }
        public decimal stock_disponible_origen { get; set; }
    }
}
