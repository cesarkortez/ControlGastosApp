using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlGastosApp.Models
{
    public class Gasto
    {
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Fondo Monetario")]
        public int FondoMonetarioId { get; set; }

        [StringLength(500)]
        public string Observaciones { get; set; }

        [Required]
        [StringLength(200)]
        public string Comercio { get; set; }

        [Required]
        [Display(Name = "Tipo Documento")]
        public string TipoDocumento { get; set; } = "Factura"; // Valor por defecto

        [Required]
        [StringLength(50)]
        [Display(Name = "Número de Documento")]
        public string NumeroDocumento { get; set; }  // Nuevo campo

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total")]
        [ScaffoldColumn(false)]
        public decimal MontoTotal { get; set; }

        // Relaciones
        public FondoMonetario? FondoMonetario { get; set; }
        public List<DetalleGasto> Detalles { get; set; }
    }
}