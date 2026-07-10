using ApiOrdenes.Data;
using ApiOrdenes.Models;
using Azure.Messaging.ServiceBus;
using IntegracionPubSub.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ApiOrdenes.Messaging;

public sealed class ProductosProcesadosListener(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<ProductosProcesadosListener> logger) : BackgroundService
{
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = client.CreateProcessor(Colas.ProductosProcesados, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });
        _processor.ProcessMessageAsync += ProcesarMensajeAsync;
        _processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Error al escuchar {Cola}", Colas.ProductosProcesados);
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
        var evento = args.Message.Body.ToObjectFromJson<ProductoProcesadoMensaje>();
        if (evento is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "MensajeInvalido", "No se pudo leer el JSON.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdenesDbContext>();
        var producto = await db.Productos.SingleOrDefaultAsync(x => x.Id == evento.ProductoId);
        if (producto is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "ProductoNoEncontrado",
                $"No existe el producto {evento.ProductoId} en Órdenes.");
            return;
        }

        producto.Estado = EstadoProducto.Procesado;
        producto.ModificadoEn = evento.ProcesadoEn;
        await db.SaveChangesAsync();
        await args.CompleteMessageAsync(args.Message);
        logger.LogInformation("Producto {ProductoId} actualizado a Procesado", producto.Id);
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
