using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMAjusteStock
    {
        public int IdAjuste { get; set; }

        [Required(ErrorMessage = "El producto es obligatorio")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La pollería es obligatoria")]
        public int IdPolleria { get; set; }

        [Required(ErrorMessage = "El nuevo stock es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public decimal NuevoStock { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio")]
        [StringLength(500, ErrorMessage = "El motivo no puede exceder los 500 caracteres")]
        public string Motivo { get; set; }

        public DateTime FechaAjuste { get; set; }
        public decimal StockAnterior { get; set; }
        public decimal Diferencia { get; set; }
        public string NombreProducto { get; set; }
        public string Polleria { get; set; }
        public string NombreUsuario { get; set; }
        public string TipoAjuste { get; set; }

        public CMAjusteStock()
        {
            FechaAjuste = DateTime.Now;
            TipoAjuste = "MANUAL";
        }
    }
}
