using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosApp.Models
{
    public class Presupuesto
    {
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required]
        public string TipoGastoCodigo { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mes { get; set; }

        [Required]
        [Range(2023, 2100)]
        public int Anio { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal MontoPresupuestado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoEjecutado { get; set; } = 0;

        [Required]
        public int FondoMonetarioId { get; set; }

        [ForeignKey("FondoMonetarioId")]
        public FondoMonetario FondoMonetario { get; set; } // Esta propiedad de navegación sí existe
    }
}
