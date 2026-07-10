
## Recursos orquestados

- AppHost de .NET Aspire.
- API de Órdenes.
- API de Facturación.
- Un servidor PostgreSQL con dos bases independientes: `ordenesdb` y `facturaciondb`.
- pgAdmin para observar las tablas.
- Emulador local de Azure Service Bus.
- Cola `productos`.
- Cola `productos-procesados`.
- SQL Server interno requerido por el emulador de Service Bus.
- Aspire Dashboard y recursos auxiliares.

## Requisitos

- .NET SDK 10.
- Aspire CLI 13.4 o superior.
- Docker Desktop iniciado.

## Ejecución

Desde la carpeta del proyecto: aspire run
