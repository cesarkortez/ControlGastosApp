namespace ControlGastosApp.ViewModels
{
    public class GastoFormModel
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public int FondoMonetarioId { get; set; }

        public string Observaciones { get; set; }

        public string Comercio { get; set; }

        public string TipoDocumento { get; set; }

        public string NumeroDocumento { get; set; }

        public List<DetalleGastoFormModel> Detalles { get; set; } = new List<DetalleGastoFormModel>();
    }

    public class DetalleGastoFormModel
    {
        public string TipoGastoCodigo { get; set; }
        public decimal Monto { get; set; }
    }
}
