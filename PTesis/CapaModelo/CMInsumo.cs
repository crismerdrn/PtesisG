using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMInsumo
    {
        public int id_insumo { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public bool estado { get; set; }
        public DateTime fecha_registro { get; set; }

        public int MEDIDAS_id_medida { get; set; }
        public string NombreMedida { get; set; }
        public string Abreviatura { get; set; }

        public CMMedida oMedida { get; set; }

        public CMInsumo()
        {
            oMedida = new CMMedida();
        }
    }
}