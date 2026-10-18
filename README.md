# CleanHome — Versión B

Plataforma web progresiva para la intermediación
de servicios de limpieza.

## Tecnologías

- ASP.NET Core 10 Web API
- Clean Architecture
- PostgreSQL 18
- Entity Framework Core 10
- xUnit
- React + TypeScript (pendiente)
- GitHub Actions (pendiente)

## Estructura

backend/
  src/
    CleanHome.Api/
    CleanHome.Application/
    CleanHome.Domain/
    CleanHome.Infrastructure/
  tests/
    CleanHome.UnitTests/
    CleanHome.IntegrationTests/

## Funcionalidades implementadas

- API REST con ASP.NET Core.
- Endpoint GET /health.
- Catálogo de servicios de limpieza.
- Conexión PostgreSQL mediante EF Core.
- Migraciones versionadas.

## Funcionalidades pendientes

- Autenticación y perfiles.
- Solicitudes y cotizaciones.
- Reservas.
- Chat y notificaciones.
- Pagos externos.
- Administración.
- Frontend PWA.
- Pipeline DevSecOps.

## Seguridad

Las credenciales de desarrollo se gestionan mediante
.NET User Secrets y no deben subirse al repositorio.
