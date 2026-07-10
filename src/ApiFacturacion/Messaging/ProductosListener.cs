using ApiFacturacion.Data;
using ApiFacturacion.Models;
using Azure.Messaging.ServiceBus;
using IntegracionPubSub.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ApiFacturacion.Messaging;

public sealed class ProductosListener(
    ServiceBusClient client,
    ServiceBusSender sender,
    IServiceScopeFactory scopeFactory,
    ILogger<ProductosListener> logger) : BackgroundService
{
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = client.CreateProcessor(Colas.Productos, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });
        _processor.ProcessMessageAsync += ProcesarMensajeAsync;
        _processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Error al escuchar {Cola}", Colas.Productos);
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(stoppingToken);
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ProcesarMensajeAsync(ProcessMessageEventArgs args)
    {
        var evento = args.Message.Body.ToObjectFromJson<ProductoCreadoMensaje>();
        if (evento is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "MensajeInvalido", "No se pudo leer el JSON.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FacturacionDbContext>();
        var producto = await db.Productos
            .SingleOrDefaultAsync(x => x.ProductoOrdenId == evento.ProductoId);
        var procesadoEn = DateTimeOffset.UtcNow;

        if (producto is null)
        {
            producto = new ProductoFacturado
            {
                ProductoOrdenId = evento.ProductoId,
                Nombre = evento.Nombre,
                Cantidad = evento.Cantidad,
                CreadoEn = evento.CreadoEn,
                ProcesadoEn = procesadoEn
            };
            db.Productos.Add(producto);
            await db.SaveChangesAsync();
        }
        else
        {
            procesadoEn = producto.ProcesadoEn;
        }

        var confirmacion = new ProductoProcesadoMensaje(
            Guid.NewGuid(), evento.ProductoId, procesadoEn);
        var mensaje = new ServiceBusMessage(BinaryData.FromObjectAsJson(confirmacion))
        {
            MessageId = confirmacion.EventoId.ToString(),
            Subject = nameof(ProductoProcesadoMensaje),
            ContentType = "application/json"
        };
        await sender.SendMessageAsync(mensaje);
        await args.CompleteMessageAsync(args.Message);
        logger.LogInformation("Producto {ProductoId} procesado en Facturación", evento.ProductoId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
