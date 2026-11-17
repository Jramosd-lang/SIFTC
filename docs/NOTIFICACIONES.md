# Notificaciones (HU-AD-10 + HU-MO-05)

Correo por **SMTP** y push por **Firebase Cloud Messaging (FCM v1)**, con reglas por
usuario y reintentos registrados.

| CA | Requisito | Dónde se cumple |
|---|---|---|
| **CA1** | Envío por correo y push en menos de 1 minuto | Worker en background con intervalo corto + prioridad alta en FCM + `fecha_creacion → fecha_envio` en `seguridad.notificaciones_envios` |
| **CA2** | Reglas configurables por usuario (canal, entidades y umbral) | Tabla `seguridad.notificaciones_reglas` + CRUD en la API |
| **CA3** | Reintentos y fallos registrados | `estado`, `intentos`, `max_intentos`, `ultimo_error` en `seguridad.notificaciones_envios` (+ evento en `seguridad.auditorias` con `accion=notificacion`) |

Base de datos: **`database/Script-25-notificaciones-usuario.sql`** (idempotente, ejecutar
como dueño de los objetos, después de Script-22). **Ya aplicado en local y en Neon.**

---

## 0. Estado y plan de trabajo

**Implementado y probado (HU-AD-10 / HU-MO-05):**
- 3 tablas (`seguridad.notificaciones_reglas/_tokens/_envios`) aplicadas en local y Neon.
- Backend .NET completo: mapeo EF, repositorio, CRUD de reglas (CA2), tokens FCM,
  generador de avisos, worker en background (CA1), reintentos y auditoría (CA3).
- Trigger real: al publicarse un lote (`LotePublicadoEvent`) se encolan los avisos de las
  entidades cuya variación de IC cumple las reglas. También existe
  `POST /api/v1/notificaciones/eventos` para que el ETL/otro backend ingrese eventos.
- 21 pruebas unitarias de notificaciones (74 en total en la suite) y E2E contra Neon
  (`verificar_notificaciones.ps1`, 24/24 PASS).

**Pendiente de quien tenga la consola Firebase / Render** (bloquea la entrega real, no el
código):
- `Firebase__CredentialsJson` (§1.6/§4) → sin esto FCM no envía.
- Credenciales SMTP reales (§4) → sin esto el correo no envía.
- Push web: clave VAPID (§1.3) y `google-services.json` (§1.4) cuando el front/móvil los pidan.

Sin esas credenciales la API **arranca igual**: cada envío queda en la cola como
`fallido` con `ultimoError = "SMTP no configurado..."` / `"Firebase no configurado..."`,
que es justamente la evidencia del CA3.

**Tareas del plan original (todas cerradas):**

| # | Tarea | CA | Estado |
|---|---|---|---|
| 1 | Configurar backend | — | Hecho: `FirebaseAdmin 3.7.1` + `MailKit 5.0.0`, variables §4, mapeo EF de las 3 tablas |
| 2 | CRUD de reglas | **CA2** | Hecho: §5, validación de `canal`, `umbral >= 0`, `idEntidad` opcional (= todas) |
| 3 | Registro de tokens | — | Hecho: `POST/DELETE /notificaciones/tokens` |
| 4 | Worker de envío | **CA1** | Hecho: `NotificacionesWorker` cada `IntervaloSegundos`; FCM `Priority.High` |
| 5 | Reintentos y fallos | **CA3** | Hecho: `intentos`, `ultimo_error`, `max_intentos=3`, `UNREGISTERED` desactiva token, evento en `seguridad.auditorias` |
| 6 | Verificación | CA1-3 | Hecho: §7 (24/24 contra Neon sin credenciales) |

---

## 1. Setup de Firebase (consola)

> La **Cloud Messaging API (Legacy)** fue retirada (junio 2024): **no existen Server keys
> legacy**. Todo el envío es por la **API v1** autenticada con una **cuenta de servicio**.

### 1.1 Crear el proyecto

1. <https://console.firebase.google.com> → **Crear proyecto**.
2. Nombre: `siftc-notificaciones` → Continuar.
3. Google Analytics: desactivar (o asociar si ya tienen cuenta) → **Crear proyecto** → Continuar.

### 1.2 Registrar la app Web (PWA)

1. Panel → icono **Web** (`</>`) → nombre `siftc-web` → **Registrar app**.
2. Copiar el objeto `firebaseConfig` (`apiKey`, `authDomain`, `projectId`,
   `messagingSenderId`, `appId`). **No es secreto**: `apiKey` es pública por diseño.
3. Continuar → saltar el SDK (el frontend instala `firebase` con npm).

### 1.3 Clave VAPID (obligatoria para web push)

1. **Configuración del proyecto** (engranaje) → pestaña **Cloud Messaging**.
2. **Certificados de Web Push** → **Generar par de claves**.
3. Copiar la **clave pública** (empieza con `BK...`): el frontend la usa en
   `getToken({ vapidKey })` y en `applicationServerKey`.

### 1.4 Registrar la app Android

1. **Configuración** → *Tus apps* → icono **Android**.
2. **Nombre del paquete**: confirmarlo con quien tenga la app (ej. `co.siftc.app`).
3. **SHA-1** (recomendado; se obtiene de la huella del keystore de depuración):

   ```powershell
   keytool -list -v -keystore "$env:USERPROFILE\.android\debug.keystore" -alias androiddebugkey -storepass android | findstr /i SHA
   ```

4. **Registrar app** → descargar **`google-services.json`** → va en `android/app/`.
   Si el repo de la app es público, añadirlo a `.gitignore` y entregarlo por otro canal.

### 1.5 Habilitar Cloud Messaging v1

1. **Configuración** → pestaña **Cloud Messaging**.
2. Verificar que la **API de Cloud Messaging (V1)** esté habilitada. Si aparece
   deshabilitada, el botón lleva a Google Cloud a habilitar *Firebase Cloud Messaging API*.
3. Ignorar la sección *Cloud Messaging API (Legacy)*: está retirada.

### 1.6 Cuenta de servicio para el backend ⚠️ SECRETO

1. **Configuración** → pestaña **Cuentas de servicio** → **Firebase Admin SDK**.
2. **Generar nueva clave privada** → confirma → se descarga un JSON
   (`siftc-notificaciones-<id>.json`).
3. **Nunca subirlo a git.** En Render se guarda como variable de entorno
   `Firebase__CredentialsJson` con el **contenido completo del JSON en una sola línea**.

### 1.7 Probar el envío

- Consola → **Cloud Messaging** → *Enviar mensaje de prueba* pegando un token de prueba, o
- desde el backend con el snippet de la sección 3 usando un token real del service worker.

---

## 2. Frontend (PWA)

### 2.0 Configuración real del proyecto (ya creada)

```ts
// firebaseConfig — proyecto: siftc-notificaciones
export const firebaseConfig = {
  apiKey: "AIzaSyBFmcJOVDM1mdKCnMi69k4tb-VJYJghU2Y",
  authDomain: "siftc-notificaciones.firebaseapp.com",
  projectId: "siftc-notificaciones",
  storageBucket: "siftc-notificaciones.firebasestorage.app",
  messagingSenderId: "768771204385",
  appId: "1:768771204385:web:c05f6b413b6af674404a60"
};
```

> `apiKey` es **público por diseño** (no es un secreto): va en el código del frontend.
> Lo que sí es secreto: la cuenta de servicio (§1.6) y la clave privada VAPID.

```bash
npm install firebase
```

```ts
import { initializeApp } from "firebase/app";
import { getMessaging, getToken, onMessage } from "firebase/messaging";

const app = initializeApp(firebaseConfig);           // de la consola (1.2)
const messaging = getMessaging(app);

// Pide permiso y registra el token en el backend
const token = await getToken(messaging, { vapidKey: VAPID_PUBLIC_KEY }); // de (1.3)
await fetch("/api/v1/notificaciones/tokens", {
  method: "POST",
  headers: { "Content-Type": "application/json", Authorization: `Bearer ${jwt}` },
  body: JSON.stringify({ plataforma: "web", token }),
});

onMessage(messaging, (payload) => { /* aviso en pantalla */ });
```

Requisito: un **service worker** (`firebase-messaging-sw.js`) registrado en la raíz del
sitio; sin él `getToken()` falla en navegadores.

---

## 3. Backend .NET (implementado)

Paquetes NuGet en `SIFTC.Infrastructure`: **`FirebaseAdmin 3.7.1`** y **`MailKit 5.0.0`**.

| Pieza | Archivo | Rol |
|---|---|---|
| Dominio | `SIFTC.Domain/Entities/NotificacionRegla.cs`, `NotificacionToken.cs`, `NotificacionEnvio.cs` | Validaciones y transiciones (`pendiente → enviado/fallido`) |
| Generador | `SIFTC.Application/UseCases/Notificaciones/GeneradorNotificaciones.cs` | Evalúa reglas y encola filas (CA2) |
| Procesador | `SIFTC.Application/UseCases/Notificaciones/ProcesadorEnviosNotificacion.cs` | Envía, reintenta y audita (CA1/CA3) |
| Trigger de lote | `SIFTC.Application/UseCases/Notificaciones/EventHandlers/NotificacionAlPublicarLoteHandler.cs` | Encola al publicar un lote |
| Repositorio | `SIFTC.Infrastructure/Persistence/Repositories/NotificacionRepository.cs` | Cola, tokens y variaciones de IC del lote |
| Correo | `SIFTC.Infrastructure/Notifications/SmtpCorreoNotificador.cs` | SMTP con MailKit |
| Push | `SIFTC.Infrastructure/Notifications/FirebasePushNotificador.cs` | FCM v1 (cuenta de servicio) |
| Worker | `SIFTC.API/Notifications/NotificacionesWorker.cs` | `BackgroundService` con `PeriodicTimer` |

Notas de implementación:

- `FirebaseApp` se crea una sola vez con nombre `siftc-notificaciones`; si
  `Firebase__CredentialsJson` falta, se registra un warning y la API sigue arrancando.
- El worker toma pendientes cuyo `fecha_ultimo_intento` sea nulo o haya vencido
  (`ReintentoSegundos`) y los procesa en lotes de `LoteMaximo`.
- El envío de correo usa `SecureSocketOptions.StartTls` cuando `Smtp__UseStartTls=true`.
- Un token FCM `UNREGISTERED` se desactiva y no se reintenta; los demás errores suman
  intentos.
- El texto del aviso se arma con el nombre de la entidad y la variación porcentual.

Manejo de errores para CA3:

| Resultado | Acción |
|---|---|
| `UNREGISTERED` | `notificaciones_tokens.activo = false`, no reintentar |
| Error transitorio (SMTP/FCM) | `intentos + 1`, `ultimo_error`, nuevo intento hasta `max_intentos` |
| Se agotan los intentos | `estado = 'fallido'` |
| Éxito | `estado = 'enviado'`, `fecha_envio = now()` |

Cada intento deja un evento en `seguridad.auditorias` con
`accion = notificacion.enviada | notificacion.reintento | notificacion.fallida`
(no se usa una sola acción `notificacion`, así se distingue el resultado). Un fallo al
escribir la auditoría **no** revierte ni reintenta el envío: solo se registra en el log.

---

## 4. Variables de entorno (Render)

| Variable | Ejemplo / nota |
|---|---|
| `Smtp__Host` | `smtp.gmail.com` (o el SMTP de la institución) |
| `Smtp__Port` | `587` |
| `Smtp__User` | remitente autenticado |
| `Smtp__Password` | contraseña de aplicación (no la de la cuenta) |
| `Smtp__From` | `notificaciones@siftc.local` |
| `Smtp__UseStartTls` | `true` (puerto 587); `false` para SSL directo |
| `Firebase__CredentialsJson` | **contenido JSON** de 1.6 en una línea (secreto) |
| `Notificaciones__IntervaloSegundos` | `10` (worker; CA1 exige ≤ 60 s) |
| `Notificaciones__ReintentoSegundos` | `30` (espera antes de reintentar) |
| `Notificaciones__LoteMaximo` | `50` (envíos por pasada) |

En local se pueden definir con `dotnet user-secrets` o variables de sesión, por ejemplo:

```powershell
$env:Notificaciones__IntervaloSegundos = '10'
$env:Notificaciones__ReintentoSegundos = '30'
dotnet run --project SIFTC.API --launch-profile http
```

No se versiona ningún secreto: `SIFTC.API/appsettings.json` **no** debe llevar SMTP ni
Firebase.

---

## 5. Contrato API

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/notificaciones/reglas` | Reglas activas propias (CA2) |
| `POST` | `/api/v1/notificaciones/reglas` | Crea regla: `{ canal, idEntidad?, umbral? }` |
| `DELETE` | `/api/v1/notificaciones/reglas/{id}` | Desactiva regla propia |
| `POST` | `/api/v1/notificaciones/tokens` | Registra token FCM `{ plataforma, token }` |
| `DELETE` | `/api/v1/notificaciones/tokens/{token}` | Da de baja un token propio |
| `GET` | `/api/v1/notificaciones/envios?estado=&limite=` | Cola/log de envíos (CA1/CA3) |
| `POST` | `/api/v1/notificaciones/eventos` | Evento relevante del ETL; encola avisos (Admin/Analista) |

Reglas de negocio:
- `idEntidad` ausente = todas las entidades; `umbral` ausente = cualquier cambio
  (se compara el valor absoluto de la variación).
- `canal = 'ambos'` genera **dos filas** en `notificaciones_envios` (una por canal).
- Regla repetida (mismo usuario, canal y entidad) → `409`; si estaba desactivada se
  reactiva con el umbral nuevo.
- El Administrador ve todos los envíos; los demás roles solo los propios.
- Cada intento deja un evento en `seguridad.auditorias` con
  `accion = notificacion.enviada | notificacion.reintento | notificacion.fallida`.
- El token FCM nunca se devuelve en las respuestas.

---

## 6. Relación con lo existente

- `analitica.notificaciones_aprobacion` (Script-19) es la bandeja de **rechazos de lotes**;
  esta HU no la modifica. El disparador real es `LotePublicadoEvent` (publicación de lote):
  `NotificacionAlPublicarLoteHandler` calcula la variación de IC por entidad del lote y
  encola los avisos. El ETL también puede llamar `POST /api/v1/notificaciones/eventos`.
- La bitácora `seguridad.auditorias` (Script-22) se reutiliza como registro de auditoría
  del envío; `notificaciones_envios` guarda el detalle operativo (intentos/errores).

---

## 7. Checklist de verificación (evidencia)

**Automatizado contra Neon:** `C:\Users\jr10p\AppData\Local\Temp\opencode\verificar_notificaciones.ps1`
(API en `http://localhost:5057`, `Notificaciones__IntervaloSegundos=3`,
`Notificaciones__ReintentoSegundos=5`). Resultado de la última corrida: **24/24 PASS**.

**CA1 – < 1 minuto**
1. Crear regla `correo`/`push` y disparar un evento → `POST /eventos` responde
   `{"encolados": 2}`.
2. El worker intenta el envío en la siguiente pasada; medido en **7 s** con intervalo de
   3 s (con el default de 10 s queda muy por debajo de 60 s).
3. `GET /notificaciones/envios` trae `fechaCreacion`, `fechaUltimoIntento` y `fechaEnvio`.
4. Entrega real: **pendiente de credenciales** (correo en bandeja y push en dispositivo).

**CA2 – reglas**
1. `POST /reglas` con `canal`, `idEntidad` y `umbral` → `200`.
2. Evento de otra entidad → `encolados = 0`; evento sobre la entidad y por encima del
   umbral → encola.
3. `canal='ambos'` → 2 filas (correo y push).
4. Regla duplicada → `409`; canal inválido o umbral negativo → `422`.
5. Cada usuario solo ve y desactiva sus reglas; la de otro → `404`.

**CA3 – reintentos y fallos**
1. Sin SMTP/Firebase configurados, `ultimoError` queda con
   `SMTP no configurado...` / `Firebase no configurado...`.
2. Tras 3 intentos la fila queda `estado='fallido'`, `intentos=3`, `maxIntentos=3`.
3. `seguridad.auditorias` contiene los eventos `notificacion.reintento` /
   `notificacion.fallida` de los intentos.
4. Token FCM inválido (`UNREGISTERED`) → el token se desactiva y no se reintenta.

> Para cerrar el CA1 al 100 % falta solo aportar `Smtp__*` y `Firebase__CredentialsJson`
> reales y repetir los pasos 3 y 4 de CA1 con un correo y un dispositivo reales.
