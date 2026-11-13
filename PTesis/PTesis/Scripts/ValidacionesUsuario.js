
(function ($) {
    'use strict';

    $.validator.addMethod("telefonoNumerico", function (value, element) {
        return this.optional(element) || /^[0-9]+$/.test(value);
    }, "El teléfono solo debe contener números.");

    function configurarValidaciones() {
        var $telefono = $('#telefono');
        var $password = $('#password');
        var $usuario = $('#usuario');
        var $correo = $('#correo');
        var $selectRol = $('#id_rol');
        var $nombre = $('#nombre');
        var $apellido = $('#apellido');

        if ($telefono.length > 0) {
            var esRequerido = !$('#id_usuario').length || $('#id_usuario').val() === '';

            var reglas = {
                telefonoNumerico: true,
                minlength: 6,
                maxlength: 20,
                digits: true
            };

            var mensajes = {
                telefonoNumerico: "El teléfono solo debe contener números.",
                minlength: "El teléfono debe tener al menos 6 dígitos.",
                maxlength: "El teléfono no puede tener más de 20 dígitos.",
                digits: "El teléfono solo debe contener números."
            };

            if (esRequerido) {
                reglas.required = true;
                mensajes.required = "El campo Teléfono es obligatorio.";
            }

            $telefono.rules('add', reglas);
            $telefono.rules('add', { messages: mensajes });
        }

        if ($password.length > 0) {
            var esRequerido = !$('#id_usuario').length || $('#id_usuario').val() === '';

            var reglas = {
                minlength: 6,
                maxlength: 100
            };

            var mensajes = {
                minlength: "La contraseña debe tener al menos 6 caracteres.",
                maxlength: "La contraseña no puede tener más de 100 caracteres."
            };

            if (esRequerido) {
                reglas.required = true;
                mensajes.required = "La contraseña es obligatoria.";
            }

            $password.rules('add', reglas);
            $password.rules('add', { messages: mensajes });
        }

        if ($usuario.length > 0) {
            $usuario.rules('add', {
                required: true,
                minlength: 3,
                maxlength: 50,
                messages: {
                    required: "El nombre de usuario es obligatorio.",
                    minlength: "El nombre de usuario debe tener al menos 3 caracteres.",
                    maxlength: "El nombre de usuario no puede tener más de 50 caracteres."
                }
            });
        }

        if ($correo.length > 0) {
            $correo.rules('add', {
                required: true,
                email: true,
                maxlength: 100,
                messages: {
                    required: "El correo electrónico es obligatorio.",
                    email: "Ingrese un correo electrónico válido.",
                    maxlength: "El correo no puede tener más de 100 caracteres."
                }
            });
        }

        if ($nombre.length > 0) {
            $nombre.rules('add', {
                required: true,
                maxlength: 100,
                messages: {
                    required: "El nombre es obligatorio.",
                    maxlength: "El nombre no puede tener más de 100 caracteres."
                }
            });
        }

        if ($apellido.length > 0) {
            $apellido.rules('add', {
                required: true,
                maxlength: 100,
                messages: {
                    required: "El apellido es obligatorio.",
                    maxlength: "El apellido no puede tener más de 100 caracteres."
                }
            });
        }

        if ($selectRol.length > 0) {
            $selectRol.rules('add', {
                required: true,
                min: 1,
                messages: {
                    required: "Debe seleccionar un rol.",
                    min: "Debe seleccionar un rol válido."
                }
            });
        }
    }

    function configurarTelefono() {
        var $telefono = $('#telefono');

        if ($telefono.length === 0) return;

        $telefono.on('input', function () {
            var valor = $(this).val();
            var soloNumeros = valor.replace(/[^0-9]/g, '');
            if (soloNumeros.length > 20) {
                soloNumeros = soloNumeros.substring(0, 20);
            }
            $(this).val(soloNumeros);
        });

        $telefono.on('paste', function (e) {
            var clipboardData = e.originalEvent.clipboardData || window.clipboardData;
            var pastedData = clipboardData.getData('text');
            var numericData = pastedData.replace(/[^0-9]/g, '');
            if (numericData.length > 20) {
                numericData = numericData.substring(0, 20);
            }
            $(this).val(numericData);
            e.preventDefault();
        });

        $telefono.on('keydown', function (e) {
            var key = e.key;

            var teclasPermitidas = [
                'Backspace', 'Delete', 'Tab', 'Enter', 'Escape',
                'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
                'Home', 'End', 'PageUp', 'PageDown'
            ];

            if (teclasPermitidas.includes(key)) {
                return true;
            }

            if (e.ctrlKey || e.metaKey) {
                return true;
            }

            if (/^[0-9]$/.test(key)) {
                // Verificar límite de 20 caracteres
                if ($(this).val().length >= 20) {
                    e.preventDefault();
                    return false;
                }
                return true;
            }

            e.preventDefault();
            return false;
        });

        $telefono.on('keypress', function (e) {
            // Obtener el código del carácter
            var charCode = e.which || e.keyCode;
            if (charCode < 48 || charCode > 57) {
                e.preventDefault();
                return false;
            }

            if ($(this).val().length >= 20) {
                e.preventDefault();
                return false;
            }

            return true;
        });

        $telefono.on('blur', function () {
            var valor = $(this).val();
            var soloNumeros = valor.replace(/[^0-9]/g, '');
            $(this).val(soloNumeros);
        });
    }

    function configurarDropdownRol() {
        var $selectRol = $('#id_rol');

        if ($selectRol.length > 0 && $selectRol[0].options.length > 0) {
            var firstOption = $selectRol[0].options[0];
            if (firstOption && (firstOption.value === '' || firstOption.value === '0')) {
                firstOption.disabled = true;
            }
        }
    }

    $(document).ready(function () {
        configurarValidaciones();
        configurarTelefono();
        configurarDropdownRol();

        console.log('Validaciones de usuario cargadas correctamente.');
    });

})(jQuery);