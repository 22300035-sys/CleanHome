# Spec Delta

## Purpose

Comprobar documentalmente la identidad declarada de clientes y trabajadores antes de habilitar actividades de contratación u oferta de servicios.

## ADDED Requirements

### Requirement: Documentación obligatoria para ambos perfiles

El sistema DEBE exigir correo verificado, identificación oficial válida y fotografía personal aprobada tanto a clientes como a trabajadores.

#### Scenario: Cliente con documentos completos
- **WHEN** el cliente tiene correo verificado e identificación y fotografía aprobadas
- **THEN** cumple el requisito de identidad para contratar

#### Scenario: Trabajador sin identificación
- **WHEN** un trabajador únicamente presenta una fotografía personal
- **THEN** su verificación de identidad permanece incompleta

### Requirement: Identificación oficial

El sistema DEBE exigir las caras necesarias de la identificación oficial según el tipo de documento aceptado.

#### Scenario: Identificación incompleta
- **WHEN** falta una cara obligatoria de la identificación
- **THEN** no se aprueba la identidad

### Requirement: Revisión administrativa

El sistema DEBE permitir que personal administrativo autorizado apruebe o rechace documentos después de las comprobaciones técnicas y de contenido.

#### Scenario: Documento seguro y revisado
- **WHEN** seguridad y moderación están aprobadas y un revisor autorizado valida el documento
- **THEN** el documento puede quedar aprobado

#### Scenario: Intento de aprobación prematura
- **WHEN** la seguridad o la moderación permanecen pendientes
- **THEN** se rechaza la aprobación administrativa

### Requirement: Reemplazo de documentos

El sistema DEBE permitir sustituir documentos rechazados y volver a ejecutar las verificaciones obligatorias.

#### Scenario: Sustitución de identificación rechazada
- **WHEN** el usuario reemplaza un documento rechazado
- **THEN** el nuevo documento vuelve a estado pendiente sin heredar aprobaciones anteriores

### Requirement: Separación entre identidad y visibilidad

El sistema DEBE mantener las fotografías documentales privadas, sin publicarlas automáticamente como fotografías de perfil.

#### Scenario: Identidad aprobada
- **WHEN** un usuario obtiene aprobación documental
- **THEN** su identificación y fotografía de verificación permanecen privadas
