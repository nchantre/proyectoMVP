# ProyectMVP — Sistema anti-hurto Ceiba

Solución .NET 8 en **Arquitectura Limpia**.

## Puertos de desarrollo (fijos)

| Proyecto | URL | Puerto |
|----------|-----|--------|
| **ProyectMVP.Api** | http://localhost:5290/swagger | 5290 |
| **ProyectMVP.Web** | http://localhost:5103 | 5103 |

El mapa (Web) llama a la API por proxy usando `ApiBaseUrl: http://localhost:5290`.

## Error al compilar (archivo en uso)

Si ves `MSB3026` / `file is locked by ProyectMVP.Api` o `ProyectMVP.Web`, **no es un error de puerto en el código**: hay instancias anteriores aún ejecutándose.

```powershell
cd d:\ProyectoMVP\ProyectoMVP\proyectoMVP
.\scripts\stop-dev.ps1
dotnet build ProyectMVP.sln
```

## Ejecutar todo (recomendado)

```powershell
cd d:\ProyectoMVP\ProyectoMVP\proyectoMVP
.\scripts\run-dev.ps1
```

Abre dos ventanas: API (5290) y Web (5103).

## Ejecutar manualmente

```powershell
# Terminal 1
dotnet run --project src/ProyectMVP.Api --launch-profile http

# Terminal 2
dotnet run --project src/ProyectMVP.Web --launch-profile http
```

## Estructura

```
src/
  ProyectMVP.Domain/
  ProyectMVP.Application/
  ProyectMVP.Infrastructure/
  ProyectMVP.Identity/
  ProyectMVP.Api/            → puerto 5290
  ProyectMVP.Web/            → puerto 5103
  ProyectMVP.DeviceAgent/
scripts/
  stop-dev.ps1               → libera puertos y procesos
  run-dev.ps1                → inicia API + Web
```

## Base de datos (SQL Server)

```powershell
sqlcmd -S NELSON -U sa -P root -Q "IF DB_ID('VehicleTheftMvpDb') IS NULL CREATE DATABASE VehicleTheftMvpDb;"
sqlcmd -S NELSON -U sa -P root -d VehicleTheftMvpDb -i "d:\ProyectoMVP\SQL\01_schema_mvp.sql"
sqlcmd -S NELSON -U sa -P root -d VehicleTheftMvpDb -i "d:\ProyectoMVP\SQL\02_seed_mvp.sql"
```

Cadena en `src/ProyectMVP.Api/appsettings.json`.

## Paso 5 — Login policía + reportes de hurto

| Recurso | Detalle |
|---------|---------|
| Login | `POST /api/v1/auth/login` — usuario `policia` / contraseña `demo` |
| Avistamientos | `GET /api/v1/plates/{plate}/sightings` — requiere `Authorization: Bearer {token}` |
| Reportes hurto | `GET /api/v1/plates/{plate}/stolen-reports` — JWT; solo reportes del país del usuario |
| Web | http://localhost:5103 — formulario de login + panel de hurtos |

El adaptador `DevIdentityProvider` simula LDAP/REST por país; en producción se sustituye en `ProyectMVP.Identity`.

## Paso 6 — Importación de hurtos (multi-formato)

| Recurso | Detalle |
|---------|---------|
| Importación | `POST /api/v1/admin/stolen-reports/import` — rol `AdminImporter` |
| Usuario admin | `admin` / `demo` (también puede consultar como policía) |
| Formato JSON | Arreglo `records` en el body (`IStolenVehicleSource` estándar REST) |
| Formato CSV | Campo `payload` con texto CSV (simula BD legacy de otro país) |

Tras importar, los avistamientos existentes de esa placa se marcan con `IsPotentialMatch` si hay hurto ACTIVE.

Adaptadores en `Infrastructure/Import/`: `JsonStolenVehicleSource`, `CsvStolenVehicleSource`.

## Paso 7 — Analytics / hotspots

| Recurso | Detalle |
|---------|---------|
| Hotspots | `GET /api/v1/analytics/hotspots` — JWT policía, solo su país |
| Query | `category` = `all` \| `theft` \| `sighting` \| `potential_match` |
| Query | `from` / `to` (UTC, opcional; default últimos 30 días) |
| Web | Checkbox **Mapa de calor (tendencias)** en http://localhost:5103 |

Cada hotspot incluye `latitude`, `longitude`, `count`, `intensity` (0–1) para capas de calor o Power BI.

## Pruebas rápidas

- Swagger: http://localhost:5290/swagger (botón **Authorize** con el JWT)
- Mapa: http://localhost:5103
- HTTP file: `src/ProyectMVP.Api/ProyectMVP.Api.http` (login + consultas con token)
