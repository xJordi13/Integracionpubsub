using ApiFacturacion.Data;
using ApiFacturacion.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("facturaciondb")
    ?? throw new InvalidOperationException("No existe ConnectionStrings:facturaciondb.");
var serviceBusConnection = builder.Configuration.GetConnectionString("servicebus")
    ?? throw new InvalidOperationException("No existe ConnectionStrings:servicebus.");

builder.Services.AddDbContext<FacturacionDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnection));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ServiceBusClient>().CreateSender(Colas.ProductosProcesados));
builder.Services.AddHostedService<ProductosListener>();

var app = builder.Build();

await InicializarBaseDatosAsync(app);

app.MapDefaultEndpoints();
app.MapGet("/api/productos", async (FacturacionDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.Productos.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken)));

app.Run();

static async Task InicializarBaseDatosAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<FacturacionDbContext>();
    if (app.Configuration.GetValue<bool>("Database:ResetOnStart"))
    {
        await db.Database.EnsureDeletedAsync();
    }
    await db.Database.EnsureCreatedAsync();
}
