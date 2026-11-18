using System;

namespace CapaModelo
{
    // Forma en que se compra un insumo y cuántas unidades del insumo trae.
    // Ej: Arroz (kg) -> Quintal = 46; Plátano (und) -> Racimo = 50; Pollo (presa) -> Pollo entero = 8.
    public class CMPresentacionInsumo
    {
        public int id_presentacion { get; set; }
        public int INSUMO_id_insumo { get; set; }
        public string nombre { get; set; }
        public decimal factor { get; set; }
        public bool estado { get; set; }
        public DateTime fecha_registro { get; set; }
    }
}
