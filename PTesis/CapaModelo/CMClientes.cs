using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMClientes
    {
        
        public int id_cliente { get; set; }

        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public string Telefono { get; set; }

        public string Direccion { get; set; }

        public string Email { get; set; }

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }

        public string NombreCompleto
        {
            get { return $"{Nombre} {Apellido}"; }
        }
    }
}
