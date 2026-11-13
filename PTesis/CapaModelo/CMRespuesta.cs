using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMRespuesta
    {
        public bool Resultado { get; set; }
        public string Mensaje { get; set; }
        public int IdGenerado { get; set; }

        public object Datos { get; set; }

        public CMRespuesta()
        {
            Resultado = false;
            Mensaje = "";
            IdGenerado = 0;
        }
    }
}
