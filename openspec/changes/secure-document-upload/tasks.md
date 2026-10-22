# Tasks

## 1. Inventario y decisiones de arquitectura

- [ ] 1.1 Revisar las entidades, servicios, controladores y migraciones existentes de documentos, perfiles y reservas. Verificar mediante un inventario de archivos y relaciones, sin introducir entidades duplicadas.
- [ ] 1.2 Revisar las especificaciones del cambio y comprobar su correspondencia con el diseño. Verificar con `openspec validate secure-document-upload --strict`.
- [ ] 1.3 Definir los tipos de identificación aceptados, caras obligatorias y criterios de revisión manual. Verificar mediante una decisión documentada en design.md.
- [ ] 1.4 Definir reglas de asignación de incidencias y acceso justificado del personal de soporte. Verificar mediante matriz de permisos y escenarios de autorización.
- [ ] 1.5 Definir plazos de conservación, sustitución y eliminación de documentos, incluyendo requisitos de privacidad aplicables. Verificar mediante política documentada y revisada antes de activar cargas reales.
- [ ] 1.6 Evaluar modelos gratuitos de moderación por licencia, precisión, memoria y CPU. Seleccionar uno únicamente tras una prueba técnica reproducible y documentada.
- [ ] 1.7 Evaluar ClamAV, herramientas de validación de imágenes y procesadores PDF de licencia compatible. Verificar mediante matriz de compatibilidad, versiones y dependencias.
- [ ] 1.8 Documentar arquitectura de almacenamiento local de desarrollo y Azure Blob privado, incluyendo posibles cargos. Verificar mediante revisión de componentes sin crear recursos facturables.

## 2. Persistencia y estados documentales

- [ ] 2.1 Revisar CustomerLocation, PrivateDocument y DocumentAccessAudit contra las especificaciones. Verificar mediante compilación y pruebas unitarias de sus invariantes.
- [ ] 2.2 Añadir únicamente las propiedades y relaciones faltantes para documentos vigentes, sustituidos y asociados a ubicaciones. Verificar con pruebas de EF Core y revisión del esquema.
- [ ] 2.3 Implementar una relación comprobable entre propietario, documento y ubicación, evitando referencias a domicilios ajenos. Verificar con pruebas de persistencia que rechacen asociaciones inválidas.
- [ ] 2.4 Crear la persistencia de trabajos documentales con estado, intentos y control de concurrencia. Verificar mediante pruebas de reintento e idempotencia.
- [ ] 2.5 Generar una migración incremental no destructiva para los cambios necesarios. Verificar revisando Up y Down antes de aplicarla.
- [ ] 2.6 Aplicar las migraciones exclusivamente a la base de desarrollo y pruebas autorizadas. Verificar con `dotnet ef migrations list` y pruebas de consultas EF Core.
- [ ] 2.7 Crear pruebas de transición entre estados de seguridad, moderación y revisión. Verificar que no sea posible aprobar documentos incompletos o bloqueados.

## 3. Cuarentena y carga segura

- [ ] 3.1 Revisar y completar IPrivateQuarantineStorage y LocalPrivateQuarantineStorage sin exponer rutas públicas. Verificar con pruebas de escritura, tamaño y limpieza tras errores.
- [ ] 3.2 Validar tamaño máximo, formatos permitidos y firmas de archivos JPEG, PNG y PDF. Verificar con archivos válidos y ejecutables disfrazados.
- [ ] 3.3 Validar decodificación de imágenes, dimensiones y límites de píxeles. Verificar con imágenes corruptas y casos de consumo excesivo.
- [ ] 3.4 Implementar procesamiento seguro de PDF con límites de páginas, memoria y tiempo, sin ejecutar contenido activo. Verificar con PDF válidos, corruptos y maliciosos de prueba.
- [ ] 3.5 Implementar recepción en streaming, hash SHA-256 y claves aleatorias de almacenamiento. Verificar que nombres y rutas suministrados por usuarios nunca determinen la ubicación física.
- [ ] 3.6 Implementar coordinación entre archivo guardado y registro PostgreSQL, incluyendo limpieza de archivos huérfanos. Verificar con fallos simulados de almacenamiento y transacción.
- [ ] 3.7 Implementar API autenticada para cargar documentos propios, sin permitir aprobar estados desde el cliente. Verificar con pruebas HTTP de 401, 403, 400, 413 y carga válida pendiente.
- [ ] 3.8 Documentar contratos HTTP, límites de carga y estados de procesamiento. Verificar contra OpenAPI y las respuestas reales.
- [ ] 3.9 Verificar que ningún documento pendiente pueda servirse desde rutas públicas. Comprobar mediante pruebas HTTP y revisión de configuración.

## 4. Antivirus y procesamiento de amenazas

- [ ] 4.1 Definir contrato de análisis antimalware independiente de la implementación concreta. Verificar con pruebas usando un analizador simulado.
- [ ] 4.2 Integrar el motor gratuito seleccionado con aislamiento y límites de recursos. Verificar con archivo de prueba antivirus inofensivo y archivos limpios.
- [ ] 4.3 Implementar resultados clean, blocked y pending sin aprobar en caso de error. Verificar mediante pruebas de timeout y analizador no disponible.
- [ ] 4.4 Implementar reintentos controlados e idempotentes para análisis incompletos. Verificar con ejecución repetida del mismo trabajo.
- [ ] 4.5 Registrar eventos técnicos sin incluir datos personales ni contenido del archivo. Verificar inspeccionando logs de pruebas.
- [ ] 4.6 Documentar requisitos de ejecución, memoria y actualización de firmas. Verificar con instrucciones reproducibles y medición local.

## 5. Moderación de imágenes y PDF

- [ ] 5.1 Crear contrato de clasificación de contenido visual. Verificar con pruebas de resultados permitido, prohibido e incierto.
- [ ] 5.2 Integrar el modelo gratuito seleccionado según la decisión del grupo 1. Verificar licencia, versión y ejecución reproducible.
- [ ] 5.3 Integrar moderación de fotografías JPEG y PNG. Verificar con conjunto de prueba autorizado y sin datos personales reales.
- [ ] 5.4 Integrar moderación de páginas PDF mediante procesamiento aislado. Verificar con documentos de prueba de varias páginas.
- [ ] 5.5 Implementar manejo de resultados inciertos y fallos del modelo sin aprobación automática. Verificar con pruebas de respuestas ambiguas y timeouts.
- [ ] 5.6 Impedir la aprobación administrativa si seguridad o moderación no han terminado favorablemente. Verificar mediante pruebas de servicio.
- [ ] 5.7 Documentar umbrales, limitaciones y procedimiento de revisión humana. Verificar correspondencia con los escenarios OpenSpec.

## 6. Verificación de identidad de clientes y trabajadores

- [ ] 6.1 Implementar consulta de documentos propios y estado de verificación de identidad. Verificar que no se expongan StorageKey ni datos privados innecesarios.
- [ ] 6.2 Implementar requisitos de identificación oficial y fotografía personal para ambos tipos de perfil. Verificar escenarios completos e incompletos.
- [ ] 6.3 Implementar reemplazo de documentos rechazados con nueva revisión desde pending. Verificar que los archivos antiguos no conserven validez para nuevas operaciones.
- [ ] 6.4 Integrar revisión administrativa de los nuevos PrivateDocument sin eliminar prematuramente WorkerDocument. Verificar que ambos flujos no permitan aprobaciones contradictorias.
- [ ] 6.5 Implementar aprobación y rechazo administrativos con permisos específicos y trazabilidad. Verificar pruebas de autorización y auditoría.
- [ ] 6.6 Proteger la fotografía documental para que no aparezca automáticamente como avatar público. Verificar respuestas del catálogo y endpoints de perfil.
- [ ] 6.7 Documentar el flujo de identidad para frontend y administración. Verificar que cubra los estados pendiente, aprobado y rechazado.

## 7. Ubicaciones y comprobantes de domicilio

- [ ] 7.1 Implementar API de registro y consulta de ubicaciones propias. Verificar autenticación, validación de dirección y aislamiento entre usuarios.
- [ ] 7.2 Vincular comprobantes a ubicaciones del mismo propietario. Verificar rechazo de identificadores ajenos.
- [ ] 7.3 Implementar aprobación administrativa de domicilios condicionada a comprobante aprobado. Verificar con pruebas de ubicación sin documento o documento rechazado.
- [ ] 7.4 Invalidar la aprobación ante modificaciones sustanciales de dirección. Verificar con pruebas de actualización.
- [ ] 7.5 Impedir la exposición de comprobantes y direcciones particulares en consultas públicas. Verificar con pruebas HTTP de catálogo.
- [ ] 7.6 Documentar contratos de ubicaciones y estados para React. Verificar OpenAPI y ejemplos de respuesta sin datos privados ajenos.

## 8. Acceso restringido y auditoría

- [ ] 8.1 Implementar autorización para que cada usuario consulte únicamente sus propios documentos. Verificar mediante pruebas de acceso cruzado.
- [ ] 8.2 Implementar permisos administrativos granulares con membresía activa. Verificar rechazo de roles suspendidos o sin permiso.
- [ ] 8.3 Implementar acceso de soporte sujeto a incidencia relacionada, asignación y motivo justificado. Verificar casos autorizados y no autorizados.
- [ ] 8.4 Implementar lectura privada sin devolver StorageKey ni URLs públicas permanentes. Verificar cabeceras seguras y respuestas HTTP.
- [ ] 8.5 Registrar accesos autorizados y decisiones documentales en DocumentAccessAudit. Verificar actor, acción, fecha e incidencia cuando corresponda.
- [ ] 8.6 Proteger auditorías frente a modificaciones por usuarios ordinarios. Verificar autorización de endpoints administrativos.
- [ ] 8.7 Documentar matriz de permisos y reglas de consulta privada. Verificar su correspondencia con pruebas de autorización.

## 9. Elegibilidad y contratación

- [ ] 9.1 Implementar consulta centralizada de identidad documental aprobada para clientes y trabajadores. Verificar resultados con documentos pendientes, rechazados y aprobados.
- [ ] 9.2 Exigir identidad verificada para crear y repetir solicitudes. Verificar rechazo en servicios y endpoints HTTP.
- [ ] 9.3 Exigir identidad y perfil profesional aprobados para publicar y cotizar. Verificar que otras rutas no puedan omitir la regla.
- [ ] 9.4 Vincular nuevas solicitudes a ubicaciones propias activas y aprobadas. Verificar imposibilidad de contratar en ubicaciones ajenas o pendientes.
- [ ] 9.5 Preservar ServiceRequestAddress como dirección histórica de la contratación. Verificar que cambios posteriores de ubicación no alteren reservas anteriores.
- [ ] 9.6 Revalidar elegibilidad al aceptar cotizaciones y operaciones sensibles. Verificar que revocaciones impidan nuevos compromisos.
- [ ] 9.7 Mantener cancelaciones permitidas sin exigir una nueva verificación documental. Verificar con pruebas de reservas existentes.
- [ ] 9.8 Documentar cambios de contratos y estrategia de transición para clientes existentes. Verificar compatibilidad con datos históricos.

## 10. Integración y cierre del cambio

- [ ] 10.1 Ejecutar `dotnet build backend/CleanHome.slnx --configuration Release --warnaserror` y confirmar compilación sin errores.
- [ ] 10.2 Ejecutar pruebas unitarias e integración contra la base de pruebas aislada; verificar que no se utilice cleanhome_dev ni producción por accidente.
- [ ] 10.3 Ejecutar una prueba completa con documentos ficticios: carga, cuarentena, antivirus, moderación, revisión, ubicación y contratación; comprobar cada estado.
- [ ] 10.4 Ejecutar escenarios de abuso: archivos falsificados, tamaños excesivos, documentos ajenos, soporte sin incidencia y analizadores fuera de servicio; comprobar rechazo seguro.
- [ ] 10.5 Evaluar consumo estimado de almacenamiento, CPU, RAM y operaciones Azure para el MVP. Documentar riesgos de cargos y no desplegar recursos sin autorización.
- [ ] 10.6 Ejecutar `openspec validate secure-document-upload --strict` y revisar que todos los requisitos tengan evidencia de implementación.
- [ ] 10.7 Revisar `git diff --check`, confirmar que no haya secretos ni documentos reales en Git y preparar el cambio para revisión.

## Workflow follow-up

- Solicitar revisión del cambio antes de fusionarlo a la rama principal.
- Archivar el cambio en OpenSpec cuando sus requisitos y pruebas estén completos.
- Revisar la suscripción real de Azure antes de crear recursos o desplegar.
