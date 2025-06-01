namespace ControlGastosApp.Models
{
    public class MovimientoViewModel
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } // "Gasto" o "Depósito"
        public string FondoMonetario { get; set; }
        public string Descripcion { get; set; }
        public decimal Monto { get; set; }
        public string Referencia { get; set; } // Referencia del documento
    }

}
