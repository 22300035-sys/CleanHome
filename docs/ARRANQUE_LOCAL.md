# Arranque local de CleanHome en esta laptop

Estos scripts corresponden a `cleanhome-audit/CleanHome-main`, la app original en funcionamiento. **CleanHome A conserva su entorno independiente y no se modifica en este cambio.**

Desde PowerShell o Warp:

```powershell
cd "C:\Users\Doria\OneDrive\Documentos\ChatGPT\dorms21\cleanhome-audit\CleanHome-main"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-CleanHome.ps1
```

Arranca PostgreSQL nativo, backend y frontend en ese orden; reutiliza los que ya responden, espera readiness y mantiene los procesos en segundo plano. No abre ventanas ni aplica migraciones. No necesitas npm global ni .NET global en esta laptop: el archivo privado de configuración apunta a los runtimes ya instalados.

| Servicio | Dirección |
|---|---|
| Frontend | http://127.0.0.1:5173/acceso |
| Demo | http://127.0.0.1:5173/inicio?demo=1 |
| API | http://127.0.0.1:5186 |
| Swagger UI | http://127.0.0.1:5186/swagger |
| OpenAPI JSON | http://127.0.0.1:5186/openapi/v1.json |
| Readiness | http://127.0.0.1:5186/health/ready |
| PostgreSQL nativo | 127.0.0.1:55432 |

## Comandos por servicio

```powershell
.\scripts\Start-CleanHome.ps1 -Service Database
.\scripts\Start-CleanHome.ps1 -Service Api
.\scripts\Start-CleanHome.ps1 -Service Frontend
```

Para ver salida en la misma terminal, usa `-Foreground` en Api o Frontend y una terminal separada por servicio. La API necesita PostgreSQL iniciado. Logs de los procesos de fondo: `.local-services/logs`. Comprueba errores ahí si un puerto tarda o no responde.

## Detener y recompilar

```powershell
.\scripts\Stop-CleanHome.ps1
.\scripts\Start-CleanHome.ps1 -Build
```

Stop detiene exclusivamente procesos identificados por PID/fecha y registrados por Start, y cierra PostgreSQL con checkpoint. Conserva los datos. `-KeepDatabase` deja PostgreSQL activo. Los procesos iniciados manualmente antes del script se cierran con Ctrl+C en su terminal; no se matan procesos ajenos automáticamente. No uses -Build con el backend ejecutándose, porque Windows bloquea las DLL.

## Iniciar automáticamente al entrar a Windows (opcional)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Set-CleanHomeAutostart.ps1
```

Esto registra **CleanHomeLocal** en Run del usuario actual y no necesita servicio Windows ni contraseña. Se ejecuta al iniciar sesión, no antes de entrar a Windows. No se activa solo por entregar estos scripts. Si mueves la carpeta, vuelve a configurar la entrada. Los procesos usan loopback: no se exponen a la red.

Para desactivarlo:

```powershell
.\scripts\Set-CleanHomeAutostart.ps1 -Disable
```

## Swagger

Usa el generador OpenAPI existente de ASP.NET Core y los recursos locales de Swashbuckle SwaggerUI; no depende de un CDN. UI/JSON solo se habilitan en Development o con `OpenApi__Enabled=true`. Los scripts locales usan Development. En producción ambos permanecen deshabilitados por defecto.

Para probar rutas privadas: ejecuta `POST /api/v1/auth/login`, copia únicamente `accessToken`, pulsa **Authorize** y pega el token. Swagger agrega `Bearer`; no pegues comillas ni el refreshToken. La autorización no se conserva al recargar. Las operaciones ejecutadas en Swagger modifican la base local real; pagos externos todavía requieren configuración del proveedor.

## Configuración privada y persistencia

`.local-services/settings.json` queda fuera de Git. Contiene rutas a Node/.NET/PostgreSQL, directorio de datos, puertos y `ConnectionStringProtected` cifrada con DPAPI del usuario Windows. Reiniciar conserva ese archivo, la base y App_Data. No compartas este archivo ni App_Data. El cifrado está ligado a este usuario/equipo; para otra laptop hay que configurar su propia conexión, no copiar credenciales cifradas.

Esta laptop continúa usando la base ficticia `cleanhome_test` ya existente, sin crear ni migrar otra base. No es una instalación de producción. SMTP y pasarelas deben configurarse por separado; no hay cargos reales configurados por estos scripts.

Para recrear la configuración en otro entorno, copia las claves de configuración necesarias a un archivo privado y genera el valor cifrado desde una cadena suministrada de forma local:

```powershell
$secureConnection = Read-Host 'Connection string PostgreSQL local' -AsSecureString
$protectedConnection = $secureConnection | ConvertFrom-SecureString
```

Campos requeridos: DotnetPath, DotnetCliHome, NugetPackages, NodePath, PostgresBin, PostgresData, DatabasePort, ApiPort, FrontendPort y ConnectionStringProtected. No imprimir el valor desencriptado en logs ni guardarlo en el historial. Los runtimes en `.tools` y el directorio PostgreSQL deben conservarse al reiniciar; si se borran, los scripts informarán el recurso que falta.
