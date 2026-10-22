# Spec Delta

## Purpose

Permitir recibir archivos de usuarios sin exponer el servidor, los datos personales ni el almacenamiento privado a contenido malicioso.

## ADDED Requirements

### Requirement: Recepción autenticada de archivos

El sistema DEBE exigir autenticación válida y verificar el propietario y el propósito permitido antes de registrar una carga documental.

#### Scenario: Carga de documento propio
- **WHEN** un usuario autenticado carga un documento permitido de su cuenta
- **THEN** el sistema inicia su procesamiento en cuarentena

#### Scenario: Intento de carga para otro usuario
- **WHEN** un usuario intenta asociar un documento a otra cuenta sin autorización
- **THEN** el sistema rechaza la operación

### Requirement: Validación estricta del archivo

El sistema DEBE verificar tamaño, firma, estructura, límites de procesamiento y tipo real del archivo, sin confiar en la extensión ni en el Content-Type enviado.

#### Scenario: Imagen válida
- **WHEN** se recibe una imagen JPEG o PNG válida dentro de los límites
- **THEN** el sistema permite continuar al análisis de seguridad

#### Scenario: Archivo ejecutable disfrazado
- **WHEN** se recibe contenido ejecutable con extensión .jpg o .pdf
- **THEN** el sistema rechaza la carga

#### Scenario: Archivo excesivo o comprimido malicioso
- **WHEN** el archivo supera los límites de bytes, dimensiones, páginas o recursos de procesamiento
- **THEN** el sistema lo rechaza de forma controlada

### Requirement: Cuarentena privada

El sistema DEBE almacenar los archivos nuevos fuera de rutas públicas y mantenerlos bloqueados hasta completar el procesamiento.

#### Scenario: Archivo recién recibido
- **WHEN** finaliza la recepción de un documento
- **THEN** permanece inaccesible mediante URLs públicas y su estado de seguridad queda pendiente

#### Scenario: Fallo de almacenamiento
- **WHEN** falla el guardado del archivo
- **THEN** el sistema no registra el documento como disponible ni aprobado

### Requirement: Adaptación a Azure

El almacenamiento DEBE abstraerse mediante contratos que permitan implementar Azure Blob Storage privado sin cambiar las reglas de negocio.

#### Scenario: Sustitución del proveedor
- **WHEN** se configura un almacenamiento privado compatible con el contrato
- **THEN** los servicios documentales funcionan sin cambiar su lógica de autorización

### Requirement: Denegación ante fallos

El sistema DEBE impedir el uso de archivos cuyo procesamiento de seguridad esté incompleto o haya fallado.

#### Scenario: Analizador no disponible
- **WHEN** falla un analizador requerido
- **THEN** el archivo permanece bloqueado y no pasa a aprobación administrativa
