#:package Aspire.Hosting.Azure.ServiceBus@13.4.6
#:package Aspire.Hosting.PostgreSQL@13.4.6
#:sdk Aspire.AppHost.Sdk@13.4.6

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var ordenesDb = postgres.AddDatabase("ordenesdb");
var facturacionDb = postgres.AddDatabase("facturaciondb");

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

serviceBus.AddServiceBusQueue("productos");
serviceBus.AddServiceBusQueue("productos-procesados");

builder.AddProject("api-ordenes", "./src/ApiOrdenes/ApiOrdenes.csproj")
    .WithReference(ordenesDb)
    .WithReference(serviceBus)
    .WaitFor(ordenesDb)
    .WaitFor(serviceBus)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.AddProject("api-facturacion", "./src/ApiFacturacion/ApiFacturacion.csproj")
    .WithReference(facturacionDb)
    .WithReference(serviceBus)
    .WaitFor(facturacionDb)
    .WaitFor(serviceBus)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.Build().Run();
