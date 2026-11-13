using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMInsumoProducto
    {
        public int id_insumo_producto { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Producto")]
        [Display(Name = "Producto")]
        public int PRODUCTO_id_producto { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Insumo")]
        [Display(Name = "Insumo")]
        public int INSUMO_id_insumo { get; set; }

        [Required(ErrorMessage = "La cantidad requerida es obligatoria")]
        [Range(0.0001, 999999.9999, ErrorMessage = "La cantidad debe ser mayor a cero (mínimo 0.0001)")]
        [Display(Name = "Cantidad Requerida")]
        [RegularExpression(@"^\d+(\.\d{1,4})?$", ErrorMessage = "Formato inválido. Use punto decimal (ej: 3.5000)")]
        public decimal cantidad_requerida { get; set; }

        [StringLength(500)]
        [Display(Name = "Observaciones")]
        public string observaciones { get; set; }

        public bool estado { get; set; } = true;
        public DateTime fecha_registro { get; set; } = DateTime.Now;
        public DateTime? fecha_actualizacion { get; set; }

        // Propiedades para mostrar en vistas
        public string NombreProducto { get; set; }
        public string NombreInsumo { get; set; }
        public string DescripcionInsumo { get; set; }
        public string MedidaInsumo { get; set; }
        public string AbreviaturaMedida { get; set; }
        public decimal PrecioVentaProducto { get; set; }

        public string DescripcionProducto { get; set; }
        public decimal? UltimoPrecioCompra { get; set; }
        public decimal? StockActual { get; set; }
        public decimal? StockMinimo { get; set; }

        /// <summary>
        /// Consumo por unidad vendida tal como lo descuenta sp_Venta_RegistrarConStock:
        /// en insumos medidos en kg o lts, una cantidad de 1 o más se interpreta en gramos o mililitros.
        /// </summary>
        public string ConsumoPorUnidad
        {
            get
            {
                string abrev = (AbreviaturaMedida ?? "").Trim();
                string abrevMin = abrev.ToLowerInvariant();
                if ((abrevMin == "kg" || abrevMin == "lts") && cantidad_requerida >= 1)
                {
                    string menor = abrevMin == "kg" ? "gr" : "ml";
                    return $"{cantidad_requerida:0.####} {menor} = {cantidad_requerida / 1000m:0.####} {abrev}";
                }
                return $"{cantidad_requerida:0.####} {(string.IsNullOrEmpty(abrev) ? MedidaInsumo : abrev)}";
            }
        }

        // Constructor para inicializar propiedades
        public CMInsumoProducto()
        {
            observaciones = string.Empty;
            NombreProducto = string.Empty;
            NombreInsumo = string.Empty;
            DescripcionInsumo = string.Empty;
            MedidaInsumo = string.Empty;
            AbreviaturaMedida = string.Empty;
            DescripcionProducto = string.Empty;
        }
    }


    public class CMCostoProduccion
    {
        public int id_producto { get; set; }
        public string NombreProducto { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal CostoTotalProduccion { get; set; }
        public decimal GananciaUnitaria { get; set; }
        public decimal PorcentajeMargen { get; set; }
        public int TotalInsumos { get; set; }
        public int? UnidadesMaximasProducibles { get; set; }
        public string ClasificacionRentabilidad { get; set; }
        public DateTime FechaCalculo { get; set; } = DateTime.Now;
    }

    public class CMDetalleCostoProduccion
    {
        public int id_insumo_producto { get; set; }
        public string NombreInsumo { get; set; }
        public decimal cantidad_requerida { get; set; }
        public string MedidaInsumo { get; set; }
        public string AbreviaturaMedida { get; set; }
        public decimal UltimoPrecioCompra { get; set; }
        public decimal PrecioPromedio3Meses { get; set; }
        public decimal CostoInsumo { get; set; }
        public decimal? StockActual { get; set; }
        public int? UnidadesProducibles { get; set; }
        public decimal PorcentajeCosto { get; set; }
    }

    public class CMValidacionStocks
    {
        public int IdInsumo { get; set; }
        public string NombreInsumo { get; set; }
        public decimal CantidadRequerida { get; set; }
        public decimal StockActual { get; set; }
        public decimal CantidadNecesaria { get; set; }
        public bool Suficiente { get; set; }
        public int UnidadesPosibles { get; set; }
    }

    public class CMResumenValidacionStock
    {
        public bool StockSuficiente { get; set; }
        public string MensajeValidacion { get; set; }
        public int UnidadesMaximasProducibles { get; set; }
        public int CantidadSolicitada { get; set; }
        public string Recomendacion { get; set; }
    }

    public class CMRentabilidadProducto
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal CostoProduccion { get; set; }
        public decimal GananciaUnitaria { get; set; }
        public decimal PorcentajeMargen { get; set; }
        public int TotalInsumos { get; set; }
        public bool EstadoProducto { get; set; }
        public decimal StockActual { get; set; }
        public int VentasUltimoMes { get; set; }
        public string Clasificacion { get; set; }
        public decimal GananciaTotalMes { get; set; }
        public decimal ValorInventario { get; set; }
        public decimal DiasRotacion { get; set; }
    }
}
