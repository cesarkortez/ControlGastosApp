using System.ComponentModel.DataAnnotations;

namespace ControlGastosApp.Models
{
    public class MovimientoGasto
    {
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Fondo Monetario")]
        public int FondoMonetarioId { get; set; }
        public FondoMonetario FondoMonetario { get; set; }

        [StringLength(500)]
        public string Observaciones { get; set; }

        [Required]
        [Display(Name = "Nombre de Comercio")]
        [StringLength(200)]
        public string Comercio { get; set; }

        [Required]
        [Display(Name = "Tipo de Documento")]
        public string TipoDocumento { get; set; } // Comprobante, Factura, Otro

        public List<DetalleGasto> Detalles { get; set; } = new List<DetalleGasto>();
    }

    //public class DetalleGasto
    //{
    //    public int Id { get; set; }

    //    [Required]
    //    [Display(Name = "Tipo de Gasto")]
    //    public string TipoGastoCodigo { get; set; }
    //    public TipoGasto TipoGasto { get; set; }

    //    [Required]
    //    [Range(0.01, double.MaxValue)]
    //    public decimal Monto { get; set; }
    //}
}
