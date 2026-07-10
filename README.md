# Integración Pub/Sub con .NET Aspire

Actividad de **Integración e Implementación de Software** basada en el flujo explicado en el video de clase.

## Objetivo

Demostrar comunicación asíncrona y consistencia eventual entre dos sistemas independientes:

1. `ApiOrdenes` recibe un producto y lo guarda con estado `Pendiente`.
2. Publica un mensaje en la cola `productos`.
3. `ApiFacturacion` consume el mensaje y guarda el producto en su propia base de datos.
4. Facturación publica la confirmación en `productos-procesados`.
5. `ApiOrdenes` consume la confirmación y cambia el estado a `Procesado`.

```text
Cliente
  │ POST /api/productos
  ▼
ApiOrdenes ──► ordenesdb (Pendiente)
  │
  └──► cola productos ──► ApiFacturacion ──► facturaciondb
                              │
                              └──► cola productos-procesados
                                              │
                                              ▼
                              ApiOrdenes actualiza a Procesado
```

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

Desde la carpeta del proyecto:

```powershell
aspire run
```

Abre la URL del Dashboard que aparece en la terminal y espera a que todos los recursos estén saludables. En desarrollo, las dos bases se reinician al arrancar para reproducir la demostración del profesor con tablas vacías.

## Prueba

En el Dashboard abre el endpoint HTTP de `api-ordenes`. También puedes usar `src/ApiOrdenes/ApiOrdenes.http`, reemplazando el puerto por el asignado por Aspire.

Petición:

```http
POST /api/productos
Content-Type: application/json

{
  "nombre": "Laptop",
  "cantidad": 25
}
```

Verificación en Órdenes:

```http
GET /api/productos
```

El registro debe terminar con `"estado": "Procesado"` y una fecha `modificadoEn`. El endpoint `GET /api/productos` de Facturación debe mostrar el mismo producto y su fecha `procesadoEn`.

## Decisiones técnicas

- Los servicios solo comparten contratos de mensajes, no sus bases de datos.
- Cada consumidor usa un `BackgroundService` y completa el mensaje únicamente después de guardar los cambios.
- Facturación tiene un índice único por `ProductoOrdenId`, evitando duplicados si Service Bus reintenta.
- Los mensajes inválidos o referidos a un producto inexistente pasan a la Dead-letter Queue.
- `Aspire.ServiceDefaults` agrega health checks, telemetría y observabilidad en el Dashboard.

## Evidencia obtenida

En la prueba local, el producto `Laptop` con cantidad `25`:

- se creó en Órdenes con estado `Pendiente`;
- apareció en Facturación;
- regresó a Órdenes con estado `Procesado`;
- completó el recorrido en aproximadamente 0,57 segundos.

## Commits sugeridos para explicar al profesor

1. Creación de la solución Aspire y las APIs.
2. Configuración de PostgreSQL, Service Bus y contratos.
3. Implementación del flujo Pub/Sub y consistencia eventual.
4. Documentación y prueba final.
