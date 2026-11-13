using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMTransfersVentaStock
    {
        public int IdTransferencia { get; set; }

        [Required(ErrorMessage = "El producto es obligatorio")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La pollería de origen es obligatoria")]
        public int IdPolleriaOrigen { get; set; }

        [Required(ErrorMessage = "La pollería de destino es obligatoria")]
        public int IdPolleriaDestino { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio")]
        [StringLength(500, ErrorMessage = "El motivo no puede exceder los 500 caracteres")]
        public string Motivo { get; set; }

        public DateTime FechaTransferencia { get; set; }
        public string NombreProducto { get; set; }
        public string PolleriaOrigen { get; set; }
        public string PolleriaDestino { get; set; }
        public string NombreUsuario { get; set; }
        public string Estado { get; set; }

        public CMTransfersVentaStock()
        {
            Estado = "PENDIENTE";
            FechaTransferencia = DateTime.Now;
        }

    }
}
