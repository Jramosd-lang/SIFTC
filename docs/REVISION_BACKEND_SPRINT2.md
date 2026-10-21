# Revisión backend para Sprint 2

## Estándares consolidados

La guía de endpoints tiene prioridad sobre el código existente y sobre la HU cuando se contradigan. El backend de lectura usa .NET, Clean Architecture (`API` -> `Application` -> `Domain`, con `Infrastructure` para EF Core), MediatR y PostgreSQL/Npgsql. El microservicio Python se limita al cargue y validación de CSV.

- Toda ruta pública se publica bajo `/api/v1`; JSON UTF-8, propiedades `camelCase`, fechas ISO 8601 y números como números JSON.
- Autenticación JWT en todos los endpoints, salvo login. Errores 401, 403, 404 y validaciones se devuelven como RFC 9457 (`application/problem+json`).
- Los controladores delegan a casos de uso. Los DTO no son entidades EF; las consultas son asíncronas, reciben `CancellationToken` y usan `AsNoTracking()`.
- Listados: paginación, filtros, orden permitido explícitamente y orden secundario estable. Validar entradas antes de consultar.
- Consultas de portal solo leen la capa `analitica`; no calculan series, IC ni ranking bajo demanda. La carga CSV escribe en `cruda` desde Python.
- Las entidades EF reproducen los cuatro esquemas y respetan claves, longitudes, precisión decimal e índices de los scripts.

## Hallazgos prioritarios

1. **Contrato de URL pendiente de decisión.** La guía prescribe `GET /api/v1/entidades/{id}/ohlc`; HU-BE-03 menciona `GET /series/{id}`. Se debe publicar una sola ruta como contrato y actualizar OpenAPI antes de integrarla.
2. **Corregido: convenciones HTTP.** Los controladores usan `/api/v1`, el registro exige Administrador y los endpoints protegidos usan los nombres de rol almacenados en la base.
3. **Corregido: errores consistentes.** El Result pattern se traduce a Problem Details RFC 9457 con tipo URI estable, título, estado, detalle, instancia y extensión `code`.
4. **Corregido: roles.** `ParametrosController` autoriza `Analista financiero` y `Administrador`, que coinciden con el SQL semilla.
5. **Migración EF obsoleta.** `InitialCreate` describe una única tabla `usuarios` sin esquema y columnas distintas. No debe aplicarse contra la base creada por los scripts; antes de administrar esquema desde EF, se necesita una migración de baseline consensuada.
6. **Corregido en la aplicación.** La conexión y JWT se cargan desde variables de entorno/secretos, y Docker Compose las exige. Las credenciales de Script-12 deben rotarse antes de ejecutarse fuera de desarrollo.
7. **Corregido: CORS.** La política `Frontend` recibe los orígenes desde configuración y se aplica antes de autenticación/autorización.
8. **Datos insuficientes para parte del contrato.** `v_ranking` no contiene variación porcentual ni cambio de posición, y los scripts no definen el índice general del mercado. Análisis de datos debe proveer una vista/consulta acordada antes de implementar esos campos.

## Cambios aplicados

Se modelaron en EF Core las tablas de `seguridad`, `cruda`, `limpia` y `analitica`, con sus esquemas, longitudes, precisión e índices. `Usuario` ahora referencia un rol por FK, tal como exige el SQL, y el login carga ese rol antes de construir el JWT. Por eso, el comando de registro ahora recibe `idRol` (`Guid`) y no el enum anterior: los UUID de roles son los que realmente usa la base de datos.
