namespace ApiOrdenes.Models;

public enum EstadoProducto
{
    Pendiente,
    Procesado
}

public sealed class ProductoOrden
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public int Cantidad { get; set; }
    public EstadoProducto Estado { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? ModificadoEn { get; set; }
}
