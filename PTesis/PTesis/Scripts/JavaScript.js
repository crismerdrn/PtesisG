document.addEventListener("DOMContentLoaded", function () {
    const sidebarToggle = document.getElementById("sidebarToggle");
    const body = document.body;
    console.log("JavaScript cargado - Botón sidebarToggle:", sidebarToggle);

    if (sidebarToggle) {
        sidebarToggle.addEventListener("click", function (e) {
            e.preventDefault();
            console.log("Botón clickeado");
            body.classList.toggle("sb-sidenav-toggled");
            console.log("Clase toggled:", body.classList.contains("sb-sidenav-toggled"));
            localStorage.setItem(
                "sb|sidebar-toggle",
                body.classList.contains("sb-sidenav-toggled")
            );
        });
    } else {
        console.error("No se encontró el botón sidebarToggle");
    }

    const savedState = localStorage.getItem("sb|sidebar-toggle");
    console.log("Estado guardado:", savedState);
    if (savedState === "true") {
        body.classList.add("sb-sidenav-toggled");
        console.log("Estado cargado: menú colapsado");
    }

    // El select de roles
    const select = document.getElementById('id_rol');
    if (select && !select.querySelector('option[value=""]')) {
        const option = document.createElement('option');
        option.value = "";
        option.text = "Seleccione un rol";
        option.disabled = true;
        option.selected = true;
        select.insertBefore(option, select.firstChild);
    }

    // Código para prevenir copy/paste en campos de contraseña
    const passwordField = document.querySelector('input[name="password"]');
    if (passwordField) {
        passwordField.addEventListener('paste', function (e) {
            e.preventDefault();
        });
        passwordField.addEventListener('copy', function (e) {
            e.preventDefault();
        });
        passwordField.addEventListener('cut', function (e) {
            e.preventDefault();
        });
    }
});

// Código jQuery (si es necesario mantenerlo separado)
$(document).ready(function () {
    // Tu código jQuery existente aquí si lo necesitas
});