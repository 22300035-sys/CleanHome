# Spec Delta

## Purpose

Impedir operaciones sensibles de contratación y oferta de servicios cuando falten verificaciones obligatorias de identidad o domicilio.

## ADDED Requirements

### Requirement: Elegibilidad del cliente

El sistema DEBE exigir cuenta activa, correo verificado, identificación oficial y fotografía personal aprobadas para crear solicitudes de limpieza.

#### Scenario: Cliente completamente verificado
- **WHEN** el cliente cumple todos los requisitos de identidad
- **THEN** puede continuar al proceso de solicitud sujeto a las demás reglas

#### Scenario: Identidad pendiente
- **WHEN** el cliente no tiene todos sus documentos aprobados
- **THEN** no puede crear ni repetir una solicitud de limpieza

### Requirement: Elegibilidad por ubicación

El sistema DEBE exigir una ubicación activa, propia y aprobada para generar nuevas solicitudes de servicio.

#### Scenario: Ubicación válida
- **WHEN** un cliente verificado selecciona su ubicación aprobada
- **THEN** puede crear una solicitud vinculada a ella

#### Scenario: Ubicación de otra cuenta
- **WHEN** un cliente intenta usar una ubicación ajena
- **THEN** el sistema rechaza la operación

### Requirement: Elegibilidad del trabajador

El sistema DEBE exigir cuenta activa, correo verificado, identidad documental aprobada y perfil profesional autorizado antes de habilitar cotizaciones o publicación.

#### Scenario: Trabajador aprobado
- **WHEN** el trabajador cumple las verificaciones y tiene servicios activos
- **THEN** puede publicar su perfil y cotizar según las demás reglas

#### Scenario: Identidad incompleta
- **WHEN** el trabajador carece de fotografía o identificación aprobada
- **THEN** no puede publicar ni enviar nuevas cotizaciones

### Requirement: Revalidación de permisos

El sistema DEBE comprobar la elegibilidad vigente en las operaciones sensibles, sin depender de datos almacenados únicamente en el frontend o JWT.

#### Scenario: Aprobación revocada
- **WHEN** se revoca la aprobación del trabajador
- **THEN** no puede realizar nuevas operaciones restringidas

### Requirement: Conservación de cancelaciones

El sistema DEBE permitir la cancelación de operaciones existentes conforme a las reglas de negocio, incluso cuando una verificación documental haya dejado de estar vigente.

#### Scenario: Cliente sin verificación vigente cancela
- **WHEN** un cliente pierde temporalmente su elegibilidad
- **THEN** puede solicitar la cancelación de una reserva que le pertenece

### Requirement: Transición segura de datos anteriores

El sistema DEBE introducir las nuevas restricciones sin borrar solicitudes ni reservas creadas antes de la verificación documental.

#### Scenario: Solicitud histórica
- **WHEN** se consulta una solicitud anterior al nuevo módulo
- **THEN** sus datos se conservan y el acceso sigue sujeto a autorización
