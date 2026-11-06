# Recuperación de contraseña — 5 noviembre 2026

La cuenta indicada por el usuario se comprobó confirmada. No se encontraron excepciones SMTP en los registros existentes; esto no demuestra entrega en buzón ni la causa de la falta de recepción.

## Problemas de código corregidos

- Identity llama SendPasswordResetCodeAsync, no SendPasswordResetLinkAsync, en forgotPassword. El mensaje anterior contenía únicamente el código. Ahora incluye enlace al formulario y código manual de respaldo.
- Recovery/ResetPassword y reenvío de confirmación usaban el transporte del contexto, que puede ser DemoApi. DemoApi simula solicitudes auth sin enviar correo. Ahora usan liveApi ante la acción explícita del usuario; visitar la página no envía correo.
- El enlace usa /restablecer#email=...&code=..., con valores escapados. El fragmento no se envía al servidor; el formulario lo lee y lo retira de la barra de dirección. Abrir el enlace no cambia contraseñas: se exige nueva contraseña y token válido en resetPassword.
- Email:PublicBaseUrl se configura en el arranque público con el dominio HTTPS de la laptop. Validación de origen evita esquemas inseguros. Sin esa configuración se conserva código manual para instalaciones anteriores.
- SMTP registra solo asunto/tipo y código de estado, sin destinatario, contenido, contraseña o token. Una excepción SMTP produce 503 genérico visible en frontend. Aceptación SMTP no equivale a entrega en bandeja de entrada.

## Evidencia

29 unitarias y 8 integraciones aprobadas. Las integraciones extraen el enlace del HTML de recuperación y usan su código para cambiar la contraseña de usuarios ficticios únicamente en cleanhome_test; SMTP simulado. TypeScript, build, lint y publish Release aprobados.

Prueba headless sobre el dominio público: recuperación desde contexto demo realiza llamada al transporte real; enlace precarga correo/código y elimina fragmento; envío de nueva contraseña conserva el código correcto. Todas las peticiones auth interceptadas en esta comprobación: no se enviaron correos ni cambiaron contraseñas reales.

Backend y frontend publicados con respaldo before-recovery-* y reinicio exclusivo de API. Sin migraciones ni push. Pendiente comprobar la recepción real del nuevo correo con el usuario. Si el proveedor acepta SMTP pero Hotmail no recibe, revisar el evento de entrega en Resend (delivered/bounced/suppressed/delayed) y Spam; no desactivar confirmación ni cambiar contraseña desde base de datos.
