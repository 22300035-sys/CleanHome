# Corrección de confirmación de correo — 5 noviembre 2026

## Causa comprobada

Identity MapIdentityApi entrega confirmationLink ya codificado para HTML. SmtpIdentityEmailSender aplicaba HtmlEncode otra vez: el HTML contenía &amp;amp;code y al pulsar el enlace el navegador enviaba un parámetro amp;code en lugar de code. La cuenta se creaba y SMTP entregaba el mensaje, pero faltaba el parámetro requerido y el endpoint devolvía 400. Se contrastó con la implementación oficial de ASP.NET Core Identity:
https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs

## Cambios

- El remitente normaliza una sola capa HTML antes de codificar el atributo href; se conserva el escape de comillas/HTML. Se aplica también al enlace de recuperación.
- Middleware exclusivo del GET confirmEmail repara los nombres amp;code y amp;changedEmail de mensajes antiguos cuando falta el nombre normal. No modifica el token ni confirma cuentas por fuera de Identity.
- En el alojamiento con frontend integrado, peticiones de navegador text/html redirigen después de validar hacia acceso?confirmacion=correcta o error; no incluyen tokens ni userId en el destino. Respuestas no-store y no-referrer. Clientes API conservan sus códigos de estado y comportamiento, y desarrollo sin frontend integrado conserva la respuesta original.
- Acceso muestra el resultado en español y permite solicitar reenvío mediante el endpoint de Identity usando solo el campo correo. La respuesta es genérica para evitar revelar cuentas existentes.

## Validación

- Suite: 29 unitarias y 8 integraciones aprobadas; cero omitidas. Las dos nuevas variantes prueban registro con confirmación obligatoria en Production, login bloqueado antes de confirmar, enlace renderizado normal/antiguo, confirmación efectiva en base, login posterior, recuperación y cambio de contraseña, rechazo de token inválido y compatibilidad API. SMTP sustituido por captura de mensajes; solo base cleanhome_test.
- Después del ajuste de navegación limitado al frontend integrado se repitieron las dos nuevas variantes: aprobadas.
- Build frontend, TypeScript y ESLint aprobados. Publish .NET Release con warnaserror aprobado.
- Navegador headless contra dominio público comprobó enlace inválido -> acceso con error, presentación del aviso de éxito y reenvío interceptado sin enviar correo real. Captura privada email-confirmation-ui.png. No se usaron enlaces/tokens reales del usuario en las pruebas.

## Publicación y comprobación del usuario

Se respaldaron binarios anteriores en .local-services/cloudflare/before-email-fix-*, se publicaron backend y frontend y se reinició solo la API. PostgreSQL, documentos, claves persistidas, SMTP y proveedores de pago se conservan. No hubo migraciones ni push.

El usuario puede volver a pulsar el enlace anterior: funcionará si su token sigue vigente. Si caducó, abrir /acceso, escribir el correo y pulsar Reenviar confirmación. La cuenta ya creada no necesita registrarse nuevamente. El email real y el acceso de esa cuenta concreta deben ser comprobados por el usuario; no se afirma que se hayan confirmado durante esta ejecución.
