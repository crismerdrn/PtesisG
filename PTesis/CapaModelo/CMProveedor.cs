using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMProveedor
    {
        public int id_proveedor { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el tipo de proveedor")]
        [Display(Name = "Tipo de Proveedor")]
        public bool esEmpresa { get; set; } = true; // true = Empresa, false = Persona

        // Campos para Empresa
        [Display(Name = "Razón Social")]
        [StringLength(250, ErrorMessage = "La razón social no puede exceder los 250 caracteres")]
        [RequiredIf("esEmpresa", true, ErrorMessage = "La razón social es obligatoria para empresas")]
        public string RazonSocial { get; set; }

        [Display(Name = "NIT")]
        [StringLength(50, ErrorMessage = "El NIT no puede exceder los 50 caracteres")]
        [RequiredIf("esEmpresa", true, ErrorMessage = "El NIT es obligatorio para empresas")]
        [RegularExpression(@"^[0-9-]+$", ErrorMessage = "El NIT solo puede contener números y guiones")]
        public string NIT { get; set; }

        // Campos para Persona
        [Display(Name = "Nombre")]
        [StringLength(250, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 250 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        [RequiredIf("esEmpresa", false, ErrorMessage = "El nombre es obligatorio para personas")]
        public string Nombre { get; set; }

        [Display(Name = "Apellido")]
        [StringLength(250, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 250 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras y espacios")]
        [RequiredIf("esEmpresa", false, ErrorMessage = "El apellido es obligatorio para personas")]
        public string Apellido { get; set; }

        [Display(Name = "Cédula de Identidad")]
        [StringLength(250, ErrorMessage = "El CI no puede exceder los 250 caracteres")]
        [RequiredIf("esEmpresa", false, ErrorMessage = "El CI es obligatorio para personas")]
        public string CI { get; set; }

        // Campos comunes
        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(250, MinimumLength = 8, ErrorMessage = "El teléfono debe tener entre 8 y 250 caracteres")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [StringLength(250, ErrorMessage = "La dirección no puede exceder los 250 caracteres")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [StringLength(250, ErrorMessage = "El email no puede exceder los 250 caracteres")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }

        [Display(Name = "Nombre Completo / Razón Social")]
        public string NombreCompleto => esEmpresa ? RazonSocial : $"{Nombre} {Apellido}";

        [Display(Name = "Tipo")]
        public string TipoProveedor => esEmpresa ? "Empresa" : "Persona";

        [Display(Name = "Identificación")]
        public string Identificacion => esEmpresa ? NIT : CI;
    }

    // Clase de validación condicional
    public class RequiredIfAttribute : ValidationAttribute
    {
        private string PropertyName { get; set; }
        private object DesiredValue { get; set; }

        public RequiredIfAttribute(string propertyName, object desiredValue, string errorMessage = "")
        {
            PropertyName = propertyName;
            DesiredValue = desiredValue;
            ErrorMessage = errorMessage;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();
            var propertyValue = type.GetProperty(PropertyName)?.GetValue(instance, null);

            if (propertyValue?.ToString() == DesiredValue.ToString())
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
