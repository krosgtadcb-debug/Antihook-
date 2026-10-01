<?php
/**
 * Rust Server Helper - PHP 7 compatible, sin dependencias externas.
 */

function h($value)
{
    return htmlspecialchars((string) $value, ENT_QUOTES, 'UTF-8');
}

function valid_host($host)
{
    if ($host === '' || strlen($host) > 253 || preg_match('/[\s\/:?#]/', $host)) {
        return false;
    }

    if (filter_var($host, FILTER_VALIDATE_IP)) {
        return true;
    }

    return (bool) preg_match('/^(?=.{1,253}$)(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)*[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$/', $host);
}

$host = isset($_POST['host']) ? trim($_POST['host']) : '127.0.0.1';
$port = isset($_POST['port']) ? trim($_POST['port']) : '28015';
$error = '';
$result = null;

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $portNumber = filter_var($port, FILTER_VALIDATE_INT, array('options' => array('min_range' => 1, 'max_range' => 65535)));

    if (!valid_host($host)) {
        $error = 'Introduce una IP o nombre de host válido, sin http:// ni rutas.';
    } elseif ($portNumber === false) {
        $error = 'El puerto debe ser un número entre 1 y 65535.';
    } else {
        $port = (string) $portNumber;
        $address = $host . ':' . $port;
        $result = array(
            'address' => $address,
            'console' => 'client.connect ' . $address,
            'steam' => 'steam://connect/' . $address,
        );
    }
}

header('X-Content-Type-Options: nosniff');
header('X-Frame-Options: DENY');
header('Referrer-Policy: no-referrer');
?>
<!doctype html>
<html lang="es">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Rust Server Helper</title>
    <style>
        :root { color-scheme: dark; font-family: system-ui, -apple-system, sans-serif; }
        body { margin: 0; background: #101216; color: #edf0f2; }
        main { max-width: 760px; margin: 6rem auto; padding: 0 1.25rem; }
        .card { background: #1b1f26; border: 1px solid #303743; border-radius: 14px; padding: 2rem; box-shadow: 0 18px 50px #0005; }
        h1 { margin-top: 0; font-size: 2rem; } h2 { font-size: 1.2rem; margin-bottom: .5rem; }
        p, li { color: #b9c1cb; line-height: 1.55; }
        label { display: block; margin: 1rem 0 .4rem; font-weight: 700; }
        input { width: 100%; box-sizing: border-box; padding: .75rem; border-radius: 8px; border: 1px solid #46515e; background: #11151a; color: #fff; font-size: 1rem; }
        button { margin-top: 1.25rem; padding: .75rem 1rem; border: 0; border-radius: 8px; background: #e66a32; color: #fff; font-weight: 700; cursor: pointer; }
        .error { background: #48252a; border: 1px solid #a34c57; padding: .8rem; border-radius: 8px; }
        .success { margin-top: 1.5rem; background: #172e2a; border: 1px solid #327a69; padding: 1rem; border-radius: 8px; }
        code { display: block; padding: .75rem; margin: .5rem 0; background: #0b0d10; border-radius: 6px; color: #f5c78e; overflow-wrap: anywhere; }
        small { color: #8f9aa8; }
    </style>
</head>
<body>
<main>
    <section class="card">
        <h1>Rust Server Helper</h1>
        <p>Genera la instrucción oficial para conectarte directamente a un servidor Rust. No modifica la lista <em>Official Servers</em>.</p>

        <?php if ($error !== ''): ?><div class="error" role="alert"><?php echo h($error); ?></div><?php endif; ?>

        <form method="post" action="">
            <label for="host">IP o nombre de host</label>
            <input id="host" name="host" value="<?php echo h($host); ?>" maxlength="253" required>
            <label for="port">Puerto</label>
            <input id="port" name="port" value="<?php echo h($port); ?>" inputmode="numeric" pattern="[0-9]{1,5}" required>
            <button type="submit">Generar conexión</button>
        </form>

        <?php if ($result !== null): ?>
            <div class="success" aria-live="polite">
                <h2>Conexión preparada: <?php echo h($result['address']); ?></h2>
                <p>En Rust pulsa <strong>F1</strong> y pega:</p>
                <code><?php echo h($result['console']); ?></code>
                <p>También puedes probar este enlace desde Steam:</p>
                <code><?php echo h($result['steam']); ?></code>
                <small>El enlace Steam depende del sistema y de la asociación de protocolos del cliente.</small>
            </div>
        <?php endif; ?>

        <h2>Qué sí y qué no hace esta herramienta</h2>
        <ul>
            <li>Sí: prepara <code>client.connect IP:PUERTO</code> para conexión directa.</li>
            <li>Sí: valida entradas para evitar URLs, rutas y puertos inválidos.</li>
            <li>No: puede insertar servidores en <em>Official Servers</em>; esa lista la controla Facepunch.</li>
            <li>No: sustituye una IP pública, el reenvío de puertos o la configuración del servidor dedicado.</li>
        </ul>
    </section>
</main>
</body>
</html>
