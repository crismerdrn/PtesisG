using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CapaModelo
{
    public class CMTipoVenta
    {
        public int id_tipoVenta { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de venta es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [StringLength(150, ErrorMessage = "La descripción no puede superar los 150 caracteres")]
        [Display(Name = "Descripción")]
        public string descripcion { get; set; }

        [Display(Name = "Estado")]
        public bool estado { get; set; }

        [Display(Name = "Fecha de Registro")]
        public DateTime fecha_registro { get; set; }

        public CMTipoVenta()
        {
            estado = true;
            fecha_registro = DateTime.Now;
        }
    }
}
