> Informe histórico de API. Consulta [la entrega actual de aplicación](ENTREGA_FRONTEND.md) para los cambios y validaciones del 4 de noviembre.

# Entrega de CleanHome API — 3 de noviembre de 2026

## Implementado

- Identidad ASP.NET Core Identity: registro, login bearer, refresh, recuperación/confirmación por SMTP, 2FA y logout que invalida refresh.
- Perfiles Customer/Worker, domicilios privados y catálogo público; alta/desactivación administrativa del catálogo.
- Solicitudes, cotizaciones, aceptación transaccional, disponibilidad, reservas y reglas de estado.
- Documentos privados PDF/PNG/JPEG, revisión, verificación de trabajador, suspensión y auditoría.
- Chat persistente y SignalR, con acceso limitado a participantes; notificaciones por usuario.
- Stripe Checkout y Mercado Pago Checkout Pro, HMAC de webhook, consulta al proveedor, validación de importes/moneda y reembolso completo administrativo.
- Problem Details, CORS explícito, rate limiting, autorización bearer, liveness/readiness y OpenAPI con esquema de autenticación.
- Migración aditiva, SQL idempotente de revisión, pruebas y CI con PostgreSQL nativo sin Docker.

## Evidencia local

La solución de seis proyectos compiló en Release con `--warnaserror`: cero errores y cero advertencias.

Pasaron 20 pruebas unitarias y una integración de extremo a extremo con numerosos casos positivos/negativos sobre PostgreSQL 18.0 nativo y un usuario de base sin privilegios administrativos. No se omitieron pruebas. Los archivos TRX finales se incluyen en `evidence/`.

La integración verifica registro/login, perfiles, prevención de cambio de rol, 401/403, documento inválido y descarga ajena, revisión/verificación, domicilio ajeno, cotización duplicada, aceptación ajena y doble aceptación, horarios superpuestos, dos aceptaciones simultáneas (una 201 y otra 409), mensaje persistente y recepción SignalR real, pago sin firma y monto adulterado, confirmación idempotente, reembolso, protección frente a evento Paid posterior a Refunded, suspensión, refresh y logout.

Las pasarelas de esa integración son dobles de prueba inyectados. Las unitarias de adaptadores comprueban el request enviado a Stripe/Mercado Pago, HMAC, rechazo de alteración y timestamps antiguos mediante HTTP simulado. No se contactaron cuentas de pagos ni se hicieron cargos/reembolsos reales.

Comprobaciones adicionales realizadas:

- `/health` respondió healthy; `/health/ready` respondió ready.
- OpenAPI exportado desde la API ejecutada: 43 rutas y 51 operaciones documentadas. SignalR no forma parte del contrato REST.
- Tres migraciones aplicadas solo en cleanhome_test; 18 tablas funcionales más historial de migraciones.
- EF reportó que no hay cambios de modelo pendientes.
- NuGet no reportó dependencias vulnerables, incluidas transitivas, en la fuente consultada durante esta sesión; esto no equivale a un pentest.
- El comando de bootstrap creó correctamente un administrador ficticio en la base de pruebas.
- La cuota de diez documentos también se comprobó con dos cargas simultáneas compitiendo por el último espacio.
- SQL idempotente generado en `migrations-review.sql`, sin ejecutarlo sobre bases del usuario.

## Archivos para revisar

- `../README.md`: requisitos, arranque, configuración, flujo y operación.
- `openapi.json` y `ENDPOINTS.md`: contrato e inventario exportados.
- `FLUJO.http`: ejemplos de peticiones con marcadores, sin tokens reales.
- `ARQUITECTURA.md`: capas y relaciones de datos.
- `migrations-review.sql`: script de revisión antes de aplicar migraciones.
- `nuget-audit.json`: resultado de la consulta de avisos NuGet en la sesión.
- `evidence/`: resultados TRX finales.

## Configuración externa pendiente

Para cobrar con cuentas reales hacen falta las claves de Stripe y Mercado Pago, secretos y registro de webhooks, URLs HTTPS públicas y una prueba sandbox con ambas cuentas. No se solicitaron ni guardaron claves privadas. No hay dispersión automática a trabajadores, reembolsos parciales ni resolución automática de disputas/checkout interrumpido; estos requieren conciliación del comercio.

Producción también requiere SMTP, dominio/TLS, política de datos y respaldo de PostgreSQL/documentos/claves, permisos y cifrado del almacenamiento, y escáner antivirus/CDR conectado antes de aceptar documentos de usuarios externos. No se ha realizado un pentest, una prueba de carga, despliegue ni validación de GitHub Actions. La existencia del workflow no equivale a un pipeline ejecutado.

El frontend React/Capacitor del ZIP no se desarrolló como parte de esta entrega de API y conserva la plantilla inicial. La API puede integrarse mediante el contrato exportado.

## Alcance del acceso

Se leyó el ZIP de Descargas y se trabajó en una copia dentro de este workspace. SDK y PostgreSQL son portátiles dentro de `.tools`, fuera del paquete fuente; no se instalaron globalmente. La única base modificada fue cleanhome_test de la instancia local aislada en el puerto 55432. No se accedió a secretos existentes ni cuentas remotas; no se usó Docker, no se ejecutaron migraciones destructivas y no se hicieron commits ni push. No se modificó ni eliminó el ZIP original; al empaquetar ya no estaba disponible en la ruta de Descargas, por lo que se conserva su hash registrado al inicio y no se presenta una comparación binaria final con ese archivo. La API y PostgreSQL temporales se detuvieron al terminar las comprobaciones.
