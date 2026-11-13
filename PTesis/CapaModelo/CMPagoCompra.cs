using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMPagoCompra
    {
        public int id_pagoCompra { get; set; }
        public decimal monto { get; set; }
        public DateTime fecha_pago { get; set; }
        public bool estado { get; set; }
        public int COMPRA_id_compra { get; set; }
        public int TIPO_PAGO_id_tipoPago { get; set; }
        public CMTipoPago oTipoPago { get; set; }

        public CMPagoCompra()
        {
            oTipoPago = new CMTipoPago();
        }
    }
}
