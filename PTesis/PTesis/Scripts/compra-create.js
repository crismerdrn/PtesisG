console.log("--> INICIANDO SCRIPT DE COMPRA <--");

let detalles = [];
let filtroTipo = 'TODOS';
let resultadosActuales = [];

$(document).ready(function () {
    console.log("Script cargado sin errores de sintaxis.");

    if (!$('#FechaCompra').val()) {
        $('#FechaCompra').val(new Date().toISOString().split('T')[0]);
    }

    // Quitar borde rojo y advertencia al seleccionar proveedor
    $('#ProveedorSelect').on('change', function () {
        if ($(this).val()) {
            $(this).removeClass('is-invalid'); $(this).siblings('.invalid-feedback, .text-danger').hide();
        }
    });

    // Filtro de tipo
    $('.dropdown-item[data-tipo]').click(function (e) {
        e.preventDefault();
        filtroTipo = $(this).data('tipo'); $('#btnFiltroTipo').html('<i class="fas fa-filter"></i> ' + $(this).text()); $(this).closest('.dropdown-menu').find('.dropdown-item').removeClass('active');
        $(this).addClass('active'); $('#buscadorItem').focus();
    });

    // Evento de búsqueda
    $('#buscadorItem').on('input keyup', function () {
        const termino = $(this).val().trim();
        if (termino.length >= 2) {
            buscarItems(termino);
        } else {
            $('#resultadosBusqueda').hide().empty();
        }
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('#buscadorItem, #resultadosBusqueda').length) {
            $('#resultadosBusqueda').hide();
        }
    });
});

function buscarItems(termino) {
    $.post(COMPRA_CONFIG.urlBuscarProductos, { term: termino }, function (productos) {
        $.post(COMPRA_CONFIG.urlBuscarInsumos, { term: termino }, function (insumos) {
            let resultados = [];

            if (filtroTipo === 'TODOS' || filtroTipo === 'PRODUCTO') {
                resultados = resultados.concat(productos || []);
            }
            if (filtroTipo === 'TODOS' || filtroTipo === 'INSUMO') {
                resultados = resultados.concat(insumos || []);
            }

            mostrarResultados(resultados);
        });
    });
}

function mostrarResultados(items) {
    resultadosActuales = items || [];
    const $contenedor = $('#resultadosBusqueda');
    $contenedor.empty();

    if (resultadosActuales.length === 0) {
        $contenedor.append('<div class="list-group-item text-muted p-2"><i class="fas fa-search me-2"></i>No se encontraron resultados</div>');
    } else {
        resultadosActuales.forEach(function (item, index) {
            const badgeColor = item.tipo === 'PRODUCTO' ? 'bg-info' : 'bg-warning text-dark';

            $contenedor.append(`
                <a href="javascript:void(0)" class="list-group-item list-group-item-action p-2" onclick="seleccionarResultado(${index})">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <span class="badge ${badgeColor} me-2">${item.tipo}</span>
                            <strong>${item.nombre}</strong>
                        </div>
                        <div class="text-end">
                            <small class="text-muted">${item.medida}</small>
                        </div>
                    </div>
                    <small class="text-muted d-block mt-1">${item.descripcion || 'Sin descripción'}</small>
                </a>
            `);
        });
    }

    $contenedor.show();
}

function seleccionarResultado(index) {
    const item = resultadosActuales[index];
    if (!item) return;

    const existe = detalles.some(d => d.IdItemSeleccionado === item.id && d.TipoItem === item.tipo);
    if (existe) {
        mostrarError('Este ítem ya está agregado a la compra.');
        return;
    }

    $('#sinDetalles').hide();

    const detalle = {
        IdItemSeleccionado: item.id,
        TipoItem: item.tipo,
        nombre: item.nombre,
        descripcion: item.descripcion,
        medida: item.medida,
        abreviatura: item.abreviatura,
        cantidad: 1,
        precioUnitario: item.tipo === 'PRODUCTO' ? (item.precio || 0) : 0,
        subtotal: item.tipo === 'PRODUCTO' ? (item.precio || 0) : 0
    };

    detalles.push(detalle);
    agregarFilaDetalle(detalle, detalles.length - 1);
    actualizarTotal();

    $('#buscadorItem').val('');
    $('#resultadosBusqueda').hide().empty();
}

function agregarFilaDetalle(detalle, index) {
    const filaId = 'filaDetalle_' + index;
    const fila = `
        <tr id="${filaId}">
            <td>
                <strong>${detalle.nombre}</strong>
                <span class="badge ${detalle.TipoItem === 'PRODUCTO' ? 'bg-info' : 'bg-warning text-dark'} ms-1">${detalle.TipoItem}</span>
                <br />
                <small class="text-muted">${detalle.descripcion || 'Sin descripción'}</small>
            </td>
            <td>
                <input type="number" class="form-control form-control-sm cantidad sin-spinners"
                       min="0.01" step="0.01" value="${detalle.cantidad}"
                       data-index="${index}" onchange="actualizarSubtotal(${index})">
            </td>
            <td>
                <input type="number" class="form-control form-control-sm precio sin-spinners"
                       min="0" step="0.01" value="${parseFloat(detalle.precioUnitario).toFixed(2)}"
                       data-index="${index}" onchange="actualizarSubtotal(${index})">
            </td>
            <td class="subtotal text-end fw-bold">
                Bs. <span id="subtotal_${index}">${detalle.subtotal.toFixed(2)}</span>
            </td>
            <td>
                <span class="badge bg-light text-dark">${detalle.medida}</span>
            </td>
            <td class="text-center">
                <button type="button" class="btn btn-danger btn-sm" onclick="solicitarEliminacionDetalle(${index})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `;

    $('#cuerpoDetalles').append(fila);
}

function actualizarSubtotal(index) {
    const cantidad = parseFloat($('#filaDetalle_' + index + ' .cantidad').val()) || 0;
    const precio = parseFloat($('#filaDetalle_' + index + ' .precio').val()) || 0;
    const subtotal = cantidad * precio;

    detalles[index].cantidad = cantidad;
    detalles[index].precioUnitario = precio;
    detalles[index].subtotal = subtotal;

    $('#subtotal_' + index).text(subtotal.toFixed(2));
    actualizarTotal();
}

function solicitarEliminacionDetalle(index) {
    const item = detalles[index];
    const nombre = item ? item.nombre : 'este ítem';

    confirmarEliminacion(nombre, function () {
        detalles.splice(index, 1);
        $('#filaDetalle_' + index).remove();

        detalles.forEach((detalle, nuevoIndex) => {
            const filaExistente = $('#cuerpoDetalles tr:not(#sinDetalles)').eq(nuevoIndex);
            if (filaExistente.length) {
                filaExistente.attr('id', 'filaDetalle_' + nuevoIndex);
                filaExistente.find('.cantidad').attr('data-index', nuevoIndex).attr('onchange', 'actualizarSubtotal(' + nuevoIndex + ')');
                filaExistente.find('.precio').attr('data-index', nuevoIndex).attr('onchange', 'actualizarSubtotal(' + nuevoIndex + ')');
                filaExistente.find('.btn-danger').attr('onclick', 'solicitarEliminacionDetalle(' + nuevoIndex + ')');
            }
        });

        if (detalles.length === 0) {
            $('#sinDetalles').show();
        }

        actualizarTotal();
    });
}

function actualizarTotal() {
    const total = detalles.reduce((sum, d) => sum + d.subtotal, 0);
    $('#totalCompra').text('Bs. ' + total.toFixed(2));
    $('#MontoTotal').val(total.toFixed(2));
}

function limpiarFormulario() {
    Swal.fire({
        title: '¿Cancelar compra?',
        text: 'Se perderán todos los datos ingresados en el formulario.',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, cancelar',
        cancelButtonText: 'No, continuar',
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            detalles = [];
            $('#cuerpoDetalles tr:not(#sinDetalles)').remove();
            $('#sinDetalles').show();
            $('#totalCompra').text('Bs. 0.00');
            $('#MontoTotal').val('0.00');
            $('#formCompra')[0].reset();
            $('#FechaCompra').val(new Date().toISOString().split('T')[0]);
            $('#IdPolleria').val(COMPRA_CONFIG.idPolleria);
            $('#ProveedorSelect').removeClass('is-invalid');
            $('#ProveedorSelect').siblings('.invalid-feedback, .text-danger').hide();
        }
    });
}

// Envío del formulario
$('#formCompra').submit(function (e) {
    e.preventDefault();
    const form = this;

    // VALIDACIÓN DE PROVEEDOR: Solo texto rojo y borde de error, sin SweetAlert
    const $proveedor = $('#ProveedorSelect');
    if (!$proveedor.val()) {
        $proveedor.addClass('is-invalid').focus(); $proveedor.siblings('.invalid-feedback, .text-danger').show();
        return false;
    } else {
        $proveedor.removeClass('is-invalid');
    }

    const idPolleria = parseInt($('#IdPolleria').val(), 10) || 0;
    if (idPolleria === 0) {
        mostrarError('Error: No se ha configurado la pollería de almacenamiento');
        return false;
    }

    if (detalles.length === 0) {
        mostrarError('Debe agregar al menos un ítem a la compra');
        return false;
    }

    let errores = [];
    detalles.forEach((detalle, index) => {
        if (detalle.cantidad <= 0) {
            errores.push('• Ítem ' + (index + 1) + ' (' + detalle.nombre + '): Cantidad mayor a 0.');
        }
        if (detalle.precioUnitario < 0) {
            errores.push('• Ítem ' + (index + 1) + ' (' + detalle.nombre + '): El precio no puede ser negativo.');
        }
    });

    if (errores.length > 0) {
        Swal.fire({
            icon: 'warning',
            title: 'Verifique los valores',
            html: '<div class="text-start">' + errores.join('<br>') + '</div>',
            confirmButtonColor: '#ffc107'
        });
        return false;
    }

    const estado = $('#EstadoSelect').val();
    const total = $('#MontoTotal').val();

    Swal.fire({
        title: '¿Confirmar registro de compra?',
        html: `
            <div class="text-start small">
                <p class="mb-1"><strong>Estado:</strong> ${estado}</p>
                <p class="mb-1"><strong>Monto Total:</strong> Bs. ${total}</p>
                <p class="mb-0 text-muted">${estado === 'COMPLETADA' ? '⚠️ Se aumentará el inventario en almacén automáticamente.' : 'ℹ️ El stock se actualizará cuando pase a COMPLETADA.'}</p>
            </div>
        `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Sí, registrar',
        cancelButtonText: 'Revisar',
        confirmButtonColor: '#198754',
        cancelButtonColor: '#6c757d',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            const detallesParaEnviar = detalles.map(d => ({
                IdItemSeleccionado: d.IdItemSeleccionado,
                TipoItem: d.TipoItem,
                cantidad: d.cantidad,
                precioUnitario: d.precioUnitario,
                subtotal: d.subtotal
            }));

            $('#detallesJson').val(JSON.stringify(detallesParaEnviar));
            form.submit();
        }
    });
});