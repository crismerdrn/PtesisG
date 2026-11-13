using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMVenta
    {
        public int id_venta { get; set; }

        [StringLength(50)]
        public string NumeroVenta { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El total no puede ser negativo")]
        public decimal Total_venta { get; set; }

        [Required(ErrorMessage = "La fecha de venta es obligatoria")]
        public DateTime Fecha_venta { get; set; }

        public TimeSpan hora_venta { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un tipo de venta")]
        public int? TIPO_VENTA_id_tipoVenta { get; set; }
        public string NombreTipoVenta { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; }

        [StringLength(50)]
        public string Tipo_venta { get; set; }

        // Claves foráneas
        [Required(ErrorMessage = "El usuario es obligatorio")]
        public int USUARIOS_id_usuario { get; set; }

        public int? CLIENTE_id_cliente { get; set; }

        [Required(ErrorMessage = "La pollería es obligatoria")]
        public int POLLERIA_id_polleria { get; set; }

        // Datos relacionados
        [StringLength(150)]
        public string NombreVendedor { get; set; }

        [StringLength(150)]
        public string NombreCliente { get; set; }

        [StringLength(20)]
        public string TelefonoCliente { get; set; }

        [EmailAddress(ErrorMessage = "Email inválido")]
        public string EmailCliente { get; set; }

        [StringLength(20)]
        public string DNICliente { get; set; }

        [StringLength(150)]
        public string NombrePolleria { get; set; }

        [StringLength(250)]
        public string DireccionPolleria { get; set; }

        // Pago
        [Range(0, double.MaxValue)]
        public decimal MontoPagado { get; set; }

        [StringLength(50)]
        public string TipoPago { get; set; }

        // Detalles
        public List<CMDetalleVenta> Detalles { get; set; }

        // UI / Reportes
        public string FechaVentaFormateada { get; set; }
        public string HoraVentaFormateada { get; set; }
        public string TotalVentaFormateado { get; set; }

        public CMVenta()
        {
            Detalles = new List<CMDetalleVenta>();
            ValidacionesStock = new List<CMValidacionStock>();
            Estado = "PENDIENTE";
            Fecha_venta = DateTime.Now;
            hora_venta = DateTime.Now.TimeOfDay;
            StockSuficiente = true;
        }

        // Validación de negocio
        public bool PagoCompleto()
        {
            return MontoPagado >= Total_venta;
        }

        // Nuevas propiedades para manejar stock
        public bool ValidadoStock { get; set; }
        public string MensajeValidacionStock { get; set; }
        public List<CMValidacionStock> ValidacionesStock { get; set; }
        public decimal StockDisponibleProducto { get; set; }
        public bool StockSuficiente { get; set; }

        // Método para validar stock
        public bool ValidarStock()
        {
            StockSuficiente = true;
            MensajeValidacionStock = "";

            if (Detalles == null || Detalles.Count == 0)
            {
                StockSuficiente = false;
                MensajeValidacionStock = "No hay detalles para validar";
                return StockSuficiente;
            }

            foreach (var detalle in Detalles)
            {
                if (!detalle.StockSuficiente)
                {
                    StockSuficiente = false;
                    MensajeValidacionStock += $"{detalle.NombreProducto}: Stock insuficiente. ";
                }
            }

            ValidadoStock = true;
            return StockSuficiente;
        }
    }
}
