// Scripts/validacion-proveedor.js

function inicializarFormularioProveedor(config) {
    const urlVerificar = config?.urlVerificarDuplicado || '';
    const idExcluir = config?.idExcluir || null;
    const esModoEdicion = idExcluir !== null && idExcluir > 0;

    // 1. Mensajes en español para jQuery Validate
    if ($.validator) {
        $.extend($.validator.messages, {
            required: "Este campo es obligatorio."
        });
    }

    // 2. Alternar campos (solo aplica en modo Create donde existen los radios)
    if (!esModoEdicion) {
        function toggleCampos() {
            const esEmpresa = $('#tipoEmpresa').is(':checked');

            if ($('#Telefono').val().trim() === '') {
                $('#Telefono').removeClass('is-invalid is-valid');
                $('#Telefono').siblings('.invalid-feedback').text('');
            }

            if (esEmpresa) {
                $('#camposPersona').hide();
                $('#camposEmpresa').show();
                $('#Nombre, #Apellido, #CI').removeClass('is-invalid is-valid');
                $('#camposPersona .invalid-feedback').text('');
            } else {
                $('#camposPersona').show();
                $('#camposEmpresa').hide();
                $('#RazonSocial, #NIT').removeClass('is-invalid is-valid');
                $('#camposEmpresa .invalid-feedback').text('');
            }

            if ($('form').data('validator')) {
                $('form').data('validator').resetForm();
            }
        }

        $('input[name="esEmpresa"]').change(toggleCampos);
        toggleCampos();
    }

    // 3. Verificación de duplicados por AJAX (Debounce)
    let timer;

    function iniciarVerificacion(tipo, selector) {
        const valor = $(selector).val().trim();
        clearTimeout(timer);

        timer = setTimeout(function () {
            if (valor.length >= 3) {
                verificarDuplicado(tipo, valor, selector);
            }
        }, 500);
    }

    function verificarDuplicado(tipo, valor, selector) {
        if (!urlVerificar) return;

        const $input = $(selector);
        let $feedback = $input.siblings('.invalid-feedback');

        if ($feedback.length === 0) {
            $feedback = $("span[data-valmsg-for='" + $input.attr('id') + "']");
        }

        $.post(urlVerificar, { tipo: tipo, valor: valor, idExcluir: idExcluir })
            .done(function (data) {
                $input.removeClass('is-valid is-invalid');

                if (data.valido) {
                    $input.addClass('is-valid');
                    $feedback.text('');
                } else {
                    $input.addClass('is-invalid');
                    $feedback.text(data.mensaje);
                }
            })
            .fail(function () {
                console.error("Error al conectar con la verificación de duplicados.");
            });
    }

    // Event listeners
    $('#NIT').on('input', function () {
        iniciarVerificacion('nit', '#NIT');
    });

    $('#CI').on('input', function () {
        iniciarVerificacion('ci', '#CI');
    });

    $('#Telefono').on('input', function () {
        this.value = this.value.replace(/\D/g, '');
        const valor = $(this).val().trim();
        const $feedback = $(this).siblings('.invalid-feedback');

        if (valor.length === 0) {
            $(this).removeClass('is-valid is-invalid');
            $feedback.text('');
        } else if (valor.length < 8) {
            $(this).removeClass('is-valid').addClass('is-invalid');
            $feedback.text('El teléfono debe tener al menos 8 dígitos.');
        } else {
            $(this).removeClass('is-invalid');
            $feedback.text('');
            iniciarVerificacion('telefono', '#Telefono');
        }
    });

    // 4. Validación antes del envío
    $('form').on('submit', function (e) {
        let hayError = false;

        // Si existe #tipoEmpresa (Create), lo evaluamos por checkbox; si no (Edit), por campo oculto
        const esEmpresa = $('#tipoEmpresa').length
            ? $('#tipoEmpresa').is(':checked')
            : ($('#esEmpresa').val() === 'True' || $('#esEmpresa').val() === 'true');

        if (esEmpresa) {
            if (!$('#RazonSocial').val().trim()) {
                $('#RazonSocial').addClass('is-invalid');
                $('#RazonSocial').siblings('.invalid-feedback').text('Debe ingresar la Razón Social.');
                hayError = true;
            } else {
                $('#RazonSocial').removeClass('is-invalid');
            }

            if (!$('#NIT').val().trim()) {
                $('#NIT').addClass('is-invalid');
                $('#NIT').siblings('.invalid-feedback').text('Debe ingresar el NIT.');
                hayError = true;
            } else {
                $('#NIT').removeClass('is-invalid');
            }
        } else {
            if (!$('#Nombre').val().trim()) {
                $('#Nombre').addClass('is-invalid');
                $('#Nombre').siblings('.invalid-feedback').text('Debe ingresar el Nombre.');
                hayError = true;
            } else {
                $('#Nombre').removeClass('is-invalid');
            }

            if (!$('#Apellido').val().trim()) {
                $('#Apellido').addClass('is-invalid');
                $('#Apellido').siblings('.invalid-feedback').text('Debe ingresar el Apellido.');
                hayError = true;
            } else {
                $('#Apellido').removeClass('is-invalid');
            }

            if (!$('#CI').val().trim()) {
                $('#CI').addClass('is-invalid');
                $('#CI').siblings('.invalid-feedback').text('Debe ingresar el CI.');
                hayError = true;
            } else {
                $('#CI').removeClass('is-invalid');
            }
        }

        const telVal = $('#Telefono').val().trim();
        const $telFeedback = $('#Telefono').siblings('.invalid-feedback');

        if (!telVal) {
            $('#Telefono').addClass('is-invalid');
            $telFeedback.text('El teléfono del proveedor es obligatorio.');
            hayError = true;
        } else if (telVal.length < 8) {
            $('#Telefono').addClass('is-invalid');
            $telFeedback.text('El teléfono debe tener al menos 8 dígitos.');
            hayError = true;
        } else if ($('#Telefono').hasClass('is-invalid')) {
            hayError = true;
        } else {
            $('#Telefono').removeClass('is-invalid');
        }

        if (hayError || $(':visible.is-invalid').length > 0) {
            e.preventDefault();
            return false;
        }
    });
}