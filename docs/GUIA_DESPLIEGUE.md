# GuÃ­a de despliegue SIFTC

## 1. Arquitectura de despliegue

```text
Frontend (Vercel/Netlify)
        |
        +----------------------+
        |                      |
        v                      v
.NET API (Render)       Python API (Render)
        |                      |
        +----------+-----------+
                   |
                   v
          PostgreSQL (Neon)
             siftc_db
```

- `.NET` lee `analitica` y escribe Ãºnicamente lo permitido en `seguridad`.
- Python valida/carga la matriz y expone el ranking.
- Power BI se conecta directo a `analitica` con `rol_powerbi`.
- No se debe hostear Power BI: es un servicio de Microsoft.

## 2. Crear la base de datos en Neon

1. Crear cuenta en <https://neon.tech>.
2. Crear un proyecto en la regiÃ³n mÃ¡s cercana a los usuarios.
3. Usar la base por defecto de Neon (`neondb`) o crear `siftc_db`. Lo importante es que todos los scripts y todas las cadenas de conexiÃ³n usen **el mismo nombre**.
4. Copiar la cadena de conexiÃ³n **pooled** y la **direct**.
5. Abrir el SQL Editor de Neon, seleccionar esa base y ejecutar, en orden:
   - `Script-1.sql`
   - `Script-2.sql`
   - ...
   - `Script-16.sql`
   - `database/Script-17-vistas-sprint2.sql`

> `Script.sql` crea una base llamada `siftc1_db` y usa `OWNER = postgres`, que no existe en Neon. No lo ejecutes: empieza en `Script-1.sql` sobre `neondb` o sobre la base que hayas creado.
>
> Si `SELECT current_database();` devuelve `neondb` y allÃ­ corriste los scripts, usa `Database=neondb` en las cadenas. No hace falta crear `siftc_db`.

Verificar que existan:

```sql
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema IN ('cruda', 'limpia', 'analitica', 'seguridad')
ORDER BY 1, 2;

SELECT * FROM analitica.v_entidades_cotizacion LIMIT 5;
```

## 3. Roles y cadenas de conexiÃ³n

`Script-12.sql` crea `rol_etl`, `rol_backend` y `rol_powerbi`. Rotar las contraseÃ±as antes de publicar.

```sql
ALTER ROLE rol_etl PASSWORD '<nueva-clave-etl>';
ALTER ROLE rol_backend PASSWORD '<nueva-clave-backend>';
ALTER ROLE rol_powerbi PASSWORD '<nueva-clave-powerbi>';
```

Opcional, para el microservicio de ranking:

```sql
CREATE ROLE rol_ranking LOGIN PASSWORD '<nueva-clave-ranking>';
GRANT USAGE ON SCHEMA analitica TO rol_ranking;
GRANT SELECT ON ALL TABLES IN SCHEMA analitica TO rol_ranking;
ALTER DEFAULT PRIVILEGES IN SCHEMA analitica
    GRANT SELECT ON TABLES TO rol_ranking;
```

Cadenas:

```text
# .NET
Host=<host-neon-pooler>;Port=5432;Database=siftc_db;Username=rol_backend;Password=<clave>;SSL Mode=Require;Trust Server Certificate=true

# Python, si valida/carga la matriz
postgresql://rol_etl:<clave>@<host-neon-pooler>:5432/siftc_db?sslmode=require

# Python, si solo lee ranking
postgresql://rol_ranking:<clave>@<host-neon-pooler>:5432/siftc_db?sslmode=require

# Power BI
Host=<host-neon-directo>;Port=5432;Database=siftc_db;Username=rol_powerbi;Password=<clave>;SSL Mode=Require;Trust Server Certificate=true
```

Si la contraseÃ±a tiene caracteres especiales, URL-encodearla en `DATABASE_URL`.

## 4. Desplegar la API .NET en Render

1. Subir el repositorio a GitHub.
2. En Render: **New > Web Service**.
3. Conectar el repositorio `SIFTC`.
4. Configurar:
   - Language: **Docker**
   - Dockerfile path: `dockerfile`
   - Branch: `master`
   - Health check path: `/health`
   - Port: `8080`
   - Region: la mÃ¡s cercana a Neon
5. Variables de entorno:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ConnectionStrings__DefaultConnection=Host=<host-neon-pooler>;Port=5432;Database=siftc_db;Username=rol_backend;Password=<clave>;SSL Mode=Require;Trust Server Certificate=true
JwtSettings__Secret=<secreto-de-al-menos-32-caracteres>
JwtSettings__Issuer=SIFTC.Backend
JwtSettings__Audience=SIFTC.Clients
JwtSettings__ExpiryMinutes=480
PythonBackend__BaseUrl=https://backendfastapi-b3mt.onrender.com/
PythonBackend__TimeoutSeconds=90
Cors__AllowAnyOrigin=true
```

> `Cors__AllowAnyOrigin=true` es temporal. Cuando el frontend tenga URL fija:
> ```text
> Cors__AllowAnyOrigin=false
> Cors__AllowedOrigins__0=https://<frontend>
> ```

> El `.dockerignore` excluye `appsettings*.json` para no hornear secretos en la imagen. Por eso **todas** las variables crÃ­ticas deben estar definidas en Render; si falta `JwtSettings__Secret` o `ConnectionStrings__DefaultConnection`, la API no arranca.

6. Desplegar y probar:

```bash
curl -i https://<api>.onrender.com/health
```

Debe responder `200 {"status":"ok"}`.

### Notas de Render

- El plan free duerme el servicio por inactividad. La primera peticiÃ³n puede tardar 30â€“60 s.
- Para la demo, usar UptimeRobot/cron-job.org cada 10â€“14 minutos o pagar Starter.
- Si Render muestra `502`, revisar que la app escuche en `8080` y que el contenedor haya arrancado.
- `/docs` y `/openapi/v1.json` estÃ¡n disponibles tambiÃ©n en `Production` por defecto. Para desactivarlos, define `Documentation__Enabled=false`.

## 5. Configurar la API Python en Render

El servicio ya existe en `https://backendfastapi-b3mt.onrender.com`.

1. Entrar al servicio en Render.
2. **Environment > Add Environment Variable**:

```text
DATABASE_URL=postgresql://rol_etl:<clave>@<host-neon-pooler>:5432/siftc_db?sslmode=require
JWT_SECRET=<el mismo JwtSettings__Secret del .NET>
JWT_ISSUER=SIFTC.Backend
JWT_AUDIENCE=SIFTC.Clients
```

3. Guardar y hacer **Manual Deploy**.
4. Verificar:

```bash
curl -i https://backendfastapi-b3mt.onrender.com/
curl -i "https://backendfastapi-b3mt.onrender.com/api/v1/ranking?limit=1"
```

El error actual `connection to server at localhost:5433` desaparece cuando `DATABASE_URL` apunte a Neon.

> Si el servicio Python usa su propia migraciÃ³n/esquema, ejecutarla contra `siftc_db`. No crear una base paralela: el proyecto usa una sola base.

## 6. CORS

El backend .NET quedÃ³ con CORS abierto por defecto:

- `Cors__AllowAnyOrigin=true`
- Responde `Access-Control-Allow-Origin: *`
- Expone `X-Total-Count`

Probar preflight:

```bash
curl -i -X OPTIONS \
  -H "Origin: https://ejemplo.com" \
  -H "Access-Control-Request-Method: GET" \
  -H "Access-Control-Request-Headers: authorization" \
  https://<api>.onrender.com/api/v1/metricas
```

Debe responder `204` con `Access-Control-Allow-Origin: *`.

Cuando cierres el CORS, cambia `Cors__AllowAnyOrigin=false` y define `Cors__AllowedOrigins__0` con el dominio real.

## 7. Power BI

Power BI no se hostea: se usa Power BI Desktop y, si se publica, Power BI Service.

- Abrir Power BI Desktop.
- Obtener datos > PostgreSQL.
- Servidor: host directo de Neon (no el pooler si hay problemas).
- Base: `siftc_db`.
- Usuario: `rol_powerbi`.
- SSL: obligatorio.
- Esquema: `analitica`.

Para la sustentaciÃ³n:

- OpciÃ³n gratis: `.pbix` local + PDF/capturas.
- OpciÃ³n publicada: cuenta Power BI Pro (o la de la universidad) y publicar en `app.powerbi.com`.

## 8. Frontend

- Subir a Vercel, Netlify o Cloudflare Pages.
- Configurar la URL base de la API .NET.
- Mientras `Cors__AllowAnyOrigin=true`, cualquier origen funciona.
- Cuando cierres CORS, agregar el dominio del frontend en `Cors__AllowedOrigins__0`.

## 9. VerificaciÃ³n final

1. `GET /health` â†’ `200`.
2. `POST /api/v1/auth/login` con `usuario` y `password` â†’ `200` con `token`.
3. `GET /api/v1/entidades` con token â†’ `200`.
4. `GET /api/v1/metricas` con token â†’ `200`.
5. `GET /api/v1/ranking` con token â†’ `200` cuando Python tenga DB.
6. `POST /api/v1/cargas` con el archivo `MatrizFinancieraEntidadesPedido1.csv` (separador `;`) â†’ `200` cuando Python tenga DB.
7. `OPTIONS` con `Origin` â†’ `204` y `Access-Control-Allow-Origin: *`.
8. Power BI conectado a `analitica` con `rol_powerbi`.

## 10. Problemas comunes

| SÃ­ntoma | Causa probable | SoluciÃ³n |
|---|---|---|
| `503` en `/ranking` o `/cargas` | Python sin DB o dormido | Configurar `DATABASE_URL` y redesplegar |
| `connection refused localhost:5433` | Python sigue usando su DB local | Cambiar `DATABASE_URL` en Render |
| `502` en .NET | Contenedor no escucha en 8080 | Revisar `ASPNETCORE_URLS` y logs |
| `401` inesperado | Secret/issuer/audience distintos | Igualar `JwtSettings__*` y `JWT_*` |
| Error SSL en Neon | Falta `sslmode=require` | Agregar SSL a la cadena |
| CSV rechazado | Separador incorrecto | Usar `;` (archivo ya generado) |
| CORS bloqueado | Origen no permitido | `Cors__AllowAnyOrigin=true` temporal |
| Power BI no conecta | Host/puerto/SSL o rol | Usar host directo, `rol_powerbi`, SSL |

## 11. Seguridad mÃ­nima antes de entregar

- Rotar contraseÃ±as de `Script-12`.
- No subir secretos a GitHub; usar variables de Render.
- Mantener `rol_backend` y `rol_powerbi` sin escritura en `cruda`/`limpia`.
- Cerrar CORS cuando el frontend estÃ© estable.
- No exponer `rol_etl` al frontend ni a Power BI.

