namespace ApiFacturacion.Models;

public sealed class ProductoFacturado
{
    public int Id { get; set; }
    public int ProductoOrdenId { get; set; }
    public required string Nombre { get; set; }
    public int Cantidad { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ProcesadoEn { get; set; }
}
