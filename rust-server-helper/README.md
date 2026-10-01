# Rust Server Helper (PHP 7)

Módulo independiente del repositorio **Antihook-** para preparar la conexión directa a un servidor Rust.

## Alcance

La lista **Official Servers** la controla Facepunch. Ninguna página PHP, JSON, archivo `hosts` u plugin de Oxide puede insertar un servidor local en esa pestaña. Este helper hace lo que sí es posible: validar una dirección y generar el comando oficial de consola.

Para un servidor local, la salida esperada es:

```text
client.connect 127.0.0.1:28015
```

Para que otras personas se conecten se necesita una IP pública o dominio, puerto UDP reenviado y un servidor dedicado correctamente configurado. En condiciones normales aparecerá en **Community**, no en **Official**.

## Requisitos

- PHP 7.0 o superior (compatible con PHP 7.x).
- Servidor web Apache/Nginx o el servidor integrado de PHP.
- No requiere Composer, base de datos ni extensiones externas.

## Ejecutar en local

Desde este directorio:

```bash
php -S 127.0.0.1:8080 -t public
```

Abre <http://127.0.0.1:8080> y usa `127.0.0.1` y `28015`.

## Seguridad y diseño

- Se escapa toda salida HTML con `htmlspecialchars`.
- Se validan host y puerto antes de crear comandos.
- Se envían cabeceras básicas contra framing, MIME sniffing y referrer leakage.
- La aplicación no ejecuta comandos del sistema, no consulta APIs privadas de Facepunch y no solicita credenciales.

## Estructura

```text
rust-server-helper/
├── public/
│   └── index.php
└── README.md
```
