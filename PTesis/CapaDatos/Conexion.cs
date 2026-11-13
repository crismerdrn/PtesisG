using System.Configuration;
using System.Data.SqlClient;

namespace CapaDatos
{
    public class Conexion
    {
        public static string CN =
            ConfigurationManager.ConnectionStrings["proyecto_grado"]?.ConnectionString
            ?? "Data Source =DESKTOP-NEOKNPT; Initial Catalog = proyecto_grado;Integrated Security=True";
        public static SqlConnection Conectar()
        {
            SqlConnection cn = new SqlConnection(CN);
            return cn;
        }
    }
}
