# Mercado Pago Checkout Pro: Orders

Actualización del 5 noviembre 2026, solo CleanHome original. StripeGateway, secretos Stripe y contrato REST del frontend se conservan. No requiere migraciones de base de datos.

## Contrato implementado

- Crear `POST https://api.mercadopago.com/v1/orders`: type online, processing_mode manual, total_amount/unit_price como cadenas con punto decimal, importe calculado por el servidor, external_reference del pago interno, X-Idempotency-Key estable. Retorno config.online success/failure/pending. Se recibe checkout_url y se verifica moneda/importe. Cuenta mexicana y MXN.
- Webhook público `https://cleanhome-laptop.dorms21.win/api/v1/payments/webhooks/mercadopago`: seleccionar **Order (Mercado Pago)** en el panel. Tipo order; HMAC con data.id en minúsculas en el manifiesto y timestamp reciente. Se conserva el ID original al consultar GET /v1/orders/{id}; el body de la notificación no determina el importe ni el estado.
- processed/accredited y total_paid_amount igual al total -> Paid. refunded/refunded -> Refunded. charged_back -> Disputed. Creado, pendiente, fallido y reembolso parcial no se convierten en cobro total confirmado. El servicio mantiene sus comprobaciones de referencia/importe/moneda y su idempotencia.
- Reembolso completo POST /v1/orders/{id}/refund sin body de monto parcial. Solo confirma éxito con order ID coincidente y estado refunded/refunded. IDs de pago legacy se rechazan para evitar reembolsar un recurso incorrecto. Reembolsos parciales y conciliación de disputas continúan siendo administrativos.

## Configuración pendiente en tu cuenta

1. Aplicación **Checkout Pro / API de Orders**.
2. Vendedor y comprador de prueba. Usar el token del vendedor de prueba según las instrucciones de su panel; el prefijo APP_USR por sí solo no demuestra si la cuenta es real o ficticia. Orders no selecciona sandbox mediante sandbox_init_point.
3. Webhooks: entorno Prueba, evento **Order (Mercado Pago)**, URL anterior. Copiar firma secreta de esa misma aplicación.
4. Ejecutar desde la raíz:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Configure-TestPayments.ps1 -Provider MercadoPago
```

El script conserva la sección Stripe existente, cifra cada secreto mediante DPAPI y no lo envía al chat. Luego reiniciar solo la API pública. El cambio de código no configura automáticamente cuentas o credenciales externas.

## Validación y límites

Pruebas del adaptador con HTTP simulado: creación de order, importe y retorno, idempotencia, firma en segundos/milisegundos, ID original, consulta autenticada, estado pagado/reembolsado/contracargo, rechazo de firma adulterada/antigua, pendientes y parciales, reembolso total sin monto parcial. También se ejecuta la suite de regresión de Stripe y la integración PostgreSQL aislada. No equivale a un checkout sandbox de Mercado Pago.

Falta prueba externa completa con la aplicación del usuario: crear checkout, pagar con comprador/tarjeta de prueba, recibir webhook del proveedor, repetir notificación y comprobar reembolso. Las notificaciones simuladas del panel con IDs inventados pueden fallar al consultar Orders; no se elude esa consulta para devolver un éxito falso. No se hicieron cargos o reembolsos reales.

Fuentes oficiales consultadas:
- https://www.mercadopago.com.mx/developers/es/docs/checkout-pro-orders/create-order
- https://www.mercadopago.com.mx/developers/es/docs/checkout-pro-orders/web-integration/configure-back-urls
- https://www.mercadopago.com.mx/developers/es/docs/checkout-pro-orders/notifications
- https://www.mercadopago.com.mx/developers/es/reference/online-payments/checkout-pro/refund-order/post
- https://www.mercadopago.com.mx/developers/es/docs/checkout-pro-orders/chargebacks/notifications

## Primera solicitud externa — 5 noviembre 2026

El proveedor confirmó la cuenta mexicana test_user. La primera creación fue rechazada HTTP 400: items.external_code superaba 30 caracteres. Corregido a referencia hexadecimal de 30 caracteres y añadida regresión; 29 unitarias aprobadas tras el cambio. Se actualizó el servidor sin modificar StripeGateway ni sus credenciales.

Se creó una fixture explícita de 100 MXN en cleanhome_laptop (cuentas ficticias, domicilio ficticio, sin Admin ni trabajador verificado) para el piloto. Tras el rechazo confirmado se usó una clave de idempotencia fija nueva para la solicitud corregida, recibida HTTP 201. La order ORDTST01M6PD7NB1YK43EKHKT36FXS20 quedó vinculada al registro de pago sandbox original. GET de pago autenticado en API pública confirmó monto 100 MXN y Pending. Aún falta que el usuario complete checkout con comprador/tarjeta de prueba y verificar webhook del proveedor. Helpers y referencias privadas en .local-services/cloudflare, excluidos de Git. No se enviaron correos ni se efectuaron cargos.

## Corrección del flujo de prueba de Orders

La documentación vigente de Checkout Pro Orders indica realizar la compra en incógnito SIN iniciar sesión en Mercado Pago, usando tarjeta de prueba; no requiere comprador test_user. La indicación anterior de iniciar sesión como comprador correspondía al flujo anterior y se corrige. Ante el error de mezcla de partes reales/de prueba, GET autenticado confirmó vendedor test_user México y coincidencia con user_id de la order. La order sigue created y no contiene transacciones de pago. Reintentar el checkout original en una sesión privada nueva sin login; no crear otro pago mientras el actual siga pendiente.
Fuente: https://www.mercadopago.com.mx/developers/es/docs/checkout-pro-orders/integration-test/test-purchase-with-card

## Prueba controlada con checkout nuevo

Tras errores del checkout anteriores, se creó otra reserva ficticia independiente de 100 MXN y se generó su checkout a través de la API pública autenticada. Nueva order: ORDTST01M6Q03PRV8JDJXWPBBV24F1EW. Estado observado: Pending en CleanHome, sin confirmación de pago. La primera consulta local terminó sin respuesta útil (HTTP 0); antes de reintentar se consultó GET payment, recuperando la orden ya creada sin duplicarla. Referencias en sandbox-reservation-fresh.json y sandbox-checkout-fresh.json privados. No se alteró el pago original ni Stripe.

Se comprobó que retries.allowed=false también aparece en ejemplos oficiales: no constituye evidencia suficiente de la causa del rechazo. La preferencia interna contiene un sandbox_init_point, pero no se usa: el flujo Orders documenta checkout_url. No se cambió a Preferences ni se sustituyó el host de checkout sin respaldo del contrato. La causa del fallo de procesamiento sigue sin confirmarse; la siguiente prueba de usuario aislará checkout nuevo frente al usado previamente.

## Aislamiento de personalizaciones: order mínima

El PUT observado pertenece a https://www.mercadopago.com.mx/checkout/v1/payment/api/flow/... (checkout alojado), no a un PUT /v1/orders de CleanHome. El adaptador solo usa POST /v1/orders, GET /v1/orders/{id} y POST refund.

Se creó una reserva ficticia independiente de 100 MXN reutilizando exclusivamente participantes ficticios existentes, con nuevo PaymentId d0f84bf8-ddff-4fae-bf53-6b60694cd1e2. Tras verificar test_user, POST mínimo obtuvo 201 y order ORDTST01M6Q2V89Q2J5V3MGB5QR5VZXW, vinculada al pago interno. Payload limitado a type online, processing_mode manual, total_amount, external_reference e items title/quantity/unit_price. Sin config, return URLs, auto_return, external_code, payer ni exclusiones. Archivo privado sandbox-checkout-minimal.json y payload .request.json. Estado Pending; falta pago de usuario para comparar. No se modificaron el adaptador ni claves Stripe para este diagnóstico. No se efectuó pago ni se presentó esta creación como validación completa.

## Nueva evidencia: timeout de tokenización en checkout mínimo

La captura del usuario del checkout mínimo muestra: [BRICKS ERROR]: Fail to create card token: Request timed out. Esto identifica un fallo de tokenización en ese intento, sin demostrar que todos los intentos previos tengan la misma causa. Comprobación HTTP pública sin tarjeta ni claves: api.mercadopago.com respondió 401 en 1,21 s (respuesta esperable sin autorización); sdk.mercadopago.com/js/v2 agotó 12 s. El panel público de estado reporta Checkout Pro operativo; eso no excluye problemas regionales o del trayecto del usuario. Aislar con otra red y navegador limpio antes de cambiar de nuevo el adaptador. Los avisos de fontSize/configuración de campos pertenecen al checkout alojado, no al frontend CleanHome.
