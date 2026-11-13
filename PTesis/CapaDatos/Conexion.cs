using System.Data.SqlClient;

namespace CapaDatos
{
    public class Conexion
    {
        public static string CN = "Data Source =DESKTOP-NEOKNPT; Initial Catalog = proyecto_grado;Integrated Security=True";
        public static SqlConnection Conectar()
        {
            SqlConnection cn = new SqlConnection(CN);
            return cn;
        }
    }
}
