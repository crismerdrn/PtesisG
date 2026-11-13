using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaModelo
{
    public class CMPermisos
    {
        public int id_permiso { get; set; }

        [Required(ErrorMessage = "El nombre del menú es obligatorio")]
        [StringLength(250, ErrorMessage = "El nombre no puede exceder los 250 caracteres")]
        [Display(Name = "Nombre del Menú")]
        public string NombreMenu { get; set; }

        [Display(Name = "Módulo")]
        public string Modulo { get; set; }

        [Required(ErrorMessage = "El tipo es obligatorio")]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } 

        [Display(Name = "Menú Padre")]
        public int? Padre_id { get; set; }

        [Display(Name = "Nombre del Padre")]
        public string PadreNombre { get; set; }

        [Display(Name = "Orden")]
        public int? Orden { get; set; }

        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        public DateTime FechaRegistro { get; set; }

        public int Nivel { get; set; }
        public bool Asignado { get; set; }
        public List<CMPermisos> Hijos { get; set; }

        public CMPermisos()
        {
            Hijos = new List<CMPermisos>();
        }
    }

    public class CMRolPermiso
    {
        public int id_rol_permiso { get; set; }
        public int ROL_id_rol { get; set; }
        public string RolNombre { get; set; }
        public int permisos_id_permiso { get; set; }
        public string PermisoNombre { get; set; }
        public DateTime FechaAsignacion { get; set; }
    }

    public class CMAsignarPermisos
    {
        public int IdRol { get; set; }
        public List<int> PermisosIds { get; set; }

        public CMAsignarPermisos()
        {
            PermisosIds = new List<int>();
        }
    }

    public class CMMatrizPermiso
    {
        public int IdSubmodulo { get; set; }
        public string NombreSubmodulo { get; set; }
        public string Modulo { get; set; }

        public int? IdVer { get; set; }
        public int? IdCrear { get; set; }
        public int? IdEditar { get; set; }
        public int? IdEliminar { get; set; }

        public bool Ver { get; set; }
        public bool Crear { get; set; }
        public bool Editar { get; set; }
        public bool Eliminar { get; set; }
    }

    public class CMGrupoMatrizPermiso
    {
        public int IdModuloPadre { get; set; }
        public string NombreModuloPadre { get; set; }
        public List<CMMatrizPermiso> Submodulos { get; set; }

        public CMGrupoMatrizPermiso()
        {
            Submodulos = new List<CMMatrizPermiso>();
        }
    }

    public class CMAsignarPermisosViewModel
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; }
        public string DescripcionRol { get; set; }
        public List<CMGrupoMatrizPermiso> Grupos { get; set; }

        public CMAsignarPermisosViewModel()
        {
            Grupos = new List<CMGrupoMatrizPermiso>();
        }
    }
}