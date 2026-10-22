# Spec Delta

## Purpose

Restringir la lectura y revisión de documentos privados al propietario y al personal autorizado, manteniendo trazabilidad de los accesos.

## ADDED Requirements

### Requirement: Acceso del propietario

El sistema DEBE permitir a un usuario consultar exclusivamente sus documentos propios mediante endpoints autenticados y autorizados.

#### Scenario: Consulta propia
- **WHEN** un usuario solicita un documento del que es propietario
- **THEN** el sistema comprueba sus permisos antes de entregar el contenido permitido

#### Scenario: Consulta ajena
- **WHEN** un usuario solicita el identificador de un documento de otra cuenta
- **THEN** no recibe el documento ni su contenido privado

### Requirement: Acceso administrativo

El sistema DEBE exigir una membresía administrativa activa y los permisos específicos para consultar documentos de verificación.

#### Scenario: Administrador autorizado
- **WHEN** un administrador habilitado consulta un documento para revisión
- **THEN** el sistema autoriza el acceso conforme a su función y lo audita

#### Scenario: Administrador suspendido
- **WHEN** una cuenta administrativa suspendida solicita un documento
- **THEN** el sistema deniega el acceso

### Requirement: Acceso de soporte por incidencia

El personal de soporte DEBE contar con una incidencia válida, relacionada con el documento o su propietario, y autorización específica.

#### Scenario: Incidencia justificada
- **WHEN** un agente autorizado consulta un documento relacionado con una incidencia permitida
- **THEN** el sistema registra la incidencia y el acceso

#### Scenario: Acceso sin incidencia
- **WHEN** soporte intenta consultar un documento sin incidencia autorizada
- **THEN** la operación se rechaza

### Requirement: Auditoría de acceso

El sistema DEBE registrar los intentos permitidos de lectura, descarga y revisión de documentos con actor, acción y fecha.

#### Scenario: Descarga autorizada
- **WHEN** una persona autorizada descarga un documento
- **THEN** queda registrado el acceso y su justificación cuando corresponda

### Requirement: Entrega privada y segura

El sistema DEBE impedir enlaces públicos, cachés compartidas y exposición de claves internas de almacenamiento.

#### Scenario: Solicitud de archivo privado
- **WHEN** se entrega un documento autorizado
- **THEN** la respuesta utiliza medidas para evitar almacenamiento público y exposición de rutas internas

### Requirement: Privacidad ante cambios de permisos

El sistema DEBE comprobar los permisos actuales antes de cada acceso y no confiar en una autorización anterior.

#### Scenario: Permiso revocado
- **WHEN** un empleado pierde autorización
- **THEN** sus nuevas solicitudes de documentos son rechazadas
