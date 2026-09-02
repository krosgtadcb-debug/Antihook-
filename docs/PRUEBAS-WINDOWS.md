# Pruebas de Antihook v1.0 en Windows

## Requisitos

Compilar en Visual Studio 2022 con la carga de trabajo **Desarrollo de escritorio .NET** y .NET Framework 4.7.2 Developer Pack. La base de datos debe ser MariaDB/MySQL compatible con el esquema existente.

## Preparación de base de datos

Antes de aplicar cambios, realizar un respaldo. Ejecutar `sql/v1_admin_security.sql` sobre la base `bf`. Para conceder administración a un usuario existente, insertar su `user_id` en `antihook_roles` con `role_name = 'admin'`, o usar temporalmente el campo legacy `devTeam` distinto de cero.

## Compilación

Abrir `BF3AntiHook.sln`, restaurar los paquetes NuGet y compilar `Debug` para `Any CPU`. Revisar especialmente que `Newtonsoft.Json.dll` tenga una ruta válida en ambos `.csproj`; las rutas absolutas del repositorio original pueden requerir corrección local.

## Prueba del servidor

Iniciar la aplicación de servidor, seleccionar un puerto libre, por ejemplo `4040`, y pulsar el botón de inicio. En Windows, el listener `HttpListener` puede requerir reservar la URL ACL para el usuario que ejecuta la aplicación. La ruta WebSocket esperada es `ws://HOST:4040/ws/`.

## Prueba del cliente

Iniciar el cliente, introducir host, usuario y contraseña, y pulsar Login. Una respuesta `auth.ok` debe abrir GameHub. Entrar a Battlefield 3 y comprobar que los servidores aparecen sin solicitar polling manual. El servidor debe emitir `servers.updated` aproximadamente cada dos segundos mientras exista una suscripción activa.

## Prueba administrativa

Autenticar con una cuenta cuyo rol sea `admin`. Abrir Administración y comprobar que se muestran `Nombre`, `HWID`, `IP` y `LongIP`. En cada acción, comprobar que se solicita motivo. Verificar que una cuenta sin rol recibe `Permisos insuficientes`. Para `Banear`, comprobar el registro en `antihook_audit_events`, el registro en `antihook_bans`, el cierre de la sesión objetivo y la notificación audible o visible en las sesiones autorizadas.

## Prueba del anticheat defensivo

Iniciar Battlefield 3 con una lista de módulos aprobados. El componente debe emitir hallazgos informativos ante un módulo no esperado o acceso denegado, sin terminar procesos, elevar privilegios, inyectar código ni acceder a memoria arbitraria. Una coincidencia aislada debe quedar como señal para revisión y no como baneo automático.

## Limitación conocida

El entorno Linux usado para preparar esta rama no incluye MSBuild ni el Developer Pack de .NET Framework, por lo que la validación final debe completarse en Windows. La rama de trabajo es `feature/version-1.0-foundation`.
