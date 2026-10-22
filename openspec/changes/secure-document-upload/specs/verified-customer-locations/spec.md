# Spec Delta

## Purpose

Permitir registrar domicilios de clientes y validar el comprobante correspondiente antes de utilizarlos para solicitar servicios.

## ADDED Requirements

### Requirement: Registro de ubicaciones propias

El sistema DEBE permitir a un cliente registrar y administrar únicamente sus propias ubicaciones.

#### Scenario: Registro correcto
- **WHEN** un cliente registra una dirección válida
- **THEN** la ubicación queda pendiente de comprobación

#### Scenario: Acceso a ubicación ajena
- **WHEN** un usuario intenta modificar una ubicación de otro cliente
- **THEN** el sistema deniega la operación

### Requirement: Comprobante obligatorio por ubicación

Cada ubicación DEBE contar con un comprobante de domicilio revisado y aprobado antes de ser utilizada para contratar.

#### Scenario: Comprobante aprobado
- **WHEN** el comprobante coincide con la ubicación y supera todas las verificaciones
- **THEN** un revisor autorizado puede aprobar la ubicación

#### Scenario: Ubicación sin comprobante
- **WHEN** un cliente intenta contratar en una ubicación sin comprobante aprobado
- **THEN** el sistema rechaza la contratación

### Requirement: Modificación de domicilio

El sistema DEBE invalidar la aprobación de una ubicación cuando cambien sus datos sustanciales de domicilio.

#### Scenario: Cambio de dirección
- **WHEN** el cliente modifica la calle o los datos del domicilio
- **THEN** la ubicación vuelve a estado pendiente y requiere nueva comprobación

### Requirement: Protección de datos de ubicación

El sistema DEBE impedir que las direcciones particulares y sus comprobantes sean accesibles mediante catálogos públicos.

#### Scenario: Consulta pública
- **WHEN** un visitante explora servicios o perfiles
- **THEN** no recibe domicilios privados ni comprobantes de clientes

### Requirement: Compatibilidad con reservas existentes

El sistema DEBE conservar la dirección histórica de reservas existentes al introducir referencias a ubicaciones verificadas.

#### Scenario: Reserva anterior a la nueva función
- **WHEN** se consulta una reserva creada antes de la migración
- **THEN** la dirección histórica sigue disponible únicamente para usuarios autorizados
