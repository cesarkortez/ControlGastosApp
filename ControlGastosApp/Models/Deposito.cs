using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosApp.Models
{
    public class Deposito
    {
        public int Id { get; set; }
        [Required]
        public string? UserId { get; set; }


        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Fondo Monetario")]
        public int FondoMonetarioId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Monto { get; set; }

        // Relación
        public FondoMonetario FondoMonetario { get; set; }
    }
}
