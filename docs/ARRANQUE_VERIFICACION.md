# Verificación de arranque y Swagger — 4 de noviembre de 2026

- SwaggerUI 10.3.1 incorporado como interfaz del OpenAPI existente; sin segundo generador ni recursos CDN.
- Build Release sin errores ni advertencias; 20 pruebas unitarias y cinco de integración aprobadas (25, cero omitidas).
- Cuatro casos nuevos verifican Development, Testing y Production con/sin habilitación de OpenAPI, recursos locales y autorización no persistida.
- Navegador Brave headless renderizó 56 operaciones y el diálogo Bearer sin errores JavaScript. Captura: screenshots/swagger-api.png.
- Se verificaron HTTP 200 de readiness, Swagger HTML/bundle y catálogo a través del proxy frontend.
- Start-CleanHome se ejecutó repetidamente sin duplicar servicios; Stop/Start completos funcionaron desde Windows PowerShell, cerrando PostgreSQL con checkpoint y conservando datos.
- Configuración de este equipo en .local-services/settings.json, ignorada por Git y excluida del empaquetado; conexión cifrada con DPAPI. No se imprimió el valor descifrado.
- Scripts de arranque, parada y autostart pasaron el parser de PowerShell. La opción de inicio automático se entrega para activación voluntaria: no se habilitó una entrada de inicio en esta preparación.
- No se reinició físicamente Windows. Se ensayó el ciclo completo de parada y arranque con los mismos scripts; no se crearon ni aplicaron migraciones en los scripts. Las pruebas de integración revisan las migraciones de la base ficticia existente.

Esta actualización corresponde a cleanhome-audit/CleanHome-main; CleanHome A no fue modificada. Las pruebas de aplicación previas se describen en ENTREGA_FRONTEND.md; este informe documenta la validación adicional de Swagger y operación local.
