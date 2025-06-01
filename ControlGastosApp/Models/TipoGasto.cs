using System.ComponentModel.DataAnnotations;

namespace ControlGastosApp.Models
{
    public class TipoGasto
    {
        [Key]
        [StringLength(10)]
        public string? Codigo { get; set; }  // Formato: "0001", "0002", etc.

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }
    }
}
