using CapaDatos;
using CapaModelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PTesis.Controllers
{
  [Authorize]
    public class HomeController : BaseController
    {
        private CDDashboard cdDashboard = new CDDashboard();
        private CDPolleria cdPolleria = new CDPolleria();

        public ActionResult Index()
        {
            var polleria = ObtenerPolleriaActiva();
            int? idPolleria = polleria?.id_polleria;

            var modelo = cdDashboard.ObtenerMetricasDashboard(idPolleria);

            ViewBag.NombrePolleria = polleria?.Nombre ?? "El Escondite del Loro";

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarPolleria(int idPolleria, string returnUrl)
        {
            if (EsPolleriaActiva(idPolleria))
                SeleccionarPolleriaTrabajo(idPolleria);
            else
                TempData["Error"] = "La pollería seleccionada no existe o está inactiva.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

    }
}