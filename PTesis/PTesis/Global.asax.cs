using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Globalization;
using System.Threading;

namespace PTesis
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Configurar culture para toda la aplicación
            ConfigureCulture();
            
            // Registrar el model binder para decimales
            ModelBinders.Binders.Add(typeof(decimal), new DecimalModelBinder());
            ModelBinders.Binders.Add(typeof(decimal?), new DecimalModelBinder());
            
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void ConfigureCulture()
        {
            CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            
            // Configurar separadores decimales
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.NumberGroupSeparator = ",";
            culture.NumberFormat.CurrencyDecimalSeparator = ".";
            culture.NumberFormat.CurrencyGroupSeparator = ",";
            
            //// Configurar formato de fecha si lo necesitas
            //culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            //culture.DateTimeFormat.LongDatePattern = "dddd, dd 'de' MMMM 'de' yyyy";
            
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context == null || context.Session == null)
                return;
            
            // Configuración de cache
            context.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            context.Response.Cache.SetValidUntilExpires(false);
            context.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();
            
            // Asegurar cultura en cada request
            ConfigureCulture();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Forzar cultura en cada request
            ConfigureCulture();
        }
    }
    
    // Model Binder personalizado para decimales
    public class DecimalModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, 
            ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider
                .GetValue(bindingContext.ModelName);
            
            var modelState = new ModelState { Value = valueResult };
            object actualValue = null;
            
            if (valueResult != null && !string.IsNullOrWhiteSpace(valueResult.AttemptedValue))
            {
                try
                {
                    // Limpiar y convertir el valor
                    string attemptedValue = CleanDecimalString(valueResult.AttemptedValue);
                    
                    // Intentar parsear con diferentes formatos
                    if (decimal.TryParse(attemptedValue, 
                        NumberStyles.Any, 
                        CultureInfo.InvariantCulture, 
                        out decimal decimalValue))
                    {
                        actualValue = decimalValue;
                    }
                    else
                    {
                        modelState.Errors.Add($"Formato inválido. Use punto decimal (ej: 20.0000). Valor recibido: {valueResult.AttemptedValue}");
                    }
                }
                catch (Exception ex)
                {
                    modelState.Errors.Add($"Error al convertir a decimal: {ex.Message}");
                }
            }
            
            bindingContext.ModelState.Add(bindingContext.ModelName, modelState);
            return actualValue;
        }
        
        private string CleanDecimalString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
                
            // Eliminar espacios
            input = input.Trim();
            
            // Si hay múltiples puntos decimales, tomar el último como decimal
            if (input.Count(c => c == '.') > 1)
            {
                int lastDot = input.LastIndexOf('.');
                input = input.Substring(0, lastDot).Replace(".", "") + 
                        input.Substring(lastDot);
            }
            
            // Reemplazar coma por punto si es necesario
            input = input.Replace(",", ".");
            
            return input;
        }
    }
}