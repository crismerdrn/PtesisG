using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMDetalleVenta
    {
        public int id_detalleVenta { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor a 0")]
        public decimal precio_unitario { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El subtotal no puede ser negativo")]
        public decimal subtotal { get; set; }

        // Claves foráneas
        [Required(ErrorMessage = "La venta es obligatoria")]
        public int VENTAS_id_venta { get; set; }

        [Required(ErrorMessage = "El producto es obligatorio")]
        public int PRODUCTO_id_producto { get; set; }

        // Datos del producto
        [StringLength(150)]
        public string NombreProducto { get; set; }

        [StringLength(300)]
        public string DescripcionProducto { get; set; }

        [StringLength(100)]
        public string NombreCategoria { get; set; }

        [StringLength(50)]
        public string Medida { get; set; }

        [StringLength(10)]
        public string AbreviaturaMedida { get; set; }

        [StringLength(50)]
        public string CodigoProducto { get; set; }

        // Stock
        [Range(0, double.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public decimal StockActual { get; set; }

        public decimal StockMinimo { get; set; }
        public decimal StockDisponible { get; set; }

        // Estado de validación de stock
        public bool StockSuficiente { get; set; }
        public bool ValidadoStock { get; set; }
        public string MensajeStock { get; set; }

        // Campos UI
        public int Item { get; set; }
        public string CantidadFormateada { get; set; }
        public string PrecioUnitarioFormateado { get; set; }
        public string SubtotalFormateado { get; set; }

        // Constructor
        public CMDetalleVenta()
        {
            StockSuficiente = true;
            ValidadoStock = false;
            MensajeStock = string.Empty;
        }

        // Método para validar stock del detalle
        public void ValidarStock(int idPolleria)
        {
            try
            {
                // Aquí deberías consultar stock real por pollería
                // Simulación temporal
                StockDisponible = StockActual;

                StockSuficiente = StockDisponible >= cantidad;

                MensajeStock = StockSuficiente
                    ? $"Stock suficiente: {StockDisponible}"
                    : $"Stock insuficiente: disponible {StockDisponible}, requerido {cantidad}";

                ValidadoStock = true;
            }
            catch
            {
                StockSuficiente = false;
                ValidadoStock = false;
                MensajeStock = "Error al validar el stock";
            }
        }
    }

}
