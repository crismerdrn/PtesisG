function mostrarExito(mensaje) {

    Swal.fire({

        icon: 'success',

        title: '¡Operación exitosa!',

        text: mensaje,

        confirmButtonColor: '#198754',

        confirmButtonText: 'Aceptar',

        timer: 2500,

        timerProgressBar: true

    });

}

function mostrarError(mensaje) {

    Swal.fire({

        icon: 'error',

        title: 'Error',

        text: mensaje,

        confirmButtonColor: '#dc3545'

    });

}

function confirmarEliminacion(nombre, callback) {

    Swal.fire({

        title: '¿Eliminar registro?',

        html:
            'Se eliminará:<br><strong>' +
            nombre +
            '</strong><br><br>Esta acción no se puede deshacer.',

        icon: 'warning',

        showCancelButton: true,

        confirmButtonText: 'Sí, eliminar',

        cancelButtonText: 'Cancelar',

        confirmButtonColor: '#dc3545',

        cancelButtonColor: '#6c757d',

        reverseButtons: true

    }).then((result) => {

        if (result.isConfirmed) {

            callback();

        }

    });

}

function confirmarAccion(titulo, mensajeHtml, botonTexto, botonColor, callback) {
    Swal.fire({
        title: titulo,
        html: mensajeHtml,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: botonTexto || 'Sí, continuar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: botonColor || '#198754',
        cancelButtonColor: '#6c757d',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            callback();
        }
    });
}