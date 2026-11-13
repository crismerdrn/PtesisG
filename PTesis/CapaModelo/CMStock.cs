using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMStock
    {
       
            public int id_stock { get; set; }
            public decimal stock_actual { get; set; }
            public decimal stock_minimo { get; set; }
            public DateTime Fecha_ultimo_movimiento { get; set; }
            public int PRODUCTO_id_producto { get; set; }
            public int POLLERIA_id_polleria { get; set; }

            // Propiedades para mostrar
            public string producto_nombre { get; set; }
            public string producto_descripcion { get; set; }
            public decimal precio_venta { get; set; }
            public string tipo_producto { get; set; }
            public string medida { get; set; }
            public string abreviatura { get; set; }
            public string polleria_nombre { get; set; }
            public string estado_stock { get; set; }

            // Objetos relacionados
            public CMProducto oProducto { get; set; }
            public CMPolleria oPolleria { get; set; }

            public CMStock()
            {
                oProducto = new CMProducto();
                oPolleria = new CMPolleria();
            }
        }

        public class CMStockInsumo
        {
            public int id_stock_insumo { get; set; }
            public decimal stock_actual { get; set; }
            public decimal stock_minimo { get; set; }
            public DateTime Fecha_ultimo_movimiento { get; set; }
            public int INSUMO_id_insumo { get; set; }
            public int POLLERIA_id_polleria { get; set; }

            // Propiedades para mostrar
            public string insumo_nombre { get; set; }
            public string insumo_descripcion { get; set; }
            public string medida { get; set; }
            public string abreviatura { get; set; }
            public string polleria_nombre { get; set; }
            public string estado_stock { get; set; }

            // Objetos relacionados
            public CMInsumo oInsumo { get; set; }
            public CMPolleria oPolleria { get; set; }

            public CMStockInsumo()
            {
                oInsumo = new CMInsumo();
                oPolleria = new CMPolleria();
            }
        }

        public class CMMovimientosStock
        {
            public int id_movimiento { get; set; }
            public DateTime fecha_movimiento { get; set; }
            public string tipo_movimiento { get; set; }
            public int id_referencia { get; set; }
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

            // Propiedades para mostrar
            public string item_nombre { get; set; }
            public string polleria_nombre { get; set; }
            public string usuario_nombre { get; set; }

            // Propiedades calculadas
            public string entrada_salida
            {
                get
                {
                    return cantidad > 0 ? "ENTRADA" : "SALIDA";
                }
            }
            public decimal cantidad_absoluta
            {
                get
                {
                    return Math.Abs(cantidad);
                }
            }
        }

    //Stock Ajuste

    // CMStockTransferVenta

}

