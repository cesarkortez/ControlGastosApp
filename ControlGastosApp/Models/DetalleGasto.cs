using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosApp.Models
{
    public class DetalleGasto
    {
        public int Id { get; set; }  // PK

        [Required]
        public string TipoGastoCodigo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public int GastoId { get; set; }  

        // Propiedades de navegación
        public TipoGasto TipoGasto { get; set; }
        public Gasto Gasto { get; set; }
    }
}
