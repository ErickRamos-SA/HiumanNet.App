// ---------------------------------------------------------------------------
// Utilidades del portal HuimanNet que requieren el navegador.
//
// Carga directa: el archivo NUNCA pasa por el servidor de aplicación. El
// navegador lo sube a la URL firmada que emitió el backend tras autorizar la
// operación: Blob Storage con SAS en Azure, o el almacén local firmado en
// desarrollo; el código es el mismo en ambos casos (ARQUITECTURA.md §3.3).
// El hash SHA-256 se calcula aquí con WebCrypto y viaja en la confirmación,
// para que el servidor pueda verificar la integridad de lo subido.
// ---------------------------------------------------------------------------
window.huimanNet = window.huimanNet || {};

window.huimanNet.leerArchivoSeleccionado = function (idInput) {
    const input = document.getElementById(idInput);
    if (!input || !input.files || input.files.length === 0) {
        return null;
    }

    const archivo = input.files[0];
    return { nombre: archivo.name, tamano: archivo.size };
};

window.huimanNet.subirArchivo = async function (idInput, urlFirmada, encabezadoTipoBlob) {
    const input = document.getElementById(idInput);
    if (!input || !input.files || input.files.length === 0) {
        return { exito: false, mensaje: 'No hay ningún archivo seleccionado.' };
    }

    const archivo = input.files[0];

    try {
        const buffer = await archivo.arrayBuffer();

        const respuesta = await fetch(urlFirmada, {
            method: 'PUT',
            headers: {
                'x-ms-blob-type': encabezadoTipoBlob || 'BlockBlob',
                'Content-Type': archivo.type || 'application/octet-stream'
            },
            body: buffer
        });

        if (!respuesta.ok) {
            return { exito: false, mensaje: `El almacenamiento rechazó la carga (HTTP ${respuesta.status}).` };
        }

        const digest = await crypto.subtle.digest('SHA-256', buffer);
        const huella = Array.from(new Uint8Array(digest))
            .map(b => b.toString(16).padStart(2, '0'))
            .join('');

        input.value = '';
        return { exito: true, huella: huella, tamano: archivo.size };
    } catch (error) {
        return { exito: false, mensaje: `No se pudo subir el archivo: ${error.message}` };
    }
};

window.huimanNet.descargar = function (url) {
    // Navegación directa a la URL firmada de lectura: la descarga tampoco
    // atraviesa el servidor de aplicación.
    window.location.assign(url);
};

window.huimanNet.descargarFlujo = async function (nombre, tipo, referencia) {
    // Contenido generado por el servidor (exportaciones): llega como flujo por
    // el circuito y se entrega al usuario como archivo.
    const buffer = await referencia.arrayBuffer();
    const blob = new Blob([buffer], { type: tipo || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    setTimeout(() => URL.revokeObjectURL(url), 2000);
};

window.huimanNet.guardarIdioma = function (codigo) {
    document.cookie = `huimannet.idioma=${encodeURIComponent(codigo)}; path=/; max-age=31536000; samesite=lax; secure`;
};
