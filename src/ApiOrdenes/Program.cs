using ApiOrdenes.Data;
using ApiOrdenes.Messaging;
using ApiOrdenes.Models;
using Azure.Messaging.ServiceBus;
using IntegracionPubSub.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("ordenesdb")
    ?? throw new InvalidOperationException("No existe ConnectionStrings:ordenesdb.");
var serviceBusConnection = builder.Configuration.GetConnectionString("servicebus")
    ?? throw new InvalidOperationException("No existe ConnectionStrings:servicebus.");

builder.Services.AddDbContext<OrdenesDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnection));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(Colas.Productos));
builder.Services.AddHostedService<ProductosProcesadosListener>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

await InicializarBaseDatosAsync(app);

app.MapDefaultEndpoints();

app.MapPost("/api/productos", async (
    CrearProductoRequest request,
    OrdenesDbContext db,
    ServiceBusSender sender,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Nombre) || request.Cantidad <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Nombre)] = ["El nombre es obligatorio."],
            [nameof(request.Cantidad)] = ["La cantidad debe ser mayor que cero."]
        });
    }

    var producto = new ProductoOrden
    {
        Nombre = request.Nombre.Trim(),
        Cantidad = request.Cantidad,
        Estado = EstadoProducto.Pendiente,
        CreadoEn = DateTimeOffset.UtcNow
    };

    db.Productos.Add(producto);
    await db.SaveChangesAsync(cancellationToken);

    var evento = new ProductoCreadoMensaje(
        Guid.NewGuid(), producto.Id, producto.Nombre, producto.Cantidad, producto.CreadoEn);
    var mensaje = new ServiceBusMessage(BinaryData.FromObjectAsJson(evento))
    {
        MessageId = evento.EventoId.ToString(),
        Subject = nameof(ProductoCreadoMensaje),
        ContentType = "application/json"
    };
    await sender.SendMessageAsync(mensaje, cancellationToken);

    return Results.Created($"/api/productos/{producto.Id}", producto);
});

app.MapGet("/api/productos", async (OrdenesDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.Productos.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken)));

app.MapGet("/api/productos/{id:int}", async (
    int id, OrdenesDbContext db, CancellationToken cancellationToken) =>
{
    var producto = await db.Productos.AsNoTracking()
        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    return producto is null ? Results.NotFound() : Results.Ok(producto);
});

app.Run();

static async Task InicializarBaseDatosAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdenesDbContext>();
    if (app.Configuration.GetValue<bool>("Database:ResetOnStart"))
    {
        await db.Database.EnsureDeletedAsync();
    }
    await db.Database.EnsureCreatedAsync();
}

public sealed record CrearProductoRequest(string Nombre, int Cantidad);
