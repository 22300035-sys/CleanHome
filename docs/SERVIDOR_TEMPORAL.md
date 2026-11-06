# CleanHome original: servidor temporal — 5 noviembre 2026

URL principal: https://cleanhome.dorms21.win/ (landing) y https://cleanhome.dorms21.win/acceso (aplicación). El dominio anterior se conserva para compatibilidad.

Cloudflare Tunnel -> HTTP loopback 127.0.0.1:5190 -> API Production y React compilado. PostgreSQL nativo permanece en loopback:55432. No se usa Docker ni Vite en la ruta pública. Swagger/OpenAPI públicos desactivados. Se exige confirmación de correo para iniciar sesión.

Base nueva `cleanhome_laptop`, rol propio con contraseña aleatoria, tres migraciones aplicadas mediante SQL idempotente a la base vacía. No se copiaron usuarios ficticios ni se modificó `cleanhome_test`. Documentos y claves están separados de desarrollo. SMTP importa la configuración ya probada por el usuario; no se enviaron correos durante esta publicación.

## Operación desde PowerShell en la raíz del repositorio

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-PublicCleanHome.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Stop-PublicCleanHome.ps1
```

Los procesos se lanzan ocultos y no dependen de que permanezca abierto el chat. Apagar, suspender o cerrar sesión en Windows interrumpe el alojamiento. No se configuró inicio automático ni recuperación tras fallos. Después de reiniciar, ejecutar Start-PublicCleanHome. El autostart de desarrollo existente no inicia este despliegue.

Configuración, credenciales cifradas DPAPI, credencial del túnel, logs y capturas: `.local-services/cloudflare`, excluida de Git. No compartir ni copiar esta carpeta al repositorio o al servidor Ubuntu: SMTP/DB requieren configuración propia allí. Las claves Data Protection persistidas aún no tienen cifrado de archivo explícito. No publicar respaldos o documentos.

La publicación es una instalación nueva y aún no tiene administrador. No se elevó ninguna cuenta ni se reutilizó el administrador ficticio. Preparar bootstrap administrativo con contraseña privada antes de realizar operaciones de verificación de trabajadores.

## Evidencia nueva

- Publish .NET Release con warnaserror aprobado; build TypeScript/Vite y ESLint aprobados.
- 20 unitarias y 6 integraciones aprobadas, cero omitidas, usando exclusivamente cleanhome_test. TRX en `.local-services/cloudflare/test-results`.
- HTTPS público: acceso, catálogo y readiness 200; reservas anónimas 401; Swagger y archivo privado de configuración 404.
- Las pruebas no certifican pagos reales ni un recorrido de registro/correo externo completo. Los adaptadores de pago usan transporte simulado en integración.

## Brechas priorizadas del código y operación

| Prioridad | Pendiente | Evidencia y cierre necesario |
|---|---|---|
| P0 antes de datos reales | Respaldo y restauración | No hay rutina del despliegue para respaldar juntos PostgreSQL, documentos y claves. Implementar copia cifrada externa y probar restauración. |
| P0 antes de documentos reales | Escaneo de archivos | DocumentsController valida tamaño/tipo y permisos, LocalPrivateFileStore almacena sin antivirus/CDR. Integrar cuarentena/escáner o deshabilitar carga pública hasta resolverlo. |
| P0 antes de cobrar | Stripe y Mercado Pago | No se configuraron credenciales ni webhooks públicos en esta instancia. Probar sandbox por proveedor, firma, reintentos, importes, confirmación y reembolso antes de producción. No hay dispersión automática a trabajadores. |
| P1 | Alta completa y correo | SMTP fue probado por el usuario. Falta probar registro, confirmación y recuperación desde el dominio público. Access/context conservan nombre de registro solo durante intento de login; con confirmación obligatoria, el primer login posterior puede crear perfil con el prefijo del correo. Corregir onboarding persistente o solicitar nombre tras confirmar; comprobar selección de rol. |
| P1 | Administrador | La base pública está nueva: requiere bootstrap privado explícito y prueba de revisión/verificación/suspensión. |
| P1 | Continuidad y monitoreo | Sin supervisor, arranque automático, alertas ni restauración ante caídas. La laptop necesita energía y red permanentes. |
| P1 | Privacidad y almacenamiento | Documentos y claves están en workspace bajo OneDrive; verificar política de sincronización, ACL, cifrado en reposo, retención y aviso de privacidad antes de datos personales. |
| P1 | Validación del despliegue | Repetir E2E contra instalación aislada equivalente a Production; las suites existentes live utilizan entorno Testing y no prueban confirmación obligatoria. CI de cambios nuevos aún no enviado/ejecutado. |
| P2 producto | Funciones adicionales | Reseñas reales, mapas dinámicos, fotos en solicitudes, push y liquidaciones no implementados. No confundir pantallas demo con servicios reales. APK Android sin compilar/firmar. |

El flujo principal y las pantallas existen; este despliegue sirve para revisión y piloto controlado. Terminar los P0 antes de admitir información sensible o dinero real. No se hicieron commit, push, cambios a CleanHome A ni migraciones destructivas.

## Landing y dominio principal — 5 noviembre 2026

Se publicó / como landing pública, con ilustración vectorial local, pasos para contratar, accesos para cliente/trabajador y FAQ. Los CTA dirigen a /acceso. El área autenticada continúa en /inicio y no se mezclan los datos de demostración con las solicitudes de la landing.

Dominio principal: https://cleanhome.dorms21.win. Nuevo DNS en el túnel existente. Páginas del hostname anterior redirigen temporalmente al nuevo conservando path/query. APIs, webhooks y confirmación permanecen operativos en el anterior. Los nuevos mensajes de recuperación y retornos de checkout usan el nuevo dominio; no se rotaron claves ni se cambiaron los endpoints ya registrados en los proveedores.

Validación nueva: 29 unitarias + 8 integraciones; publish Release, build frontend y ESLint aprobados. Headless público en 320/390/768/1440: sin overflow, imágenes rotas ni errores JavaScript; CTA abre login. Enlace de recuperación en dominio anterior conserva el fragmento al pasar al nuevo y la pantalla elimina el token de la barra de dirección. API readiness 200, reservas anónimas 401 y Swagger 404 en ambos dominios. Capturas privadas landing-1440.png y landing-390.png.

Se realizó exactamente una solicitud real de recuperación previamente autorizada por el usuario antes del cambio de dominio: HTTP 200 y SMTP accepted message: Restablece tu contraseña de CleanHome. Esto confirma aceptación SMTP, no llegada a Hotmail. Las pruebas posteriores no enviaron correo. Sin push, migraciones ni cambios a CleanHome A.
