using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMCompra
    {
            public int id_compra { get; set; }
            public string Referencia { get; set; }
            public decimal MontoTotal { get; set; }
            public DateTime FechaCompra { get; set; }
            public DateTime FechaRegistro { get; set; }
            public string Estado { get; set; }
            public int USUARIOS_id_usuario { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un proveedor.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un proveedor.")]
        [Display(Name = "Proveedor")]
        public int PROVEEDOR_id_proveedor { get; set; }

        public int? TIPO_COMPRA_id_tipoCompra { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un método de pago.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un método de pago.")]
        [Display(Name = "Método de Pago")]
        public int? IdTipoPago { get; set; }
        [Display(Name = "Método de Pago")]
        public string TipoPago { get; set; }

        public string NombreProveedor { get; set; }
            public string DocumentoProveedor { get; set; }
            public string NombreUsuario { get; set; }
            public string NombreTipoCompra { get; set; }
            public int TotalItems { get; set; }

            public List<CMDetalleCompra> Detalles { get; set; }

            public CMCompra()
            {
                Detalles = new List<CMDetalleCompra>();
                FechaCompra = DateTime.Now;
                FechaRegistro = DateTime.Now;
                Estado = "PENDIENTE";
            }
    }
}
