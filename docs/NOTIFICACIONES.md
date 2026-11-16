# Notificaciones (HU-AD-10 + HU-MO-05)

Correo por **SMTP** y push por **Firebase Cloud Messaging (FCM v1)**, con reglas por
usuario y reintentos registrados.

| CA | Requisito | Dónde se cumple |
|---|---|---|
| **CA1** | Envío por correo y push en menos de 1 minuto | Worker en background con intervalo corto + prioridad alta en FCM + `fecha_creacion → fecha_envio` en `seguridad.notificaciones_envios` |
| **CA2** | Reglas configurables por usuario (canal, entidades y umbral) | Tabla `seguridad.notificaciones_reglas` + CRUD en la API |
| **CA3** | Reintentos y fallos registrados | `estado`, `intentos`, `max_intentos`, `ultimo_error` en `seguridad.notificaciones_envios` (+ evento en `seguridad.auditorias` con `accion=notificacion`) |

Base de datos: **`database/Script-23-notificaciones-usuario.sql`** (idempotente, ejecutar
como dueño de los objetos, después de Script-22).

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

## 3. Backend .NET

Paquete NuGet: **`FirebaseAdmin`** (Firebase Admin SDK for .NET).

```csharp
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

// Arranque de la API/worker (una sola vez)
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromJson(
        Environment.GetEnvironmentVariable("Firebase__CredentialsJson"))
});

// Envío (CA1: debe ser < 1 minuto desde el evento)
await FirebaseMessaging.DefaultInstance.SendAsync(new Message
{
    Token = token,
    Notification = new Notification { Title = asunto, Body = cuerpo },
    Android = new AndroidConfig { Priority = Priority.High },
});
```

Manejo de errores para CA3:

| Resultado FCM | Acción |
|---|---|
| `UNREGISTERED` | Token inválido: marcar `notificaciones_tokens.activo = false`, no reintentar |
| `UNAVAILABLE`, `INTERNAL`, `UNAUTHENTICATED` transitorio | `intentos + 1`, `ultimo_error`, nuevo intento (máx. `max_intentos`) |
| Éxito | `estado = 'enviado'`, `fecha_envio = now()` |

Para correo: cualquier `System.Net.Mail.SmtpClient`/MailKit con la configuración SMTP de
la tabla de variables; mismo tratamiento de reintentos (el `canal` del envío es
`correo` o `push`).

---

## 4. Variables de entorno (Render)

| Variable | Ejemplo / nota |
|---|---|
| `Smtp__Host` | `smtp.gmail.com` (o el SMTP de la institución) |
| `Smtp__Port` | `587` |
| `Smtp__User` | remitente autenticado |
| `Smtp__Password` | contraseña de aplicación (no la de la cuenta) |
| `Smtp__From` | `notificaciones@siftc.local` |
| `Firebase__CredentialsJson` | **contenido JSON** de 1.6 en una línea (secreto) |
| `Notificaciones__IntervaloSegundos` | `10` (worker: pendientes vencidos) |

---

## 5. Contrato API propuesto

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/notificaciones/reglas` | Reglas propias del usuario (CA2) |
| `POST` | `/api/v1/notificaciones/reglas` | Crea regla: `{ canal, idEntidad?, umbral? }` |
| `DELETE` | `/api/v1/notificaciones/reglas/{id}` | Desactiva/borra regla propia |
| `POST` | `/api/v1/notificaciones/tokens` | Registra token FCM `{ plataforma, token }` |
| `DELETE` | `/api/v1/notificaciones/tokens/{token}` | Da de baja un token |
| `GET` | `/api/v1/notificaciones/envios?estado=` | Consulta de la cola/log (CA3) |

Reglas de negocio:
- `idEntidad` ausente = todas las entidades; `umbral` ausente = cualquier cambio.
- `canal = 'ambos'` genera **dos filas** en `notificaciones_envios` (una por canal).
- Cada envío se registra además en `seguridad.auditorias` con `accion=notificacion`
  (mismo formato `key=value` de Script-22; política estricta: si falla el registro → 503).

---

## 6. Relación con lo existente

- `analitica.notificaciones_aprobacion` (Script-19) es la bandeja de **rechazos de lotes**;
  el worker de esta HU puede consumirla y traducirla en filas de
  `seguridad.notificaciones_envios`. No se duplica ni se modifica ese flujo.
- La bitácora `seguridad.auditorias` (Script-22) se reutiliza como registro de auditoría
  del envío; `notificaciones_envios` guarda el detalle operativo (intentos/errores).

---

## 7. Checklist de verificación

**CA1 – < 1 minuto**
1. Disparar un evento (ej. rechazo de lote o cambio que cumpla una regla).
2. Medir `fecha_evento` (auditoría) vs `fecha_envio` (`notificaciones_envios`) → < 60 s.
3. Confirmar recepción: correo en la bandeja y push en el dispositivo.

**CA2 – reglas**
1. Crear regla por API: `canal='push'`, `idEntidad=<una>`, `umbral=2`.
2. Cambio menor en otra entidad → no llega nada; cambio > 2 % en la entidad → llega.
3. Cambiar `canal='correo'` → llega solo por correo.

**CA3 – reintentos y fallos**
1. Apuntar SMTP a un host inválido → fila en `notificaciones_envios` con
   `estado='pendiente'`, `intentos=1`, `ultimo_error` poblado.
2. Dejar agotar `max_intentos` → `estado='fallido'`.
3. Verificar que cada envío quedó también en `seguridad.auditorias` (`accion=notificacion`).
