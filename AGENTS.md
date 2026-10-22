# CleanHome — Instrucciones para agentes de IA

## Proyecto
CleanHome es una plataforma de intermediación de servicios de limpieza.

## Tecnologías
- Backend: ASP.NET Core 10, C# y Clean Architecture.
- Base de datos: PostgreSQL y Entity Framework Core.
- Frontend: React, TypeScript, Vite y PWA.
- Aplicación Android: Capacitor.
- Infraestructura objetivo: Microsoft Azure.
- Objetivo económico: $0 MXN de gasto personal.
- No utilizar Docker ni servicios de pago sin autorización.

## Arquitectura del backend
- Domain: entidades y reglas de negocio.
- Application: interfaces, contratos y DTO.
- Infrastructure: persistencia y servicios externos.
- Api: controladores, autenticación y autorización.

## Seguridad
- JWT y sesiones revocables.
- Contraseñas mediante Argon2id.
- Identificación oficial y fotografía para clientes y trabajadores.
- Comprobante de domicilio por cada ubicación del cliente.
- Documentos privados y acceso restringido.
- Soporte solo con incidencia justificada.
- Archivos en cuarentena hasta pasar controles de seguridad.
- Antivirus, moderación y revisión documental.
- Nunca exponer secretos ni documentos privados.

## Desarrollo
1. Inspeccionar el código antes de modificarlo.
2. No duplicar clases ni funcionalidades.
3. Seguir OpenSpec y Spec-Driven Development.
4. No ejecutar migraciones destructivas.
5. No realizar push, merges o despliegues sin autorización.
6. No instalar herramientas comerciales.
7. No modificar código fuera de las tareas autorizadas.
8. Comprobar que el backend compile correctamente.
9. Escribir documentación y explicaciones en español.

## Verificación
dotnet build backend/CleanHome.slnx --configuration Release --warnaserror

Las pruebas de integración deben utilizar una base de datos aislada.

## OpenSpec
Antes de implementar nuevas funcionalidades, revisar:
- openspec/changes/
- proposal.md
- specs/
- design.md
- tasks.md

Marcar tareas como completadas solo cuando exista evidencia.
