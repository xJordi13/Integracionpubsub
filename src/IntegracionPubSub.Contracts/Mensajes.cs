namespace IntegracionPubSub.Contracts;

public sealed record ProductoCreadoMensaje(
    Guid EventoId,
    int ProductoId,
    string Nombre,
    int Cantidad,
    DateTimeOffset CreadoEn);

public sealed record ProductoProcesadoMensaje(
    Guid EventoId,
    int ProductoId,
    DateTimeOffset ProcesadoEn);
