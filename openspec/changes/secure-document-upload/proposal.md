# Propuesta: verificación documental segura en CleanHome

## Why

CleanHome necesita verificar la identidad de sus clientes y trabajadores,
así como los domicilios donde se solicitarán servicios de limpieza.

Actualmente existen entidades para documentos, ubicaciones y cuarentena,
pero todavía no contamos con un proceso completo y seguro de carga,
análisis, autorización y revisión.

Debemos impedir que archivos maliciosos, contenido inapropiado o documentos
sin verificar habiliten operaciones dentro de la plataforma.

## What Changes

- Implementar un sistema privado de carga de fotografías y documentos.
- Exigir identificación oficial y fotografía personal tanto a clientes
  como a trabajadores.
- Exigir un comprobante de domicilio por cada ubicación del cliente.
- Permitir formatos JPEG, PNG y PDF bajo límites estrictos.
- Validar el formato real y la estructura del archivo.
- Mantener archivos nuevos en cuarentena.
- Integrar análisis antimalware mediante herramientas gratuitas.
- Incorporar moderación de imágenes para detectar contenido inapropiado.
- Aplicar controles también a imágenes contenidas en documentos PDF.
- Requerir revisión administrativa para la aprobación documental.
- Permitir acceso al propietario y a administradores autorizados.
- Limitar el acceso de soporte a incidencias justificadas.
- Registrar accesos y decisiones de revisión.
- Impedir que clientes sin verificar contraten servicios.
- Impedir que trabajadores sin verificar ofrezcan servicios.
- Exigir ubicaciones aprobadas para solicitar limpieza.
- Preparar almacenamiento privado compatible con Microsoft Azure.
- Mantener los componentes desacoplados de proveedores de pago.

### Cambios incompatibles previstos

**BREAKING:** el flujo de contratación deberá referenciar ubicaciones
registradas y aprobadas, en lugar de aceptar únicamente una dirección
proporcionada durante la creación de la solicitud.

**BREAKING:** las operaciones de contratación y publicación requerirán
verificación documental completa, además del correo verificado.

Estos cambios se introducirán mediante una transición controlada para
no perder datos ni romper reservas existentes.

## Capabilities

### New Capabilities

- `secure-file-upload`: recepción, validación, cuarentena,
  procesamiento y almacenamiento privado de archivos.

- `document-content-moderation`: análisis antimalware, detección
  de contenido inapropiado y gestión de resultados inciertos.

- `identity-verification`: verificación documental de identidad
  de clientes y trabajadores mediante revisión autorizada.

- `verified-customer-locations`: registro y aprobación de domicilios
  de clientes mediante comprobantes privados.

- `private-document-access`: autorización, consulta restringida
  y auditoría de accesos a documentos privados.

- `verified-marketplace-eligibility`: reglas que condicionan
  contratación, cotizaciones y publicación al cumplimiento
  de las verificaciones obligatorias.

### Modified Capabilities

No se declaran capacidades existentes modificadas hasta revisar
si ya hay especificaciones oficiales en openspec/specs/.

## Impact

### Backend

- ASP.NET Core 10.
- CleanHome.Domain.
- CleanHome.Application.
- CleanHome.Infrastructure.
- CleanHome.Api.
- Políticas de autorización y servicios de contratación.
- Configuración de Entity Framework Core y PostgreSQL.

### Datos

Se reutilizarán y ampliarán cuando sea necesario:

- CustomerLocation.
- PrivateDocument.
- DocumentAccessAudit.
- WorkerDocument.
- ServiceRequest y ServiceRequestAddress.

Las migraciones deberán conservar datos y compatibilidad con
las operaciones ya registradas.

### Frontend

Se añadirán posteriormente:

- Carga de identificación oficial.
- Carga de fotografía personal.
- Registro y verificación de domicilios.
- Consulta de estados documentales.
- Corrección de documentos rechazados.
- Panel de revisión administrativa.

### Seguridad y privacidad

- Archivos privados y no accesibles públicamente.
- Denegación por defecto.
- Acceso administrativo restringido.
- Soporte únicamente con incidencia justificada.
- Auditoría de consultas y decisiones.
- Eliminación de contenido activo cuando corresponda.
- Bloqueo de archivos pendientes, maliciosos o rechazados.
- Conservación y eliminación conforme a una política de privacidad.

### Infraestructura y costos

- Despliegue objetivo en Microsoft Azure.
- Herramientas gratuitas y de licencias compatibles.
- Sin APIs comerciales de moderación.
- Sin Docker como requisito de desarrollo.
- Azure Blob Storage privado como implementación futura.
- Evaluación previa de consumo de cómputo y almacenamiento.
- Objetivo económico: evitar gastos personales y cargos inesperados.

### Fuera del alcance

- Reconocimiento facial biométrico.
- Validación automática de autenticidad de identificaciones.
- Contratación de servicios comerciales.
- Despliegue inmediato a producción.
