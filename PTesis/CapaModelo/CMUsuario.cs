using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; 
using System.Web.Mvc;

namespace CapaModelo
{
    public class Usuario
    {
        public int id_usuario { get; set; }

        [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Required(ErrorMessage = "El campo Apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres.")]
        [Display(Name = "Apellido")]
        public string apellido { get; set; }

        [Required(ErrorMessage = "El campo Correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico no válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string correo { get; set; }

        [Required(ErrorMessage = "El campo Teléfono es obligatorio.")]
        [RegularExpression(@"^\d{6,20}$", ErrorMessage = "El teléfono solo debe contener números.")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres.")]
        [Display(Name = "Teléfono")]
        public string telefono { get; set; }

        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres.")]
        [Display(Name = "Dirección")]
        public string direccion { get; set; }

        [Required(ErrorMessage = "El campo Usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres.")]
        [Display(Name = "Usuario")]
        public string usuario { get; set; }

        [StringLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string password { get; set; }

        public bool estado { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un Rol.")]
        [Display(Name = "Rol")]
        public int ROL_id_rol { get; set; }

        public string Rol { get; set; }
        public List<string> Permisos { get; set; }

        public Usuario()
        {
            Permisos = new List<string>();
        }

        public bool TienePermiso(string nombreMenu)
        {
            return Permisos.Contains(nombreMenu) || Rol == "Administrador";
        }

        public bool PuedeEditar(string modulo)
        {
            return TienePermiso(modulo + "_Editar") || Rol == "Administrador";
        }

        public bool PuedeEliminar(string modulo)
        {
            return TienePermiso(modulo + "_Eliminar") || Rol == "Administrador";
        }
    }
}