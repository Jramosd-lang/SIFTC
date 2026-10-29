---
name: siftc-sprint2-endpoints
description: Implement or review SIFTC Sprint 2 read endpoints in the .NET API, preserving the approved frontend contract, clean layers, and PostgreSQL analytical-read boundary. Do not use for the Python CSV upload microservice.
---

# SIFTC Sprint 2 endpoints

Use this skill for HU-BE-03: entities, metrics, OHLC series, ranking, and their filter catalogues. The frontend guide is the highest-priority API contract. Python is the sole exception: it is the CSV-upload microservice and does not change how Sprint 2 read endpoints are built.

## Before implementing

1. Read the target endpoint contract and confirm it is compatible with the current OpenAPI document.
2. Read only from the `analitica` schema for public market data. Use its calculated views/tables; do not recalculate IC, ranking, or series in the request path.
3. Resolve contract disagreements before coding. Current discrepancy: the frontend guide defines `GET /api/v1/entidades/{id}/ohlc`, whereas HU-BE-03 mentions `GET /series/{id}`. Treat the guide route as authoritative unless the team formally updates OpenAPI.

## Required conventions

- Route prefix: `/api/v1`.
- JSON fields use `camelCase`; dates use ISO 8601 (`yyyy-MM-dd`); numeric values are JSON numbers, never formatted strings.
- Protect every data endpoint with JWT. Return RFC 9457 `application/problem+json` for failures: a stable problem-type URI, title, status, detail, instance and an optional machine-readable `code`. Use 401 for missing/invalid/expired tokens, 403 for insufficient role, 404 for missing resources, 422 for validation failure, and 400 for invalid query input.
- Controllers delegate to MediatR commands/queries. Keep use-case logic in `SIFTC.Application`, entities/value invariants in `SIFTC.Domain`, and EF/Npgsql access in `SIFTC.Infrastructure`.
- Apply CQRS: a command mutates state and a query only reads it; each is an `IRequest<Result<T>>` with its own MediatR handler. Never put business or EF logic in a controller.
- Use the Result pattern for expected failures and map its error type/code at the API boundary. Construct aggregates through their factory method so validation and identifier generation stay in the domain.
- Keep one domain entity per database table in `SIFTC.Domain/Entities`; do not create a combined model file. Each entity exposes a private EF constructor and a `Create(...)` factory returning `Result<T>`, following `Usuario.Create` and the project's `Error`/`ErrorType` conventions.
- Use asynchronous calls and `CancellationToken` end-to-end. Read queries use `AsNoTracking()` and project directly to DTOs.
- Never expose EF entities directly from controllers; return endpoint DTOs matching the contract examples.
- Validate query inputs at the API/application boundary. Listings consistently support `page`, `size`, and `sort`; ranking/metrics support sector and period filters. Whitelist sort fields and cap `size`.
- Preserve deterministic ordering, including a secondary key when sorting can tie.

## Endpoint checklist

- `GET /api/v1/entidades`: optional `sector`; current quote and its absolute/percentage variation.
- `GET /api/v1/mercado/top`: optional `limite`, default 5; backend orders winners and losers.
- `GET /api/v1/indice-general`: latest market IC and variation versus previous close.
- `GET /api/v1/ranking`: sector, dimension, period, order and direction filters; include position movement if the analytic data supports it.
- `GET /api/v1/entidades/{id}/ohlc`: validate range `1D|1S|1M|1A|MAX`; return oldest-to-newest and fill missing calculation days with the prior close when required by the contract.
- `GET /api/v1/sectores`, `/dimensiones`, `/periodos`: stable catalogue DTOs for filters.

## Data-model rules

- Keep schema/table names and precision aligned to the SQL scripts. `seguridad.usuarios` references `seguridad.roles`; analytic facts reference the clean/crude identifiers for traceability.
- Persist the `Frecuencia` domain enum as text through Fluent API (`HasConversion<string>()`). A new enum value also requires a matching database migration for the `analitica.dim_metrica.frecuencia` `CHECK` constraint.
- Use `DateOnly` for PostgreSQL `DATE` and `DateTimeOffset` for `TIMESTAMPTZ`.
- The .NET API backend DB role is read-only in `analitica`, except explicitly permitted operations in `seguridad`. Do not add writes to `cruda` or `limpia`.
- CSV parsing, raw inserts, and CSV validation are Python microservice concerns. The .NET API may consume their resulting data, not duplicate their upload workflow.

## Verification

- Build the solution and add/adjust migration only after confirming the target database lifecycle with the team.
- Test authorization (401/403), validation (400/422), no-data (404 where applicable), filter/sort/page behavior, JSON field names, and sample responses.
- Measure 20 representative requests; typical responses must remain under one second.
- Document every released endpoint with summaries, parameter descriptions, response examples, and status codes in OpenAPI.
