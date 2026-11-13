using System.Web;
using System.Web.Mvc;
using PTesis.App_Start;
namespace PTesis
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new NoCacheAttribute());
            filters.Add(new VerificarSesionAttribute());
        }
    }
}
