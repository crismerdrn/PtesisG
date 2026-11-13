using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMPagoVenta
    {
        public int id_pagoVenta { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal monto { get; set; }

        [Required(ErrorMessage = "La fecha de pago es obligatoria")]
        public DateTime fecha_pago { get; set; }

        public bool estado { get; set; }

        // Claves foráneas
        [Required(ErrorMessage = "La venta es obligatoria")]
        public int VENTAS_id_venta { get; set; }

        [Required(ErrorMessage = "El tipo de pago es obligatorio")]
        public int TIPO_PAGO_id_tipoPago { get; set; }

        // Datos relacionados
        [StringLength(100)]
        public string NombreTipoPago { get; set; }

        [StringLength(250)]
        public string DescripcionTipoPago { get; set; }

        // UI
        public string MontoFormateado { get; set; }
        public string FechaPagoFormateada { get; set; }
        public string EstadoFormateado { get; set; }
    }
}
