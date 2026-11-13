using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMProducto
    {
        public int id_producto { get; set; }

        [Required(ErrorMessage = "El Nombre es obligatorio")]
        [StringLength(250)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El Precio de Venta es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a cero")]
        [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]

        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Formato inválido. Use punto decimal (ej: 3.50)")]
        public decimal PrecioVenta { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una Medida")]
        public int MEDIDAS_id_medida { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Tipo de Producto")]
        public int TIPO_PRODUCTO_id_tipoProducto { get; set; }
        public string Medida { get; set; }
        public string AbreviaturaMedida { get; set; }
        public string TipoProducto { get; set; }
        public int CantidadInsumos { get; set; } // De sp_Producto_Listar

        public bool Estado { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}
