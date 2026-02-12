# Bot de Soporte - Motor Conversacional

API en .NET 6 que implementa un bot conversacional para gestionar tickets de soporte.

## Que hace

El bot permite a los usuarios:
- Crear tickets de soporte paso a paso
- Consultar el estado de tickets existentes
- Cancelar operaciones en cualquier momento

## Estructura

```
src/
├── BotApi/          -> API principal del bot (puerto 5000)
└── MockApi/         -> Simula el servicio externo de tickets (puerto 5001)
```

## Como ejecutar

Abrir dos terminales:

```bash
# Terminal 1 - Mock API (iniciar primero)
cd src/MockApi
dotnet run

# Terminal 2 - Bot API
cd src/BotApi
dotnet run
```

## Endpoint

`POST http://localhost:5000/messages`

```json
{
    "conversationId": "usuario-123",
    "message": "hola"
}
```

## Flujos disponibles

### Crear ticket

El usuario dice "crear ticket" y el bot pide:
1. Nombre
2. Email (se valida el formato)
3. Descripcion del problema
4. Confirmacion

Al confirmar se crea el ticket y devuelve el ID.

### Consultar ticket

```
"ver estado del ticket TCK-100"
```

Devuelve la info del ticket si existe.

### Cancelar

En cualquier momento escribir "cancelar" para salir del flujo actual.

## Tickets de prueba

La mock API trae 2 tickets precargados:
- TCK-100 (Abierto)
- TCK-101 (En Progreso)

## OAuth

El bot se autentica automaticamente con la mock API usando client_credentials.
Las credenciales estan en appsettings.json.

## Probar con curl

```bash
# Saludo
curl -X POST http://localhost:5000/messages \
  -H "Content-Type: application/json" \
  -d '{"conversationId":"test","message":"hola"}'

# Crear ticket
curl -X POST http://localhost:5000/messages \
  -H "Content-Type: application/json" \
  -d '{"conversationId":"test","message":"crear ticket"}'

# Consultar
curl -X POST http://localhost:5000/messages \
  -H "Content-Type: application/json" \
  -d '{"conversationId":"test","message":"ver estado del ticket TCK-100"}'
```

## Postman

Importar el archivo `ConversationalBot.postman_collection.json` que tiene todas las peticiones armadas.
