# SIFTC API - Referencia completa

Version del documento: 1.0  
API: v1  
Base URL: `https://siftc.onrender.com`  
Documentacion interactiva: `https://siftc.onrender.com/docs`  
OpenAPI: `https://siftc.onrender.com/openapi/v1.json`

Este documento define el contrato publico de la API. No se debe asumir ningun comportamiento que no este descrito aqui.

---

## 1. Convenciones generales

### 1.1 Formato

- Todas las rutas usan el prefijo `/api/v1`, excepto `/health`, `/docs` y `/openapi`.
- Los cuerpos de solicitud y respuesta son JSON UTF-8.
- Las cargas de matriz usan `multipart/form-data`.
- Los errores usan `application/problem+json`.
- Los identificadores son UUID.
- Las fechas usan `yyyy-MM-dd`.
- Las fechas con hora usan ISO 8601 con zona, por ejemplo `2026-09-01T12:00:00+00:00`.
- Los valores monetarios y ratios se envian como numeros JSON, no como texto.
- No se aceptan valores `null` donde el contrato indica obligatorio.

### 1.2 Autenticacion

Todos los endpoints, excepto los publicos, requieren:

```http
Authorization: Bearer <token>
```

El token se obtiene en `POST /api/v1/auth/login` y contiene:

- `sub`: UUID del usuario.
- `email`: correo.
- `name`: nombre.
- rol del sistema.
- `jti`, `iss`, `aud`, `exp`.

Si el token falta, esta vencido o es invalido:

```json
{
  "type": "about:blank",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Token invalido o vencido",
  "instance": "/api/v1/..."
}
```

### 1.3 Roles

| Valor exacto | Uso |
|---|---|
| `Administrador` | Administracion, usuarios, parametros del IC. |
| `Data Steward` | Diccionario, calidad, lectura de matriz cruda y datos detallados. |
| `Analista financiero` | Cargas, analisis, aprobacion y rechazo de calculos. |
| `Gerente / Directivo` | Consulta del mercado y ranking sin posicion. |
| `Auditor` | Consulta, linaje, aprobaciones y auditoria. |
| `Usuario movil` | Consulta y watchlist personal. |

### 1.4 Paginacion

Los listados paginados usan:

- `page`: entero `>= 1`. Por defecto `1`.
- `size`: entero entre `1` y `100`. Por defecto `100`.

La cantidad total viaja en la cabecera:

```http
X-Total-Count: 288
```

El cuerpo es un arreglo, no un objeto con `items`.

### 1.5 Ordenamiento

Los parametros de ordenamiento son:

- `sort`: campo permitido por endpoint.
- `dir`: `asc` o `desc`.

Los valores se comparan sin distinguir mayusculas.

### 1.6 Enums

#### Frecuencia

| Valor canonico | Alias aceptados | Significado |
|---|---|---|
| `Diaria` | `diaria`, `diario` | Frecuencia diaria. |
| `Mensual` | `mensual` | Frecuencia mensual. |
| `Trimestral` | `trimestral` | Frecuencia trimestral. |

La respuesta siempre usa el valor canonico: `Diaria`, `Mensual` o `Trimestral`.

#### Dimension

- `Rentabilidad`
- `Liquidez`
- `Solvencia`
- `Crecimiento`
- `Mercado`
- `Trazabilidad`

`Ranking` solo admite las cuatro primeras.

#### CampoOrdenEntidad

- `codigo`
- `nombre`
- `sector`
- `ic`
- `variacionPct`

#### CampoOrdenMetrica

- `fecha`
- `codigo`
- `nombre`
- `sector`
- `dimension`
- `codigoMetrica`
- `valor`
- `valorNormalizado`

#### DireccionOrden

- `asc`
- `desc`

#### Estado de publicacion

- `pendiente`
- `publicado`
- `rechazado`

#### Estado de carga

- `Exitoso`
- `Fallido`

#### Severidad de umbral

- `Informativa`
- `Advertencia`
- `Critica`

### 1.7 Periodo y frecuencia

La API separa dos conceptos:

1. **Frecuencia de una metrica:** atributo del catalogo `dim_metrica`. Se usa en `GET /api/v1/metricas` con el parametro `periodo`.
2. **Periodo de consulta:** parametro de `GET /api/v1/ranking` y `GET /api/v1/entidades/{id}/ohlc`.

Reglas:

- Los valores canonicos son `Diaria`, `Mensual`, `Trimestral`.
- La comparacion no distingue mayusculas.
- Se aceptan `diario` y `diaria` como alias de `Diaria`.
- La respuesta siempre devuelve el valor canonico.
- La matriz actual es mensual. Por eso:
  - `Mensual` es el periodo con datos historicos completos.
  - `Trimestral` agrupa tres meses para OHLC.
  - `Diaria` devuelve los puntos disponibles; no inventa dias sin dato.
- `GET /api/v1/ranking` usa el ultimo periodo publicado. El parametro `periodo` se devuelve en la respuesta, pero hoy no reagrupa el ranking; la fuente es mensual.
- La frecuencia configurada para el IC se guarda en la version de configuracion. El ETL actual usa los pesos versionados; la frecuencia y los umbrales quedan registrados para las HUs de alertas y periodicidad.

### 1.8 Formato de error

Todos los errores usan Problem Details (RFC 9457):

```json
{
  "type": "https://siftc.local/problems/auth.forbidden",
  "title": "Forbidden",
  "status": 403,
  "detail": "No tiene permiso",
  "instance": "/api/v1/entidades",
  "code": "auth.forbidden"
}
```

Campos:

- `type`: URI del tipo de problema.
- `title`: titulo corto.
- `status`: codigo HTTP.
- `detail`: explicacion legible.
- `instance`: ruta solicitada.
- `code`: codigo estable para clientes.

### 1.9 Codigos HTTP

| Codigo | Significado |
|---|---|
| `200` | OK. |
| `201` | Recurso creado. |
| `204` | Sin contenido. |
| `400` | Query, body o binding invalido. |
| `401` | Token ausente, invalido o vencido. |
| `403` | Rol sin permiso. |
| `404` | Recurso inexistente. |
| `409` | Conflicto: duplicado, version ya decidida, etc. |
| `415` | Content-Type no soportado. |
| `422` | Regla de negocio incumplida. |
| `500` | Error interno. |
| `503` | Microservicio Python no disponible. |

Un rol sin permiso siempre recibe `403`, nunca `200` con arreglo vacio.

---

## 2. Catalogo de endpoints

| Metodo | Ruta | Roles | Descripcion |
|---|---|---|---|
| GET | `/health` | Publico | Estado de la API. |
| GET | `/docs` | Publico | Documentacion Scalar. |
| GET | `/openapi/v1.json` | Publico | Contrato OpenAPI. |
| POST | `/api/v1/auth/login` | Publico | Login JWT. |
| POST | `/api/v1/usuarios/registrar` | Administrador | Crear usuario. |
| GET | `/api/v1/entidades` | Todos | Listar entidades. |
| GET | `/api/v1/entidades/{id}` | Todos | Obtener entidad. |
| GET | `/api/v1/entidades/{id}/ohlc` | Todos | Serie OHLC. |
| GET | `/api/v1/metricas` | Todos | Listar metricas. |
| GET | `/api/v1/ranking` | Admin, Data Steward, Analista, Gerente, Auditor | Ranking. |
| GET | `/api/v1/watchlist` | Todos | Watchlist propia. |
| POST | `/api/v1/watchlist/{idEntidad}` | Usuario movil | Agregar. |
| DELETE | `/api/v1/watchlist/{idEntidad}` | Usuario movil | Quitar. |
| POST | `/api/v1/cargas` | Administrador, Analista | Cargar matriz. |
| POST | `/api/v1/parametros/actualizar-umbral` | Administrador, Analista | Placeholder de parametros. |
| GET | `/api/v1/aprobaciones` | Admin, Data Steward, Analista, Auditor | Bandeja pendiente. |
| POST | `/api/v1/aprobaciones/{idLote}/aprobar` | Analista | Aprobar lote. |
| POST | `/api/v1/aprobaciones/{idLote}/rechazar` | Analista | Rechazar lote. |
| GET | `/api/v1/ic/configuracion` | Administrador | Configuracion vigente del IC. |
| GET | `/api/v1/ic/configuracion/versiones` | Administrador | Historial de versiones. |
| POST | `/api/v1/ic/configuracion/versiones` | Administrador | Crear version. |

---

## 3. Endpoints

### 3.1 GET /health

- Publico.
- Sin parametros.
- Respuesta `200`:

```json
{ "status": "ok" }
```

No valida base de datos ni microservicio Python. Solo indica que el proceso .NET responde.

### 3.2 GET /docs y GET /openapi/v1.json

- Publicos.
- `/docs` puede responder `302` hacia `/docs/`; es el comportamiento normal de Scalar.
- `/openapi/v1.json` devuelve el contrato OpenAPI.

### 3.3 POST /api/v1/auth/login

- Publico.
- Body:

```json
{
  "usuario": "admin@siftc.local",
  "password": "Admin123*"
}
```

`usuario` es el correo. No existe un campo `username` separado.

Respuesta `200`:

```json
{
  "token": "<jwt>",
  "expiraEn": 28800,
  "usuario": {
    "id": "98c097d9-a661-4c29-ae77-b7a41945548a",
    "nombre": "Admin SIFTC",
    "rol": "Administrador"
  }
}
```

Errores:

- `400`: body invalido.
- `401`: correo o contrasena incorrectos.

### 3.4 POST /api/v1/usuarios/registrar

- Rol: Administrador.
- Body:

```json
{
  "email": "usuario@siftc.local",
  "password": "ClaveSegura123*",
  "primerNombre": "Nombre",
  "primerApellido": "Apellido",
  "idRol": "179acd26-b52c-4e75-8c26-0b6b1b5737e4"
}
```

- `idRol` debe existir en `seguridad.roles`.
- Respuesta `200`: UUID del usuario creado.
- `409`: correo ya registrado.
- `403`: rol distinto de Administrador.
- `400/422`: body invalido o datos incompletos.

### 3.5 GET /api/v1/entidades

- Roles: todos los seis.
- Query:

| Parametro | Tipo | Default | Descripcion |
|---|---|---|---|
| `sector` | string | vacio | Nombre exacto del sector. |
| `page` | int | 1 | Pagina, `>= 1`. |
| `size` | int | 100 | Tamano, `1..100`. |
| `sort` | string | `codigo` | `codigo`, `nombre`, `sector`, `ic`, `variacionPct`. |
| `dir` | string | `asc` | `asc`, `desc`. |

- Respuesta `200`: arreglo de entidades.
- Cabecera `X-Total-Count`.
- `400`: page/size/sort/dir invalido.

Entidad:

```json
{
  "id": "4ebbf629-0d2d-46ef-b233-93421e13eaee",
  "codigo": "ENT001",
  "nombre": "Andina Energia S.A.",
  "sector": "Energia",
  "ic": 55.042,
  "variacion": -0.531,
  "variacionPct": -0.9555
}
```

### 3.6 GET /api/v1/entidades/{id}

- Roles: todos.
- `id`: UUID.
- `200`: entidad.
- `404`: no existe.
- `400`: UUID invalido en ruta.

### 3.7 GET /api/v1/entidades/{id}/ohlc

- Roles: todos.
- Query:

| Parametro | Valores | Default |
|---|---|---|
| `periodo` | `Diaria`, `Mensual`, `Trimestral` | `Diaria` |

- `200`:

```json
{
  "entidadId": "4ebbf629-0d2d-46ef-b233-93421e13eaee",
  "entidad": "Andina Energia S.A.",
  "periodo": "Mensual",
  "serie": [
    {
      "fecha": "2026-09-01",
      "apertura": 55.042,
      "maximo": 55.042,
      "minimo": 55.042,
      "cierre": 55.042,
      "variacion": -0.531,
      "variacionPct": -0.9555
    }
  ]
}
```

- `400`: periodo invalido.
- `404`: entidad inexistente.

### 3.8 GET /api/v1/metricas

- Roles: todos.
- Query:

| Parametro | Valores | Default |
|---|---|---|
| `sector` | nombre exacto | vacio |
| `periodo` | `Diaria`, `Mensual`, `Trimestral` | todas |
| `fecha` | `yyyy-MM-dd` | todas |
| `dimension` | `Rentabilidad`, `Liquidez`, `Solvencia`, `Crecimiento`, `Mercado`, `Trazabilidad` | todas |
| `page` | `>= 1` | 1 |
| `size` | `1..100` | 100 |
| `sort` | `fecha`, `codigo`, `nombre`, `sector`, `dimension`, `codigoMetrica`, `valor`, `valorNormalizado` | `fecha` |
| `dir` | `asc`, `desc` | `desc` |

- `periodo` filtra por la frecuencia del catalogo de la metrica, no por la fecha del valor.
- La fecha de los datos actuales va del `2024-10-01` al `2026-09-01`; una fecha fuera de rango devuelve `200 []`.
- Respuesta `200`: arreglo de metricas.
- `X-Total-Count`.
- `400`: page, size, sort, dir, periodo, fecha o dimension invalidos.

Metrica:

```json
{
  "entidadId": "4ebbf629-0d2d-46ef-b233-93421e13eaee",
  "codigo": "ENT001",
  "nombre": "Andina Energia S.A.",
  "sector": "Energia",
  "fecha": "2026-09-01",
  "codigoMetrica": "RNT-01",
  "nombreMetrica": "ROE",
  "dimension": "Rentabilidad",
  "formula": "utilidad_neta / patrimonio",
  "frecuencia": "Mensual",
  "valor": 0.010896,
  "valorNormalizado": 0.6
}
```

### 3.9 GET /api/v1/ranking

- Roles: Administrador, Data Steward, Analista financiero, Gerente / Directivo, Auditor.
- Usuario movil: `403`.
- Query:

| Parametro | Valores | Default |
|---|---|---|
| `sector` | nombre exacto | vacio |
| `dimension` | `Rentabilidad`, `Liquidez`, `Solvencia`, `Crecimiento` | IC general |
| `periodo` | `Diaria`, `Mensual`, `Trimestral` | `Mensual` |
| `orden` | `posicion`, `entidad`, `sector`, `ic`, `variacionPct` | `ic` |
| `dir` | `asc`, `desc` | `desc` |
| `limit` | `1..200` | 50 |
| `offset` | `>= 0` | 0 |

- Para `Gerente / Directivo`, `posicion` y `cambioPosicion` se devuelven en `null`.
- Respuesta `200`:

```json
{
  "periodo": "Mensual",
  "total": 12,
  "items": [
    {
      "posicion": 1,
      "cambioPosicion": 0,
      "entidadId": "4ebbf629-0d2d-46ef-b233-93421e13eaee",
      "entidad": "Andina Energia S.A.",
      "sector": "Energia",
      "ic": 55.042,
      "variacionPct": -0.9555
    }
  ]
}
```

- `400`: dimension, periodo, orden, dir, limit u offset invalido.
- `503`: microservicio Python no disponible.

### 3.10 GET /api/v1/watchlist

- Roles: todos.
- Sin parametros.
- Devuelve solo la watchlist del usuario del token.
- Respuesta `200`: arreglo de entidades (mismo contrato de entidad).
- `401`: sin token; `403`: rol no permitido (no aplica en GET).
- Cada usuario solo ve su propia lista.

### 3.11 POST /api/v1/watchlist/{idEntidad}

- Rol: solo `Usuario movil`.
- Agrega la entidad.
- `204`: agregada o ya existia.
- `404`: entidad inexistente.
- `403`: rol distinto de Usuario movil.
- `401`: sin token.

### 3.12 DELETE /api/v1/watchlist/{idEntidad}

- Rol: solo `Usuario movil`.
- `204`: eliminada o no existia.
- `403`: rol distinto de Usuario movil.
- `401`: sin token.

### 3.13 POST /api/v1/cargas

- Roles: Administrador, Analista financiero.
- `multipart/form-data`, campo `file`.
- CSV:
  - Separador `;`.
  - Codificacion UTF-8.
  - 15 columnas: `codigo_entidad`, `nombre_entidad`, `sector`, `periodo`, `utilidad_neta`, `patrimonio`, `activos_totales`, `ebitda`, `ingresos`, `activo_corriente`, `pasivo_corriente`, `inventarios`, `pasivo_total`, `ebit`, `gastos_financieros`.
  - Periodo `YYYY-MM`.
- Respuesta `200`:

```json
{
  "mensaje": "CSV procesado correctamente",
  "lote": "268d23a8-44f2-48fd-8c35-b04cf1aa3005",
  "nombre_archivo": "matriz.csv",
  "ruta": "src/Csv/Crudos/268d23a8-44f2-48fd-8c35-b04cf1aa3005.csv",
  "total_filas": 288,
  "columnas_detectadas": ["..."],
  "validacion": "correcta"
}
```

- `422`: archivo ausente, columnas faltantes, tipos invalidos o errores de validacion; la lista viaja en `errores`.
- `503`: Python no disponible.
- `403`: rol no autorizado.

### 3.14 POST /api/v1/parametros/actualizar-umbral

- Roles: Administrador, Analista financiero.
- Sin body.
- Hoy es un placeholder; responde `200`:

```json
{ "mensaje": "Umbral actualizado" }
```

No modifica la base. La configuracion real del IC se hace en `/api/v1/ic/configuracion`.

### 3.15 GET /api/v1/aprobaciones

- Roles: Administrador, Data Steward, Analista financiero, Auditor.
- Gerente y Usuario movil: `403`.
- Devuelve solo lotes `pendiente`.
- Respuesta `200`:

```json
[
  {
    "idLote": "b632cbfe-6226-41f3-a660-0a140e1f8913",
    "nombreArchivo": "matriz.csv",
    "fechaCarga": "2026-11-10T04:40:03.740214+00:00",
    "registrosTotales": 288,
    "registrosValidos": 288,
    "estadoCarga": "Exitoso",
    "estado": "pendiente",
    "idUsuarioEjecutor": "01a1bce8-569d-7a19-b657-1502d3016596",
    "idUsuarioDecision": null,
    "fechaDecision": null,
    "comentario": null
  }
]
```

### 3.16 POST /api/v1/aprobaciones/{idLote}/aprobar

- Rol: solo Analista financiero.
- Sin body.
- El aprobador se toma del JWT.
- `204`: aprobado y publicado.
- `403`: rol incorrecto o el mismo analista ejecuto el lote.
- `404`: lote inexistente.
- `409`: ya fue decidido o no admite la operacion.
- `422`: lote sin registros validos o sin pendientes.

### 3.17 POST /api/v1/aprobaciones/{idLote}/rechazar

- Rol: solo Analista financiero.
- Body obligatorio:

```json
{ "comentario": "Motivo del rechazo" }
```

- `204`: rechazado.
- `400`: body ausente o mal formado.
- `415`: sin `Content-Type: application/json`.
- `422`: comentario vacio o lote sin datos validos.
- `403`: rol incorrecto.
- `404`: lote inexistente.
- `409`: ya fue decidido.

### 3.18 GET /api/v1/ic/configuracion

- Rol: solo Administrador.
- Devuelve la version vigente.
- `200`:

```json
{
  "idVersion": "uuid",
  "numeroVersion": 1,
  "fechaVigencia": "2026-11-10T17:21:38+00:00",
  "motivo": "Version inicial",
  "idUsuario": "98c097d9-a661-4c29-ae77-b7a41945548a",
  "frecuenciaIc": "Mensual",
  "pesos": [
    { "dimension": "Rentabilidad", "peso": 0.35 }
  ],
  "umbrales": []
}
```

- `404`: no existe version.
- `403`: rol distinto de Administrador.

### 3.19 GET /api/v1/ic/configuracion/versiones

- Rol: solo Administrador.
- Devuelve hasta 100 versiones, de la mas reciente a la mas antigua.
- `200`: arreglo de configuraciones con el mismo contrato.

### 3.20 POST /api/v1/ic/configuracion/versiones

- Rol: solo Administrador.
- Body:

```json
{
  "motivo": "Ajuste de pesos",
  "frecuencia": "Mensual",
  "pesos": [
    { "dimension": "Rentabilidad", "peso": 0.35 },
    { "dimension": "Liquidez", "peso": 0.20 },
    { "dimension": "Solvencia", "peso": 0.20 },
    { "dimension": "Crecimiento", "peso": 0.25 }
  ],
  "umbrales": [
    {
      "codigoMetrica": "RNT-01",
      "limiteInferior": 0,
      "limiteSuperior": 1,
      "severidad": "Advertencia"
    }
  ]
}
```

Reglas:

- `motivo`: obligatorio, maximo 500.
- `frecuencia`: `Diaria`, `Mensual` o `Trimestral`.
- `pesos`: proporcion, no porcentaje. Suma `1.000000` con tolerancia `0.000001`.
- No se repiten dimensiones.
- Cada peso esta entre 0 y 1.
- `severidad`: `Informativa`, `Advertencia` o `Critica`.
- `limiteInferior <= limiteSuperior` si ambos vienen.
- `fechaVigencia` la asigna el servidor.
- La version es inmutable y aplica al siguiente calculo.

Respuesta `201`:

```json
{ "idVersion": "uuid" }
```

Errores:

- `403`: rol incorrecto.
- `422`: suma distinta, motivo vacio, dimension inexistente, metrica inexistente, severidad invalida o rango invertido.

---

## 4. DTOs

### AuthResponseDto

| Campo | Tipo | Descripcion |
|---|---|---|
| `token` | string | JWT. |
| `expiraEn` | int | Segundos de vigencia. |
| `usuario` | object | `id`, `nombre`, `rol`. |

### EntidadDto

| Campo | Tipo | Descripcion |
|---|---|---|
| `id` | UUID | Identificador. |
| `codigo` | string | Ticker. |
| `nombre` | string | Nombre. |
| `sector` | string | Sector. |
| `ic` | decimal? | Ultimo IC. |
| `variacion` | decimal? | Variacion absoluta. |
| `variacionPct` | decimal? | Variacion porcentual. |

### MetricaDto

| Campo | Tipo | Descripcion |
|---|---|---|
| `entidadId` | UUID | Entidad. |
| `codigo` | string | Ticker. |
| `nombre` | string | Entidad. |
| `sector` | string | Sector. |
| `fecha` | date | Periodo. |
| `codigoMetrica` | string | Codigo de metrica. |
| `nombreMetrica` | string | Nombre. |
| `dimension` | string | Dimension. |
| `formula` | string | Formula. |
| `frecuencia` | string | `Diaria`, `Mensual`, `Trimestral`. |
| `valor` | decimal? | Valor. |
| `valorNormalizado` | decimal? | Valor normalizado 0..1. |

### RankingItemDto

| Campo | Tipo | Descripcion |
|---|---|---|
| `posicion` | int? | Posicion; `null` para Gerente. |
| `cambioPosicion` | int? | Cambio; `null` para Gerente. |
| `entidadId` | UUID | Entidad. |
| `entidad` | string | Nombre. |
| `sector` | string | Sector. |
| `ic` | decimal | IC. |
| `variacionPct` | decimal | Variacion. |

### PublicacionPendienteDto

Ver ejemplo de `/api/v1/aprobaciones`.

### ConfiguracionIcDto

Ver ejemplo de `/api/v1/ic/configuracion`.

---

## 5. Estado actual y limitaciones conocidas

Estas limitaciones son parte del contrato actual y deben considerarse antes de consumir la API:

- `GET /api/v1/metricas` con `periodo=Diaria` solo devuelve metricas cuya frecuencia de catalogo sea diaria; no convierte datos mensuales en diarios.
- `GET /api/v1/entidades/{id}/ohlc` con `periodo=Diaria` devuelve los puntos disponibles de la fuente mensual; no inventa dias.
- `GET /api/v1/ranking` devuelve el ultimo periodo publicado; el parametro `periodo` se devuelve en la respuesta, pero no reagrupa el ranking.
- La frecuencia y los umbrales de la configuracion del IC se guardan versionados; los pesos ya los usa el ETL. La frecuencia y los umbrales se usaran en las HUs de periodicidad y alertas.
- `POST /api/v1/parametros/actualizar-umbral` es un placeholder y no persiste cambios.
- La lectura de matriz cruda y datos financieros detallados para Data Steward y Auditor no tiene endpoint implementado.
- La auditoria minima y sus endpoints estan pendientes.
- El filtrado por sensibilidad campo a campo esta pendiente.
- Las pruebas automatizadas actuales cubren validacion del IC; las pruebas por perfil de todos los endpoints deben integrarse en el repositorio de pruebas acordado.

---

## 6. Matriz de permisos

La fuente oficial es `docs/Matriz_Permisos_SIFTC_v1.1.pdf`. La fila de configuracion del IC es:

| Recurso | Admin | Data Steward | Analista | Gerente | Auditor | Usuario movil |
|---|---|---|---|---|---|---|
| Configuracion del IC: pesos, umbrales y periodos | E | SA | SA | SA | SA | SA |

`E` = escritura; `SA` = sin acceso.

---

## 7. Ejemplos cURL

### Login

```bash
curl -X POST "https://siftc.onrender.com/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"usuario":"admin@siftc.local","password":"Admin123*"}'
```

### Listar entidades

```bash
curl "https://siftc.onrender.com/api/v1/entidades?page=1&size=20&sort=ic&dir=desc" \
  -H "Authorization: Bearer $TOKEN"
```

### Configuracion del IC

```bash
curl "https://siftc.onrender.com/api/v1/ic/configuracion" \
  -H "Authorization: Bearer $TOKEN"
```

### Crear version del IC

```bash
curl -X POST "https://siftc.onrender.com/api/v1/ic/configuracion/versiones" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "motivo": "Ajuste de pesos",
    "frecuencia": "Mensual",
    "pesos": [
      {"dimension":"Rentabilidad","peso":0.35},
      {"dimension":"Liquidez","peso":0.20},
      {"dimension":"Solvencia","peso":0.20},
      {"dimension":"Crecimiento","peso":0.25}
    ],
    "umbrales": []
  }'
```
