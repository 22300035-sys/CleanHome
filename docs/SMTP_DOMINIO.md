# SMTP del dominio para CleanHome original

Remitente propuesto: **notificaciones@dorms21.win**. Esta configuración corresponde a CleanHome original; no cambia CleanHome A.

## Estado

El código de correo existe, pero aún no hay proveedor/credencial SMTP configurados ni envío real verificado. Google Labs no es un proveedor de correo. Google Workspace permite usar el dominio con Gmail, pero requiere un plan de pago. Cloudflare Email Routing recibe/reenvía correo; Email Sending a destinatarios arbitrarios requiere Workers Paid según la oferta consultada. Tener el dominio en Cloudflare no configura por sí solo correo saliente.

Para conservar el objetivo gratuito se propone Resend Free: verificar su cuota vigente y no activar planes pagados. El proveedor sigue pudiendo limitar envíos y aplicar políticas antiabuso: usar un dominio propio no garantiza evitar spam o restricciones.

## Preparar el dominio con Resend

1. Crear/iniciar sesión en https://resend.com con la cuenta del propietario y mantener el plan Free.
2. En Domains, agregar dorms21.win. Copiar los registros SPF/DKIM que genere para esta cuenta; no inventar valores ni reutilizar los de otra cuenta.
3. Añadir esos registros en DNS de Cloudflare. No sobrescribir MX existentes ni crear un segundo SPF en el mismo nombre. Seguir los nombres de subdominio de retorno que indique Resend; los registros de correo no usan proxy naranja.
4. Esperar el estado Verified. Revisar DMARC existente antes de añadir/cambiar una política; empezar sin rechazo de correo legítimo mientras se comprueba alineación.
5. Crear una API key limitada a envío y al dominio verificado cuando la interfaz permita esa restricción. Introducirla solo en la terminal, no en chat, Git, .env frontend o capturas.

El script ya funciona con STARTTLS, compatible con smtp.resend.com:587 y usuario resend. No usar puerto 465 con System.Net.Mail.SmtpClient: este adaptador usa STARTTLS, no TLS implícito.

## Guardar y comprobar de forma privada

Desde la raíz del proyecto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Configure-DomainSmtp.ps1 -SendTest
```

El script pide la clave de manera oculta y después tu dirección para una única prueba expresamente solicitada. Solo guarda email.json después de que la prueba tenga éxito si se usa -SendTest. La clave queda cifrada con DPAPI en .local-services/cloudflare/email.json, ignorado por Git y los paquetes de entrega. SmtpAcceptedAt acredita aceptación del servidor SMTP, no recepción garantizada en bandeja de entrada. Confirmar Recibidos/Spam y eventos del proveedor.

Para el futuro servidor, usar otra clave de envío y configuración propia; no copiar el archivo cifrado de la laptop ni la credencial administradora de Cloudflare.

## Configuración de la API

Import-SmtpConfiguration.ps1 carga desde ese archivo los valores Email__Host, Email__Port, Email__Username, Email__From y Email__Password (desencriptada solo en memoria); rechaza una configuración sin prueba aceptada. Start-CleanHome.ps1 lo usa en el proceso backend cuando existe el archivo. No basta con crearlo para cambiar un proceso ya iniciado: detener/reiniciar la API local o la futura instancia pública. Production conserva confirmación de correo; no se debe quitar para ocultar un SMTP averiado.

Antes de habilitar cuentas reales, probar registro, enlace de confirmación HTTPS con el hostname público, recuperación y rechazo de códigos incorrectos/vencidos. El build y las pruebas locales no acreditan el envío externo.

Mantener rate limits en confirmación/recuperación y no enviar listas masivas. No reintentar automáticamente envíos rechazados por cuota o rebote permanente. El uso previsto son confirmación y recuperación solicitadas por el usuario, no campañas comerciales.

## Fuentes consultadas

- https://resend.com/pricing
- https://resend.com/docs/send-with-smtp
- https://developers.cloudflare.com/email-service/platform/pricing/
- https://knowledge.workspace.google.com/admin/getting-started/editions/business-editions
