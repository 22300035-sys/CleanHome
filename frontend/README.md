# CleanHome frontend

React 19, TypeScript, Vite, Capacitor y SignalR. Node.js 24 y npm. Desde esta carpeta:

```powershell
npm ci
Copy-Item .env.example .env.local
npm run dev -- --host 127.0.0.1
```

Abre http://127.0.0.1:5173/inicio?demo=1 para una demostración local sin cobros. El selector superior cambia entre cliente, trabajador y administración. Los cambios de demostración se pierden al recargar y no llaman a la API.

Para datos reales abre `/acceso`, registra las cuentas y arranca la API según el README principal. Configura `CLEANHOME_DEV_API_TARGET` con su URL (predeterminado http://127.0.0.1:5186). Admin se crea con el comando bootstrap del backend; no existe registro público de administradores. Un trabajador debe subir documentos y ser aprobado por administración antes de cotizar.

```powershell
npm run lint
npm run build
npm run preview -- --host 127.0.0.1
npx playwright install chromium
npm run test:e2e
```

Preview usa puerto 5174. Desde la raíz también puedes ejecutar `./scripts/Start-Frontend.ps1` o `./scripts/Start-Frontend.ps1 -Preview`. La PWA funciona en producción/preview, HTTPS o localhost; solo precarga recursos estáticos. El modo sin conexión permite abrir la interfaz y la demostración; las operaciones reales requieren conexión. No se almacenan documentos, tokens ni respuestas de API en caché.

Los tests normales usan demostración y respuestas controladas. Para pruebas reales contra una API con entorno Testing y PostgreSQL de pruebas, establece `CLEANHOME_LIVE_TESTS=1`, `CLEANHOME_TEST_ADMIN_EMAIL` y `CLEANHOME_TEST_ADMIN_PASSWORD`. Nunca apuntes estos tests a producción: crean cuentas, solicitudes y documentos ficticios. Opcionalmente `CLEANHOME_BROWSER_EXECUTABLE` permite usar un navegador Chromium instalado. Los resultados quedan en test-results y playwright-report; las capturas verificadas en ../docs/screenshots.

Los tokens viven en memoria: recargar requiere iniciar sesión de nuevo. El retorno del checkout conserva su ruta tras login y consulta el estado al servidor. Las credenciales de Stripe, Mercado Pago y SMTP se configuran exclusivamente en el backend. Nunca pongas secretos en variables VITE_.

En hosting web, publica dist, configura fallback de rutas a index.html y un proxy seguro para /api y /hubs (WebSocket). Para Capacitor, configura VITE_API_BASE_URL con HTTPS absoluto, revisa CORS y ejecuta build y `npx cap sync android` antes de abrir Android Studio. No se generó ni firmó un APK en esta entrega.
