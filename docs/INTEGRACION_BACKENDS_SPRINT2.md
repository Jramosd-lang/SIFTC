# Integración Sprint 2: .NET, Python y PostgreSQL

## 1. Decisión de arquitectura

Los dos backends comparten una sola base PostgreSQL. No se llama un backend desde el otro:

- **.NET (`SIFTC.API`)**: API de consulta. Lee únicamente `analitica` y opera usuarios/bitácora en `seguridad`.
- **Python (FastAPI)**: cargue/ETL del CSV y microservicio de ranking. Escribe en `cruda`/`limpia`/`analitica` con `rol_etl` y expone `GET /api/v1/ranking` leyendo `analitica.v_ranking`.
- **Power BI**: lee `analitica` con `rol_powerbi`.

```
Frontend / Móvil
   |                         |
   v                         v
.NET /api/v1/metricas    Python /api/v1/ranking
   |                         |
   +-------- PostgreSQL -----+
             siftc_db
        cruda -> limpia -> analitica
```

La conexión es por base de datos, no por HTTP. Así se evita duplicar cálculos y se mantiene un solo origen de verdad.

## 2. Roles y mínimo privilegio

Los scripts `Script-12` ya separan los roles. El backend .NET **nunca** debe usar `rol_etl` ni el usuario `postgres`.

| Rol | Uso | Permisos |
|---|---|---|
| `rol_etl` | Python ETL / cargue | ALL en `cruda`, `limpia`, `analitica` |
| `rol_backend` | API .NET | USAGE en `analitica` y `seguridad`; SELECT en `analitica`; operaciones de usuarios, watchlist y bitácora |
| `rol_powerbi` | Power BI y, si se acuerda, ranking Python | USAGE y SELECT solo en `analitica` |

Recomendación: crear un `rol_ranking` propio para Python con `USAGE` + `SELECT` solo en `analitica`, en lugar de reutilizar `rol_powerbi`.

```sql
CREATE ROLE rol_ranking LOGIN PASSWORD '<definir-en-el-secreto>';
GRANT USAGE ON SCHEMA analitica TO rol_ranking;
GRANT SELECT ON ALL TABLES IN SCHEMA analitica TO rol_ranking;
ALTER DEFAULT PRIVILEGES IN SCHEMA analitica
    GRANT SELECT ON TABLES TO rol_ranking;
```

Antes de desplegar, rotar las contraseñas de `Script-12` y pasarlas por variables de entorno; no dejarlas en el repositorio.

## 3. Vistas necesarias para Sprint 2

Ejecutar **una vez** como dueño de los objetos (`postgres`), después de `Script-9` y `Script-16`:

```bash
psql -h <host> -U postgres -d siftc_db -f database/Script-17-vistas-sprint2.sql
```

El script:

- crea `analitica.v_entidades_cotizacion` (aprobada por Análisis de Datos y usada por `GET /api/v1/entidades`);
- concede `SELECT` a `rol_backend` y `rol_powerbi`, y `ALL` a `rol_etl`.

`GET /api/v1/metricas` no crea ni modifica vistas: lee `analitica.fct_valores_metrica`, `analitica.dim_metrica`, `analitica.dim_dimension` y `analitica.v_entidades_cotizacion`. El backend no consulta `cruda` ni `limpia`.

## 4. Contrato de `GET /api/v1/metricas`

La guía de Frontend no detalla este recurso. Se implementa en formato largo para cubrir las 10 métricas de la tabla y sus dimensiones, con las convenciones del equipo (camelCase, ISO 8601, números JSON, JWT obligatorio).

```http
GET /api/v1/metricas?sector=Energía&periodo=mensual&fecha=2026-09-30&dimension=Rentabilidad&page=1&size=100&sort=codigoMetrica&dir=asc
Authorization: Bearer <token>
```

| Parámetro | Valores |
|---|---|
| `sector` | Nombre exacto del sector; vacío = todos |
| `periodo` | Frecuencia: `diario`, `mensual` o `trimestral`; vacío = todas |
| `fecha` | Fecha del corte ISO 8601 (`yyyy-MM-dd`); vacío = todas |
| `dimension` | `Rentabilidad`, `Liquidez`, `Solvencia`, `Crecimiento`, `Mercado` o `Trazabilidad` |
| `page` | Entero >= 1; por defecto 1 |
| `size` | Entero 1..100; por defecto 100 |
| `sort` | `fecha`, `codigo`, `nombre`, `sector`, `dimension`, `codigoMetrica`, `valor` o `valorNormalizado` |
| `dir` | `asc` o `desc`; por defecto `desc` |

Respuesta `200`:

```json
[
  {
    "entidadId": "0192f0a1-7c3e-7a4b-9d1e-2f6a8c0b1d23",
    "codigo": "ENT001",
    "nombre": "Entidad A",
    "sector": "Energía",
    "fecha": "2026-09-30",
    "codigoMetrica": "RNT-01",
    "nombreMetrica": "ROE",
    "dimension": "Rentabilidad",
    "formula": "utilidad_neta / patrimonio",
    "frecuencia": "Mensual",
    "valor": 0.1842,
    "valorNormalizado": 0.72
  }
]
```

La cabecera `X-Total-Count` lleva el total antes de paginar. CORS la expone al frontend.

Errores bajo RFC 9457 (`application/problem+json`):

```json
{
  "type": "about:blank",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Token inválido o vencido",
  "instance": "/api/v1/metricas",
  "code": "auth.unauthorized"
}
```

`400` para `page`, `size`, `sort`, `dir`, `periodo`, `fecha` o `dimension` inválidos; `401` sin token o token vencido; `403` para roles insuficientes. Los listados devuelven `200` con arreglo vacío si no hay datos, no `404`.

Dependencia con Análisis de Datos: `CRE-02` y `CAL-01` aparecerán en `/metricas` solo si el ETL las escribe en `fct_valores_metrica`. `CAL-01` es por lote; si no se replica por entidad, no puede salir con `entidadId`.

## 5. JWT compartido entre .NET y Python

El login vive en .NET. Python debe validar exactamente el mismo token:

- Mismo `JwtSettings:Secret`, `Issuer` y `Audience` en ambos servicios.
- Algoritmo `HS256`.
- El rol viaja en el claim de `ClaimTypes.Role`, que en el JWT se serializa como:
  `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`.
- Python debe validar `iss`, `aud`, `exp` y la firma antes de atender `/ranking`.

Variables de entorno sugeridas:

```text
ConnectionStrings__DefaultConnection=Host=<host>;Port=5432;Database=siftc_db;Username=rol_backend;Password=<secreto>;SSL Mode=Require;Trust Server Certificate=true
JwtSettings__Secret=<mismo secreto que Python>
JwtSettings__Issuer=SIFTC.Backend
JwtSettings__Audience=SIFTC.Clients
JwtSettings__ExpiryMinutes=480
Cors__AllowedOrigins__0=https://<frontend>
```

En Python:

```text
DATABASE_URL=postgresql://rol_ranking:<secreto>@<host>:5432/siftc_db?sslmode=require
JWT_SECRET=<mismo secreto>
JWT_ISSUER=SIFTC.Backend
JWT_AUDIENCE=SIFTC.Clients
```

## 6. Puente al microservicio Python

El backend .NET expone dos rutas propias y reenvía al microservicio:

| .NET | Python | Método |
|---|---|---|
| `/api/v1/ranking` | `/api/v1/ranking` | GET |
| `/api/v1/rankings` | `/api/v1/ranking` | GET (alias) |
| `/api/v1/validar-matriz` | `/v1/validar-matriz` | POST multipart, campo `file` |

Configuración:

```text
PythonBackend__BaseUrl=https://backendfastapi-b3mt.onrender.com/
PythonBackend__TimeoutSeconds=90
```

`/ranking` valida localmente `sector`, `dimension`, `periodo`, `orden`, `dir`, `limit` (1..200) y `offset` (>=0). Si el microservicio no responde o devuelve un error, el puente responde `503 application/problem+json` sin filtrar detalles internos.

`/validar-matriz` exige rol `Analista financiero` o `Administrador`. Si Python devuelve `422`, el puente conserva el mensaje y la lista de errores en la extensión `errores` de Problem Details:

```json
{
  "type": "https://siftc.local/problems/matriz.invalid",
  "title": "Matriz inválida",
  "status": 422,
  "detail": "El CSV contiene errores de validación",
  "instance": "/api/v1/validar-matriz",
  "code": "matriz.invalid",
  "errores": [
    { "tipo": "columnas_faltantes", "columnas": ["codigo_entidad", "..."] }
  ]
}
```

Estado del microservicio al momento de la verificación: `GET /api/v1/ranking` responde `400` porque su PostgreSQL apunta a `localhost:5433` dentro de Render. El puente lo traduce a `503`; el equipo Python debe configurar su `DATABASE_URL` real.

## 7. Hosting recomendado

### Opción recomendada para la demo

- **Base de datos**: Neon o Supabase (PostgreSQL administrado, SSL, free tier suficiente). Si se paga, Render PostgreSQL en la misma región que las APIs simplifica la red privada.
- **.NET**: Render Web Service con Docker (`dockerfile` corregido para `SIFTC.Api.dll`, puerto 8080). Plan Starter para evitar el cold start del plan free, que puede superar 1 s.
- **Python**: Render, como ya está.
- **Power BI**: conexión directa a `analitica` con `rol_powerbi`.

Configurar el puerto 8080 en Render y `ASPNETCORE_URLS=http://+:8080`. Si se usa el proxy TLS de Render, habilitar `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.

### Alternativas

- **Azure**: App Service para .NET + Azure Database for PostgreSQL; útil si el curso da crédito Azure.
- **Railway / Fly.io**: cómodos para ambos contenedores; revisar región y costo.
- **Supabase**: además del Postgres, da un panel SQL y backups; no usar su Auth si el login sigue en .NET.

### Advertencias

- El PostgreSQL free de Render tiene vencimiento; Neon/Supabase no vencen, pero limitan almacenamiento.
- Los Web Services free de Render se duermen: no cumplen el RNF de < 1 s en la primera petición de la demo.
- No exponer `rol_etl` al frontend ni a Power BI.
- Mantener `siftc_db` como nombre único; los scripts originales crean `siftc1_db` y `docker-compose` usa `siftc_db`. Estandarizar antes de desplegar.
