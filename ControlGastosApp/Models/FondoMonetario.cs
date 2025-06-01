using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ControlGastosApp.Models
{
    public class FondoMonetario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo { get; set; }  // "Cuenta Bancaria" o "Caja Menuda"

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoActual { get; set; }  // Se actualiza con cada movimiento
    }
}
