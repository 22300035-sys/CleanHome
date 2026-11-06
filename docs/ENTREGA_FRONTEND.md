# Entrega CleanHome — 4 de noviembre de 2026

## Implementación

Frontend conectado a la API: registro/login y recuperación, inicio de cliente, solicitud en tres pasos, catálogo, propuestas, perfil público del profesional, aceptación de cotización, reservas, checkout Stripe/Mercado Pago, perfil, domicilios, documentos privados, notificaciones, chat, radar de trabajos, envío de cotización, historial de importes y administración con revisión, suspensión, verificación, reservas, solicitudes, pagos y auditoría.

La interfaz conserva tipografías Inter/Plus Jakarta Sans, colores, iconos, fotografías, tarjetas y navegación de Figma. Se consultaron 17 pantallas de referencia. Los 376 recursos originales descargados se incluyen localmente, sin URLs temporales. El límite de herramientas del plan Figma impidió leer todas las variantes restantes; no se afirma reproducción exacta de los 43 marcos. Las vistas adicionales reutilizan el mismo sistema visual. La tabla de referencias está en FIGMA_IMPLEMENTACION.json.

Se añadió diseño móvil/escritorio, navegación accesible, foco y diálogos nativos, estados de carga/error/vacío, y una demostración aislada con tres roles. La PWA precarga únicamente recursos estáticos; no guarda API, documentos privados ni tokens. La sesión real vive en memoria y requiere nuevo login después de recargar.

## Validación realizada

- Compilación Release de .NET sin errores ni advertencias; 20 pruebas unitarias y una prueba de integración extensa aprobadas con PostgreSQL 18 nativo.
- Frontend: TypeScript/build de producción y ESLint aprobados.
- Nueve pruebas Playwright aprobadas sin omisiones: pantallas y recursos, recorrido completo de demostración, chat y texto sin ejecución HTML, autorización administrativa, error de login, alta y solicitud reales, PWA sin conexión, anchos 320/768/1440 y flujo real de documentos/aprobación/cotización/reserva/chat SignalR entre participantes.
- Auditoría npm: cero vulnerabilidades reportadas al consultar el registro. Evidencias XML/TRX y JSON en evidence; capturas en screenshots.
- Pruebas reales ejecutadas con Brave headless y usuarios ficticios en una base de pruebas local. No se efectuaron cargos ni reembolsos externos. Las pruebas de proveedores usan transporte simulado.

## Cambios de API necesarios para las pantallas

Consulta mínima de profesional verificado sin correo ni identificador de Identity; lectura administrativa de solicitudes y reservas; nombres de participantes solo dentro de una reserva autorizada; edición/eliminación de domicilios propios sin alterar el historial de solicitudes. Se añadieron pruebas de propiedad y autorización. Las transacciones serializables coordinan cambios de domicilio y publicación; conflictos de referencias se traducen a 409. No fue necesaria otra migración ni se destruyeron datos.

## Pendiente para operar en producción

Configurar y comprobar credenciales, URLs HTTPS, webhooks y reembolsos reales de Stripe y Mercado Pago; SMTP, dominio, CORS, respaldos y alojamiento de documentos; ejecutar CI en el repositorio remoto y validación de despliegue. No se hizo push. Los adaptadores y pantallas existen, pero la entrega no certifica operaciones bancarias reales.

No se implementaron liquidaciones a trabajadores, custodia de fondos, reseñas públicas reales, adjuntos fotográficos de solicitudes, mapas/geolocalización dinámica, notificaciones push del sistema ni asistente de IA. La ayuda abre preguntas frecuentes. No se muestran comisiones o saldos retirables inventados. Los importes del historial son tarifas de servicios; las reseñas de ejemplo aparecen únicamente en demostración. La carga privada de documentos sí está integrada.

La PWA sin conexión permite ver recursos y usar datos de demostración, no ejecutar servicios reales sin red. Capacitor conserva su estructura Android; no se compiló ni firmó APK.

## Acceso y entrega

Se trabajó en una copia local dentro del workspace autorizado, usando herramientas locales y un navegador headless aislado. Esto no supone acceso general a otras aplicaciones, cuentas o toda la computadora. El archivo ZIP incluye código, recursos, documentación y evidencias; excluye SDK, node_modules, compilados, bases de pruebas, credenciales, documentos subidos y tokens. Arranque e instrucciones en frontend/README.md y README.md. El informe ENTREGA.md corresponde a la entrega previa de API y es histórico.
