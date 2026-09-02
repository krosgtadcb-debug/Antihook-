# BF3 Antihook — Versión 1.0

**Desarrollador:** OxyMonster  
**Objetivo:** modernizar el cliente y servidor de administración de Battlefield 3 con una interfaz clara, autenticación, GameHub y comunicación persistente por WebSocket.

## Estado actual auditado

El repositorio contiene dos aplicaciones WinForms para .NET Framework: un servidor y un cliente. La comunicación existente usa `Socket` TCP con mensajes JSON sin un framing robusto; el cliente solicita la lista de servidores de forma manual; el servidor mantiene tokens en memoria y el almacenamiento de usuarios depende de MySQL. El cliente también contiene lógica de supervisión local muy agresiva y frágil: compara nombres de procesos, inspecciona módulos por conteo y termina procesos mediante `cmd.exe`.

## Arquitectura objetivo

| Componente | Responsabilidad | Regla de seguridad |
|---|---|---|
| LoginForm | Solicitar usuario, contraseña y dirección del servidor | No guardar contraseñas; mostrar errores genéricos |
| GameHubForm | Mostrar los juegos disponibles y estado de sesión | Solo mostrar módulos autorizados por el servidor |
| Battlefield3Form | Listar servidores, filtrar y conectarse | Recibir actualizaciones push cada 2 segundos |
| AdminUsersForm | Mostrar usuarios conectados y acciones autorizadas | Roles, confirmación, auditoría y rate limit |
| WebSocket transport | Sesión persistente y mensajes JSON | TLS en producción, tamaño máximo, heartbeat y cancelación |
| AntiCheat client | Recopilar señales mínimas del proceso del juego | Transparente, opt-in/consentimiento, mínimos privilegios |
| AntiHook server | Autenticar, registrar y aplicar políticas | No confiar en datos enviados por el cliente |

## Flujo de navegación

1. `LoginForm` valida los campos y abre una conexión WebSocket.
2. Si el servidor responde `auth.ok`, se abre `GameHubForm` y se conserva una sesión efímera.
3. `GameHubForm` presenta inicialmente el módulo `Battlefield 3`.
4. `Battlefield3Form` recibe `servers.snapshot` al entrar y `servers.updated` cada dos segundos.
5. El módulo administrativo solo aparece si la sesión tiene el rol correspondiente.

## Protocolo WebSocket v1

Cada mensaje será un objeto JSON con `type`, `requestId`, `timestamp`, `sessionId` y `payload`. El servidor debe validar el tipo, longitud y esquema antes de procesarlo.

| Tipo | Dirección | Uso |
|---|---|---|
| `auth.login` | Cliente → servidor | Autenticación inicial |
| `auth.ok` / `auth.error` | Servidor → cliente | Resultado de autenticación |
| `servers.subscribe` | Cliente → servidor | Suscripción a Battlefield 3 |
| `servers.updated` | Servidor → cliente | Snapshot periódico cada 2 segundos |
| `admin.users.list` | Admin → servidor | Solicitud de usuarios conectados |
| `admin.user.action` | Admin → servidor | Acción administrativa confirmada |
| `admin.event` | Servidor → clientes autorizados | Evento de ban, expulsión o auditoría |
| `speech.notification` | Servidor → cliente | Notificación audible autorizada |
| `ping` / `pong` | Ambos sentidos | Mantener viva la sesión |

## Datos de usuario

La vista administrativa podrá mostrar `Nombre`, `HWID`, `IP` y `LongIP` únicamente a operadores autorizados. Los valores deben enmascararse en logs y protegerse en tránsito. El HWID no debe ser una contraseña ni un identificador reutilizable para otros fines.

## Acciones administrativas

Las acciones `Captura de pantalla`, `Capturar procesos`, `Capturar módulos` y `Enviar Speech` requieren consentimiento informado, un indicador visible en el cliente, autorización por rol, registro de auditoría y límites de frecuencia. La captura debe limitarse al contexto de la sesión de juego y no habilitar vigilancia oculta. `Banear` y `Expulsar` requieren confirmación explícita y motivo obligatorio.

La notificación de ban se normalizará como:

> `UsuarioBaneado ha sido baneado por proceso sospechoso.`

El servidor enviará el evento a los clientes conectados que estén autorizados a recibirlo; no se confiará en el texto recibido desde un cliente.

## Anticheat defensivo

La versión 1.0 no incluirá drivers no firmados, rootkits, técnicas de ocultación, inyección de código, lectura arbitraria de memoria ni mecanismos para evadir antivirus o controles del sistema. La detección se basará en señales documentadas y verificables del proceso del juego, integridad de archivos, versión del cliente y telemetría mínima. Las señales deben producir una revisión o una política graduada, nunca un baneo automático por una sola coincidencia de nombre.

## Orden de implementación

Primero se crearán los contratos de mensajes y el transporte WebSocket con framing y cancelación. Después se sustituirá el flujo de login, se añadirá `GameHubForm` y se conectará el listado de Battlefield 3 a actualizaciones push. A continuación se incorporará la vista de usuarios y las acciones administrativas auditables. Finalmente se reemplazará la supervisión local actual por un módulo defensivo y configurable, y se ejecutarán compilación, pruebas de protocolo y revisión de regresiones.
