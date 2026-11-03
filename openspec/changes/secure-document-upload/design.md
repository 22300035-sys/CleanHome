# Diseño técnico: Secure Document Upload

## Context

CleanHome utiliza ASP.NET Core 10 con arquitectura limpia,
PostgreSQL y Entity Framework Core.

La solución tiene módulos de autenticación, perfiles profesionales,
solicitudes, reservas, administración e incidencias.

Actualmente existen las siguientes entidades:

- User y UserProfile.
- WorkerProfile y WorkerDocument.
- ServiceRequest y ServiceRequestAddress.
- CustomerLocation.
- PrivateDocument.
- DocumentAccessAudit.
- SupportIncident y AdminMembership.

También existe el contrato IPrivateQuarantineStorage y una implementación
local de desarrollo.

Estos componentes no constituyen todavía un sistema de carga seguro
ni una verificación documental completa.

El despliegue objetivo es Microsoft Azure. Se priorizan herramientas
gratuitas, minimización de recursos y gasto personal de 0 MXN.

## Goals / Non-Goals

### Goals

- Unificar la verificación de identidad de clientes y trabajadores.
- Exigir identificación oficial y fotografía personal.
- Verificar comprobantes por ubicación del cliente.
- Procesar archivos sin confiar en su extensión ni nombre.
- Mantener todos los archivos en cuarentena hasta superar controles.
- Separar seguridad técnica, moderación y revisión documental.
- Evitar la exposición pública de documentos privados.
- Autorizar accesos usando información actual de PostgreSQL.
- Auditar accesos y decisiones administrativas.
- Integrar elegibilidad documental con las reglas de contratación.
- Mantener un almacenamiento intercambiable compatible con Azure.
- Evitar dependencias de licencias comerciales y servicios de pago.

### Non-Goals

- Reconocimiento facial biométrico.
- Verificación automática de autenticidad de INE.
- Garantizar detección absoluta de malware o contenido inapropiado.
- Desplegar recursos Azure durante este cambio.
- Implementar pagos reales.
- Reemplazar todas las funcionalidades existentes del marketplace.
- Habilitar archivos públicos sin controles de seguridad.

## Decisions

### 1. Arquitectura de componentes

El desarrollo seguirá Clean Architecture.

Domain:
- Entidades de documentos y ubicaciones.
- Estados y transiciones de verificación.
- Reglas invariantes de documentos y domicilios.

Application:
- Contratos de almacenamiento y análisis.
- DTO de solicitudes y respuestas.
- Casos de uso de carga, consulta y revisión.
- Reglas de elegibilidad de clientes y trabajadores.

Infrastructure:
- Persistencia mediante EF Core.
- Implementaciones de almacenamiento.
- Validadores de archivos.
- Integración con herramientas gratuitas de escaneo.
- Implementación de moderación local.
- Auditoría.

Api:
- Endpoints autenticados de documentos.
- Endpoints de ubicaciones.
- Endpoints administrativos y de soporte.
- Control de tamaño de solicitudes y políticas de autorización.

Los controladores no accederán directamente a rutas físicas ni
decidirán por sí solos las reglas de autorización documental.

### 2. Modelo documental unificado

PrivateDocument será el registro principal para nuevos documentos.

DocumentType:
- identity_front
- identity_back
- portrait
- address_proof

Cada documento estará asociado a un propietario.

Los comprobantes de domicilio también estarán vinculados a una
CustomerLocation del mismo propietario.

WorkerDocument seguirá existiendo durante la transición para evitar
borrar datos previos o romper operaciones administrativas.

Se migrarán los flujos administrativos hacia PrivateDocument de forma
gradual. No se eliminará WorkerDocument hasta validar la migración
de datos y la compatibilidad de los endpoints existentes.

El diseño deberá impedir que un usuario vincule documentos propios
a ubicaciones ajenas.

### 3. Estados de procesamiento

Se conservarán tres estados independientes:

SecurityStatus:
- pending
- clean
- blocked

ModerationStatus:
- pending
- approved
- rejected

ReviewStatus:
- pending
- approved
- rejected

Un documento es apto únicamente cuando:

SecurityStatus == clean
AND ModerationStatus == approved
AND ReviewStatus == approved

Las transiciones estarán controladas por casos de uso autorizados,
no por valores enviados directamente por el cliente.

Una nueva versión de archivo comenzará siempre pendiente.

Los fallos o interrupciones de procesamiento mantendrán el archivo
bloqueado hasta su reintento o revisión correspondiente.

### 4. Recepción y cuarentena

Se utilizará IPrivateQuarantineStorage como contrato inicial.

El backend:

1. Autentica al usuario.
2. Comprueba que puede cargar ese tipo de documento.
3. Aplica límites al tamaño de la petición.
4. Recibe el contenido en streaming.
5. Genera una clave aleatoria de almacenamiento.
6. Calcula SHA-256 del contenido.
7. Guarda el archivo en cuarentena privada.
8. Registra el documento en estado pendiente.
9. Inicia el procesamiento de seguridad.

No se utilizarán nombres originales como rutas físicas.

Se impedirán traversal de rutas, extensiones inesperadas,
archivos vacíos y tamaños excesivos.

Límites iniciales:
- Fotografías JPEG y PNG: máximo 5 MiB.
- Documentos PDF: máximo 10 MiB.

Los límites de dimensiones, páginas, memoria y tiempo serán
configurables y se aplicarán en los analizadores.

No se habilitará la subida pública hasta completar los controles.

### 5. Validación técnica

Se comprobarán:

- Firma real del archivo.
- Concordancia del contenido con el formato permitido.
- Estructura válida de JPEG, PNG o PDF.
- Dimensiones y cantidad de píxeles.
- Cantidad de páginas en PDF.
- Tamaño máximo.
- Posibles archivos poliglota y contenido activo no permitido.
- Recursos máximos empleados para procesarlo.

Las imágenes se decodificarán y recodificarán a un formato limpio
cuando corresponda, eliminando metadatos innecesarios.

No se aceptarán SVG, HTML, scripts, ejecutables, archivos comprimidos
ni formatos adicionales sin una nueva especificación.

Los PDF se procesarán en un entorno restringido, sin ejecutar
JavaScript, archivos adjuntos ni contenido activo.

### 6. Antivirus

Se evaluará ClamAV como motor antimalware gratuito.

El análisis será realizado por un componente aislado del flujo
de atención HTTP cuando sea posible.

El sistema no marcará como limpio un archivo cuando:
- El motor no responda.
- Exista un timeout.
- La firma esté desactualizada fuera de la política permitida.
- El análisis devuelva un resultado incompleto.

La infraestructura final de ejecución en Azure deberá ser evaluada
antes de desplegarse, debido al consumo de RAM y CPU.

No se asumirá que ClamAV puede ejecutarse gratis de forma permanente
en cualquier plan de Azure.

### 7. Moderación visual

Se utilizará un modelo abierto compatible con el uso del proyecto.

La moderación clasificará imágenes y páginas renderizadas de PDF.

El modelo:
- No verificará por sí mismo la identidad de una persona.
- No aprobará documentos administrativos automáticamente.
- No sustituirá la revisión humana.
- No enviará archivos de identidad a proveedores externos.

Los resultados inciertos quedarán pendientes.

Se realizarán pruebas con muestras permitidas e inapropiadas
sin utilizar documentos personales reales.

El modelo y su licencia se seleccionarán antes de la implementación
definitiva. Su consumo de recursos deberá ajustarse al presupuesto.

### 8. Procesamiento asíncrono y consistencia

La carga y el análisis serán operaciones separadas.

La API registrará inicialmente un documento pendiente.

Se implementará procesamiento recuperable mediante un mecanismo
persistente de trabajos pendientes, evitando depender exclusivamente
de tareas en memoria.

Los procesadores usarán estados e identificadores idempotentes
para evitar aprobar o procesar incorrectamente el mismo documento
durante reintentos.

Un fallo en antivirus o moderación no autorizará el documento.

Los archivos huérfanos se limpiarán mediante un procedimiento
controlado y auditable.

### 9. Almacenamiento local y Azure

Desarrollo:
- Almacenamiento privado local.
- Fuera de wwwroot.
- Permisos restrictivos.
- Sin depender de Docker.

Azure:
- Evaluar Azure Blob Storage con contenedores privados.
- Sin acceso anónimo.
- Sin URLs permanentes públicas.
- Sin guardar credenciales en código ni en Git.
- Usar identidad administrada cuando el plan lo permita.
- Considerar cifrado, operaciones, almacenamiento y transferencia.

No se utilizará el disco efímero de Azure Container Apps como
almacenamiento documental permanente.

La selección final dependerá de las cuotas disponibles en la
suscripción real y de una estimación económica previa.

### 10. Autorización documental

El propietario podrá consultar sus propios documentos según las
restricciones de seguridad del estado del archivo.

Los administradores necesitarán membresía activa y permiso
específico de revisión documental.

Soporte necesitará:
- Cuenta activa y autorización de soporte.
- Incidencia válida relacionada con la persona o documento.
- Motivo de acceso justificado.
- Registro de auditoría.

La existencia de un identificador de incidencia no será suficiente:
el backend comprobará su relación y autorización.

No se permitirá utilizar identificadores de documentos como
mecanismo de autenticación.

Se devolverán respuestas que no permitan inferir el contenido
de documentos ajenos.

Matriz de permisos documentales:

| Persona | Consultar documentos propios | Revisar documentos | Acceso de soporte |
| --- | --- | --- | --- |
| Propietario | Permitido | No | No |
| Administrador | Permitido | Permitido | No |
| Soporte | No | No | Permitido con incidencia válida relacionada |

El acceso del soporte se tramita exclusivamente con incidencia
justificada, motivo registrado y auditoría; no usa la vía de
revisión administrativa.

### 11. Auditoría y privacidad

DocumentAccessAudit almacenará:
- Documento.
- Actor.
- Tipo de acceso.
- Rol autorizado.
- Incidencia cuando corresponda.
- Fecha.

También se registrarán las decisiones de revisión documental.

El acceso al contenido ocurrirá únicamente después de comprobar
la autorización y registrar la acción cuando proceda.

Los logs no incluirán archivos, tokens, documentos completos,
contraseñas ni claves privadas de almacenamiento.

Las respuestas utilizarán encabezados que impidan cachés compartidas
y exposición accidental de contenido.

Se establecerán políticas de retención, sustitución y eliminación
de documentos conforme a las obligaciones aplicables.

### 12. Verificación de identidad

Clientes y trabajadores requerirán:
- Cuenta activa.
- Correo verificado.
- Identificación oficial aprobada.
- Fotografía personal aprobada.

El sistema no permitirá que una misma aprobación documental cubra
un documento diferente o reemplazado.

La identidad verificada se determinará desde los documentos vigentes
y aprobados, sin confiar únicamente en información del frontend.

La fotografía documental no será pública automáticamente.

### 12.1 Identificación oficial aceptada y caras obligatorias

Decisiones tomadas para el primer incremento:

- Tipos de documento aceptados inicialmente:
  - `identity_front`: identificación oficial (frente).
  - `identity_back`: identificación oficial (reverso).
  - `portrait`: fotografía personal del titular.
  - `address_proof`: comprobante de domicilio.
- Caras obligatorias:
  - La identificación oficial exige presentar frente y reverso.
  - La fotografía personal exige el rostro del titular.
- Criterios de revisión manual:
  - El revisor autorizado contrastará la fotografía con el
    documento y con el titular declarado.
  - Los documentos incompletos o ambiguos permanecerán en
    revisión hasta que se resuelva la duda.

### 13. Ubicaciones verificadas

CustomerLocation almacenará ubicaciones propias del cliente.

Cada ubicación requerirá un comprobante aprobado vinculado
a su identificador.

La aprobación de una ubicación dependerá de:
- Titular autorizado.
- Dirección válida.
- Comprobante vigente y aprobado.
- Revisión administrativa.

Cualquier cambio sustancial de dirección invalidará la aprobación
y exigirá una nueva comprobación.

ServiceRequest deberá referenciar CustomerLocation para nuevas
solicitudes.

ServiceRequestAddress seguirá conservando una copia histórica
de la dirección para evitar que una modificación futura altere
los datos originales de una reserva.

### 14. Elegibilidad en el marketplace

La API deberá comprobar los requisitos documentales en PostgreSQL
antes de permitir:

Cliente:
- Crear solicitudes.
- Repetir solicitudes.
- Aceptar cotizaciones.

Trabajador:
- Publicar su perfil.
- Enviar cotizaciones.
- Realizar nuevas operaciones restringidas.

La verificación de correo seguirá siendo independiente de la
verificación documental.

Se permitirá cancelar operaciones existentes según las reglas
de negocio, aunque una verificación haya caducado o sido revocada.

Los cambios se aplicarán también en los servicios de aplicación
para evitar omitir controles mediante rutas HTTP diferentes.

### 15. Compatibilidad y migraciones

No se eliminarán tablas existentes durante este cambio.

Se introducirán nuevas columnas y relaciones mediante migraciones
incrementales de EF Core.

Para solicitudes anteriores, cualquier nueva referencia de ubicación
deberá ser inicialmente nullable o tener un plan explícito
de migración de datos.

Los datos históricos conservarán sus permisos y restricciones.

No se aplicarán migraciones de producción automáticamente desde
una petición HTTP.

### 16. Observabilidad y errores

Los errores HTTP seguirán el formato Problem Details.

Se diferenciarán:
- Archivo inválido.
- Archivo demasiado grande.
- Acceso denegado.
- Documento pendiente.
- Documento rechazado.
- Servicio de análisis temporalmente no disponible.

Los mensajes externos no incluirán rutas internas ni información
sensible de los documentos.

Los eventos operativos usarán identificadores de seguimiento.

## Risks / Trade-offs

### Consumo de recursos

ClamAV y los modelos de moderación pueden superar las cuotas
gratuitas de Azure.

Mitigación:
- Procesamiento bajo demanda.
- Límites de concurrencia.
- Medición de CPU, RAM y almacenamiento.
- Suspender procesamiento antes que generar gastos imprevistos.

### Falsos positivos y falsos negativos

Los modelos automáticos pueden clasificar incorrectamente contenido.

Mitigación:
- Revisión humana de casos inciertos.
- Pruebas de precisión.
- Denegación de aprobación automática en casos ambiguos.

### Manejo de datos sensibles

INE, fotografías y comprobantes contienen datos personales.

Mitigación:
- Minimización de datos.
- Almacenamiento privado.
- Controles de acceso.
- Auditoría.
- Retención limitada.
- No enviarlos a proveedores externos no autorizados.

### Compatibilidad

La introducción de ubicaciones verificadas afecta los contratos
de creación de solicitudes.

Mitigación:
- Nuevos campos compatibles durante la transición.
- Migraciones incrementales.
- Pruebas con solicitudes y reservas históricas.

### Dependencia de procesamiento externo

El antivirus o moderador podría fallar o no estar disponible.

Mitigación:
- Estados pendientes persistentes.
- Reintentos limitados.
- Denegación por defecto.
- Sin aprobaciones automáticas ante errores.

## Migration Plan

1. Revisar las entidades y configuraciones existentes.
2. Identificar las diferencias respecto a la especificación.
3. Implementar los contratos de validación, análisis y almacenamiento.
4. Completar la persistencia del procesamiento documental.
5. Generar migraciones incrementales no destructivas.
6. Implementar cuarentena y validación técnica.
7. Integrar antivirus y moderación con herramientas gratuitas.
8. Implementar autorización, revisión y auditoría.
9. Incorporar ubicaciones verificadas.
10. Aplicar elegibilidad documental al marketplace.
11. Mantener compatibilidad con datos históricos.
12. Ejecutar pruebas de seguridad e integración.
13. Verificar todos los criterios de aceptación.
14. Preparar el plan Azure con límites y costos evaluados.

## Open Questions

- ¿Qué modelo gratuito de moderación ofrece el mejor equilibrio
  entre precisión, licencia y consumo de recursos?
- ¿Qué componentes de antivirus y procesamiento pueden ejecutarse
  dentro de las cuotas reales de nuestra suscripción Azure?
- ¿Cuál será el plazo de conservación de identificaciones y
  comprobantes después de la eliminación de una cuenta?
- ¿Qué identificaciones oficiales adicionales a INE se aceptarán?
- ¿Cómo se gestionarán las apelaciones por rechazo documental?
- ¿Qué requisitos exactos tendrá la asignación de incidencias
  al personal de soporte?
