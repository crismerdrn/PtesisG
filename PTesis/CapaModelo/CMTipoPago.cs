using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMTipoPago
    {
        public int id_tipoPago { get; set; }

        // <-- 2. REGLA DE VALIDACIÓN APLICADA
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string nombre { get; set; }

        public string descripcion { get; set; }
        public bool Estado { get; set; }
    }
}