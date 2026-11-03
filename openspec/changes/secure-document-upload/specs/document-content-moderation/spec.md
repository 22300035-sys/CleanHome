# Spec Delta

## Purpose

Examinar imágenes y documentos antes de utilizarlos, detectando amenazas informáticas y contenido visual inapropiado mediante herramientas de licencia gratuita.

## ADDED Requirements

### Requirement: Análisis antimalware

El sistema SHALL someter los archivos a un analizador antimalware antes de declararlos seguros.

#### Scenario: Archivo sin detecciones
- **WHEN** el analizador completa correctamente la inspección sin detectar amenazas
- **THEN** el sistema registra el resultado de seguridad correspondiente

#### Scenario: Amenaza detectada
- **WHEN** el analizador identifica contenido peligroso
- **THEN** el documento queda bloqueado y no puede aprobarse

#### Scenario: Antivirus fuera de servicio
- **WHEN** el analizador no está disponible
- **THEN** el archivo no se considera limpio y permanece en cuarentena

### Requirement: Moderación de imágenes

El sistema SHALL analizar las imágenes para identificar contenido sexual explícito o inapropiado, utilizando un modelo de licencia compatible con el proyecto.

#### Scenario: Contenido permitido
- **WHEN** el análisis concluye que una imagen cumple la política de contenido
- **THEN** el documento puede continuar a revisión documental

#### Scenario: Contenido claramente prohibido
- **WHEN** el análisis detecta contenido prohibido con suficiente confianza
- **THEN** el documento se bloquea para uso normal y se registra la decisión

#### Scenario: Clasificación incierta
- **WHEN** el análisis produce un resultado ambiguo
- **THEN** el documento permanece pendiente de revisión humana especializada

### Requirement: Inspección de PDF

El sistema SHALL inspeccionar la estructura de los PDF y moderar visualmente su contenido representable sin ejecutar código incorporado.

#### Scenario: PDF con contenido visual
- **WHEN** se recibe un PDF permitido
- **THEN** sus páginas se procesan de forma aislada y se someten a moderación

#### Scenario: PDF con características peligrosas
- **WHEN** el PDF contiene elementos no admitidos o excede los límites de procesamiento
- **THEN** el sistema bloquea el archivo

### Requirement: Separación de resultados

El sistema SHALL registrar los resultados de seguridad y moderación independientemente de la revisión documental.

#### Scenario: Documento seguro pero inapropiado
- **WHEN** un documento supera el antivirus pero falla la moderación
- **THEN** no puede recibir aprobación documental

### Requirement: Privacidad del procesamiento

El sistema SHALL evitar enviar documentos de identidad a proveedores externos sin una autorización y evaluación de privacidad explícitas.

#### Scenario: Servicio externo no autorizado
- **WHEN** una integración intenta enviar archivos sensibles a un proveedor no aprobado
- **THEN** la operación se impide
