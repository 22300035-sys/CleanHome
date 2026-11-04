# CleanHome

Para esta laptop, usa [el arranque local](docs/ARRANQUE_LOCAL.md): `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-CleanHome.ps1`. Levanta PostgreSQL, API y frontend; Swagger UI queda en http://127.0.0.1:5186/swagger. El inicio automatico al entrar a Windows se configura de forma opcional con `scripts/Set-CleanHomeAutostart.ps1`.

ASP.NET Core 10 + PostgreSQL, con identidad, catálogo, contratación de limpieza, documentos privados, chat SignalR, notificaciones, Stripe y Mercado Pago. Frontend React/Capacitor implementado a partir de Figma para clientes, trabajadores y administración. Consulta [la entrega del frontend](docs/ENTREGA_FRONTEND.md), [sus instrucciones](frontend/README.md) y las capturas en `docs/screenshots`.

## Requisitos

- SDK .NET 10 estable y actualizado; `global.json` admite versiones estables posteriores de la rama 10.
- PostgreSQL 18 nativo. No se necesita Docker.
- Cuenta y credenciales de cada proveedor para probar cobros externos. Los tests sustituyen las pasarelas HTTP y NO realizan cargos reales.
- SMTP con TLS para confirmar correos y recuperar contraseñas en producción.

## Arranque local

Desde la raíz del repositorio, en PowerShell:

```powershell
dotnet tool restore
dotnet restore backend/CleanHome.slnx
dotnet build backend/CleanHome.slnx -c Release --no-restore --warnaserror
$env:ASPNETCORE_ENVIRONMENT = 'Development'
# Sustituye los marcadores localmente. No publiques la contraseña.
$env:ConnectionStrings__CleanHome = 'Host=localhost;Port=5432;Database=cleanhome;Username=cleanhome_app;Password=TU_CLAVE_LOCAL'
$env:Cors__AllowedOrigins__0 = 'http://localhost:5173'
```

Crea previamente la base vacía con PostgreSQL y un usuario dedicado. Antes de aplicar a una base existente, revisa el SQL y respalda sus datos:

```powershell
dotnet ef migrations script --idempotent --project backend/src/CleanHome.Infrastructure --startup-project backend/src/CleanHome.Api --output migration-review.sql
# Aplicar únicamente a la base elegida, después de revisar el script:
dotnet ef database update --project backend/src/CleanHome.Infrastructure --startup-project backend/src/CleanHome.Api
dotnet run --project backend/src/CleanHome.Api --no-launch-profile --urls http://localhost:5200
```

No se aplican migraciones automáticamente al iniciar la API. `/health` indica liveness y `/health/ready` devuelve 503 si falla PostgreSQL o faltan migraciones. OpenAPI: `/openapi/v1.json` en Development; habilitación explícita opcional con `OpenApi__Enabled=true`.

## Identidad y roles

1. `POST /api/v1/auth/register`: `{ "email": "cliente@example.com", "password": "UnaClaveFuerte!2026" }`.
2. `POST /api/v1/auth/login` con los mismos campos; conservar `accessToken` y `refreshToken`.
3. Enviar `Authorization: Bearer <accessToken>` a las rutas privadas.
4. `POST /api/v1/profiles/me`: `{ "displayName": "Cliente", "role": "Customer" }` o rol `Worker`. El rol queda fijo.

Se utiliza ASP.NET Core Identity con tokens bearer opacos (no JWT), acceso de 15 minutos y refresh de 7 días. `POST /api/v1/auth/refresh` acepta `{ "refreshToken": "..." }`. `POST /api/v1/auth/logout` invalida refresh mediante security stamp; los access tokens ya emitidos vencen en un máximo de 15 minutos. El cliente debe eliminarlos al salir. No se aceptan cookies como autenticación de los controladores.

También están disponibles confirmación/reenvío de correo, recuperación/restablecimiento de contraseña, información de cuenta y 2FA mediante los endpoints estándar de Identity bajo `/api/v1/auth`. En Development/Testing, si falta SMTP, los correos se omiten y no se exige confirmación. En producción sí se exige; configurar SMTP antes del registro. No se imprimen códigos ni enlaces privados en logs.

Para crear el primer administrador, en un entorno controlado y con las migraciones ya aplicadas:

```powershell
$env:Bootstrap__Email = 'administrador@tu-dominio.example'
$env:Bootstrap__Password = 'CONFIGURAR_UNA_CLAVE_PRIVADA_FUERTE'
dotnet run --project backend/src/CleanHome.Api --no-launch-profile -- --bootstrap-admin true
Remove-Item Env:Bootstrap__Password
Remove-Item Env:Bootstrap__Email
```

No hay registro público de administradores ni credenciales predeterminadas. El comando rechaza correos ya existentes para impedir elevar una cuenta pública por accidente.

## Flujo de contratación

1. Cliente crea un domicilio con `POST /api/v1/locations`: `label`, `address`, `city`.
2. Trabajador sube evidencia privada; administración revisa y verifica su perfil.
3. Cliente crea solicitud con `POST /api/v1/requests`: `locationId`, `categoryId`, `description`, `startsAt` (ISO 8601 con offset), `durationMinutes` (30–720).
4. Trabajador verificado consulta `GET /api/v1/requests` y cotiza con `POST /api/v1/requests/{id}/quotes`: `amount` en MXN (1–100000, dos decimales), `notes`, `expiresAt`.
5. Cliente ve cotizaciones y acepta `POST /api/v1/quotes/{id}/accept`; obtiene una reserva.
6. Cliente solicita checkout, completa el pago en el proveedor y espera confirmación del webhook.
7. Trabajador cambia estado a `InProgress`; cliente confirma `Completed` con `POST /api/v1/bookings/{id}/status`.

La aceptación comprueba propiedad, vigencia, disponibilidad y horarios en una transacción Serializable; la solicitud tiene token de concurrencia y solo admite una reserva. Un conflicto concurrente devuelve 409 para recargar y reintentar. Se impiden reservas superpuestas tanto del trabajador como del cliente. El domicilio exacto solo se revela a participantes de una reserva; los listados de oportunidades solo muestran ciudad.

`Cancelled` se permite desde `Confirmed` sin pago/checkout pendiente, o tras reembolso. La API no cancela silenciosamente pagos en curso. La solicitud abierta también puede cancelarse antes de reservar.

## Pagos: Stripe y Mercado Pago

`POST /api/v1/bookings/{id}/checkout` con `{ "provider": "stripe" }` o `{ "provider": "mercadopago" }`. Abrir el `checkoutUrl` devuelto. El servidor obtiene importe/moneda de la reserva; el cliente no puede fijarlos. Solo hay un pago/proveedor por reserva.

Configurar mediante variables de entorno o User Secrets:

| Variable | Uso |
|---|---|
| `Payments__ReturnBaseUrl` | URL HTTPS del frontend para success/cancel/pending |
| `Payments__PublicApiUrl` | URL HTTPS pública de la API para Mercado Pago |
| `Payments__Stripe__SecretKey` | Clave privada Stripe del entorno correspondiente |
| `Payments__Stripe__WebhookSecret` | Secreto de firma Stripe |
| `Payments__MercadoPago__AccessToken` | Token privado de Mercado Pago |
| `Payments__MercadoPago__WebhookSecret` | Secreto de firma Mercado Pago |
| `Payments__MercadoPago__Sandbox` | `true` para checkout sandbox, `false` al habilitar producción |

Registrar webhooks públicos HTTPS:

- Stripe: `/api/v1/payments/webhooks/stripe`; eventos `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `charge.refunded`, `charge.dispute.created`.
- Mercado Pago: `/api/v1/payments/webhooks/mercadopago`; notificaciones de tipo `payment`, conservando `data.id` en query y encabezados `x-signature`, `x-request-id`.

Se verifica HMAC y tolerancia de tiempo, se vuelve a consultar la API del proveedor y se comprueban referencia, importe y moneda. Los reintentos con el mismo pago no duplican cambios de estado. No se confirma por redirección del navegador. `GET /api/v1/bookings/{id}/payment` permite consultar estado. `POST /api/v1/admin/payments/{id}/refund` solicita un reembolso completo; los parciales y disputas requieren conciliación con el proveedor.

Si la conexión falla mientras se crea checkout, el registro permanece pendiente para prevenir un segundo cargo. Administración debe conciliarlo por referencia en el proveedor antes de intervenir; no borrar registros para reintentar. Para una reserva ya asignada a un proveedor no se permite cambiar al otro. No existe reparto automático de fondos a trabajadores: esta integración cobra a la cuenta del comercio. Para un marketplace con dispersión se requiere el producto de cuentas conectadas del proveedor y su alta contractual.

## Documentos privados

- `POST /api/v1/documents`: multipart con campo `file`; PDF/PNG/JPEG hasta 5 MB, máximo 10 por perfil. Se verifica firma y extensión, no se confía en Content-Type.
- `GET /api/v1/documents`, `GET /api/v1/documents/{id}/download`, `DELETE /api/v1/documents/{id}`.
- Almacenamiento con claves aleatorias fuera de `wwwroot`. Descargas como attachment sin caché, solo propietario o Admin; se auditan.
- La revisión es manual. La firma de archivo no sustituye antivirus/CDR: antes de producción se debe conectar el escáner elegido, definir retención y cifrado/backup del volumen.
- Eliminar o rechazar evidencia revoca la verificación del trabajador.

Configurar `Storage__Documents` y `Storage__DataProtectionKeys` como rutas privadas persistentes. Proteger permisos del usuario del servicio y respaldar ambos volúmenes. Todas las réplicas deben compartir claves de Data Protection; esta entrega de chat/archivos está preparada para una instancia, sin backplane distribuido.

## Chat en tiempo real y notificaciones

Historial: `GET /api/v1/bookings/{id}/messages?page=1`; envío persistente: `POST` a la misma ruta con `{ "text": "Hola" }`. Solo participantes activos de esa reserva. El cliente debe renderizar texto, no HTML.

Con `@microsoft/signalr`, conectar a `/hubs/chat` mediante `accessTokenFactory`, escuchar `MessageReceived`. Se distribuyen mensajes a las conexiones autenticadas de los participantes no suspendidos. `JoinBooking(bookingId)` valida acceso adicional a una reserva; la entrega se dirige al usuario, por lo que no es necesario llamar Join para recibir. La conexión se cierra al caducar el access token y el cliente debe renovarlo/reconectar. Tras una desconexión, recuperar historial por REST: SignalR no sustituye la persistencia.

Notificaciones de cotización, reserva, chat y pago: `GET /api/v1/notifications?page=1`; marcar leída: `POST /api/v1/notifications/{id}/read`. No incluye push móvil ni emails transaccionales de estos eventos.

## Administración

Bajo `/api/v1/admin`, exclusivamente rol Identity `Admin`:

- `GET profiles`; `POST profiles/{id}/verify`; `POST profiles/{id}/suspend?suspended=true|false`.
- `GET documents`; `POST documents/{id}/review` con `{ "status": "Approved|Rejected", "notes": "..." }`.
- `GET payments`; `POST payments/{id}/refund`; `GET audit`.
- `POST service-categories` con `name`, `slug`, `description`; `POST service-categories/{id}/deactivate`.

## Pruebas y entrega

Configurar una base nueva y local llamada exactamente `cleanhome_test`, con usuario exclusivo. La integración valida nombre y host local antes de aplicar migraciones aditivas; no elimina bases ni usa Docker.

```powershell
$env:CLEANHOME_TEST_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=cleanhome_test;Username=cleanhome_test_user;Password=TU_CLAVE_LOCAL'
./scripts/Test-Api.ps1
```

La integración recorre identidad, evidencia, verificación, solicitud/cotización/reserva, aislamiento de datos, chat real SignalR y confirmación de pagos con una pasarela de pruebas inyectada. Las pruebas de adaptadores validan requests y HMAC con HTTP simulado. Ejecutar además pruebas sandbox en ambas cuentas para validar las credenciales, dominios, webhooks y comportamiento del proveedor real.

El CI usa PostgreSQL nativo en Ubuntu, build con warnings como errores, xUnit/TRX, frontend lint/build/audit, Gitleaks y Semgrep. No se ha hecho push ni se ha ejecutado GitHub Actions desde esta sesión. Consultar `docs/ENTREGA.md` para resultados efectivamente obtenidos y pendientes de configuración externa.
