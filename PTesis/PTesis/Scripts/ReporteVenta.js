function exportarExcel() {
    var tabla = document.getElementById("tablaVentas");
    if (!tabla) return;

    var fInicioInput = document.querySelector('input[name="fechaInicio"]');
    var fFinInput = document.querySelector('input[name="fechaFin"]');
    var fInicio = fInicioInput && fInicioInput.value ? fInicioInput.value : "Inicio";
    var fFin = fFinInput && fFinInput.value ? fFinInput.value : "Fin";
    var nombreArchivo = "Reporte_Ventas_" + fInicio + "_al_" + fFin + ".xlsx";

    var clonTabla = tabla.cloneNode(true);
    clonTabla.querySelectorAll('.col-accion').forEach(function (el) {
        el.remove();
    });

    var wb = XLSX.utils.table_to_book(clonTabla, { sheet: "Ventas", raw: false });
    var ws = wb.Sheets["Ventas"];

    ws['!cols'] = [
        { wch: 12 },    
        { wch: 19.71 }, 
        { wch: 22 },    
        { wch: 24 },    
        { wch: 12 },    
        { wch: 14 },    
        { wch: 8 },     
        { wch: 14 },    
        { wch: 13.14 }  
    ];

    XLSX.writeFile(wb, nombreArchivo);
}

function imprimirConNombre() {
    var fInicioInput = document.querySelector('input[name="fechaInicio"]');
    var fFinInput = document.querySelector('input[name="fechaFin"]');

    var fInicio = fInicioInput && fInicioInput.value ? fInicioInput.value : "Inicio";
    var fFin = fFinInput && fFinInput.value ? fFinInput.value : "Fin";

    var nombreSugerido = "Reporte_Ventas_" + fInicio + "_al_" + fFin;
    var tituloOriginal = document.title;

    var restaurarTitulo = function () {
        document.title = tituloOriginal;
        window.removeEventListener('afterprint', restaurarTitulo);
    };

    window.addEventListener('afterprint', restaurarTitulo);
    document.title = nombreSugerido;
    window.print();
}

function descargarPDF() {
    var elemento = document.querySelector('.container-fluid');

    var elementosOcultar = document.querySelectorAll('.no-print, .col-accion, .col-estado');
    elementosOcultar.forEach(function (el) {
        el.style.setProperty('display', 'none', 'important');
    });

    var header = document.querySelector('.print-header');
    if (header) header.style.setProperty('display', 'block', 'important');

    var fInicioInput = document.querySelector('input[name="fechaInicio"]');
    var fFinInput = document.querySelector('input[name="fechaFin"]');
    var fInicio = fInicioInput && fInicioInput.value ? fInicioInput.value : "Inicio";
    var fFin = fFinInput && fFinInput.value ? fFinInput.value : "Fin";
    var nombreArchivo = "Reporte_Ventas_" + fInicio + "_al_" + fFin + ".pdf";

    var opciones = {
        margin: [8, 8, 8, 8],
        filename: nombreArchivo,
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 2, useCORS: true },
        jsPDF: { unit: 'mm', format: 'letter', orientation: 'portrait' }
    };

    html2pdf().set(opciones).from(elemento).save().then(function () {
        elementosOcultar.forEach(function (el) {
            el.style.removeProperty('display');
        });
        if (header) header.style.removeProperty('display');
    });
}