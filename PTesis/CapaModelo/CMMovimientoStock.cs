using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMMovimientoStock
    {
        public int id_movimiento { get; set; }
        public DateTime fecha_movimiento { get; set; }
        public string tipo_movimiento { get; set; }
        public int? id_referencia { get; set; }
        public string tipo_referencia { get; set; }
        public int id_item { get; set; }
        public string tipo_item { get; set; }
        public decimal cantidad { get; set; }
        public decimal stock_anterior { get; set; }
        public decimal stock_nuevo { get; set; }
        public int id_polleria { get; set; }
        public int id_usuario { get; set; }
        public string observaciones { get; set; }
        public DateTime fecha_registro { get; set; }

        // Campos adicionales para UI
        public string nombre_item { get; set; }
        public string nombre_polleria { get; set; }
        public string nombre_usuario { get; set; }

        public string signo_cantidad
        {
            get { return cantidad >= 0 ? "+" : ""; }
        }

        public string clase_movimiento
        {
            get
            {
                switch (tipo_movimiento)
                {
                    case "ENTRADA":
                        return "table-success";

                    case "SALIDA":
                        return "table-danger";

                    case "AJUSTE":
                        return "table-warning";

                    case "TRANSFERENCIA":
                        return "table-info";

                    default:
                        return "";
                }
            }
        }
    }
}
