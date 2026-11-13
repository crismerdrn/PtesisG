using CapaDatos;
using CapaModelo;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace PTesis.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private CDUsuario datos = new CDUsuario();

        public ActionResult Index()
        {
            if (Session["Usuario"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CMLogin modelo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    string contrasenaHash = ObtenerSHA256(modelo.password);
                    int idUsuario = datos.LoginUsuario(modelo.usuario, contrasenaHash);

                    if (idUsuario > 0)
                    {
                        var usuarioSesion = datos.ObtenerUsuarioConPermisos(idUsuario);

                        if (usuarioSesion != null)
                        {
                            Session["Usuario"] = usuarioSesion;
                            FormsAuthentication.SetAuthCookie(idUsuario.ToString(), false);
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Usuario o contraseña incorrectos");
                    }
                }
                return View(modelo);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error interno del sistema");
                return View(modelo);
            }
        }

        public ActionResult CerrarSesion()
        {
            Session.Clear();
            Session.Abandon();

            FormsAuthentication.SignOut();

            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, "")
                {
                    Expires = DateTime.Now.AddDays(-1)
                };
                Response.Cookies.Add(cookie);
            }

            return Redirect(Url.Action("Index", "Login"));
        }

        private string ObtenerSHA256(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}