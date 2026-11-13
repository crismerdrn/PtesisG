using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;

namespace PTesis.Controllers
{
    public class PolleriaController : BaseController
    {
        private CDPolleria cdPolleria = new CDPolleria();

        // GET: Polleria
        public ActionResult Index(string buscar = "", bool? estado = null)
        {
            if (!TienePermiso("Pollerias"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Pollerías.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = TienePermiso("Pollerias");
            ViewBag.PuedeEditar = PuedeEditar("Pollerias");
            ViewBag.PuedeEliminar = PuedeEliminar("Pollerias");

            var lista = cdPolleria.Listar();

            // Aplicar filtro de estado
            if (estado.HasValue)
            {
                lista = lista.Where(p => p.Estado == estado.Value).ToList();
            }

            // Aplicar búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                var buscarLower = buscar.ToLower();
                lista = lista.Where(p =>
                    p.Nombre.ToLower().Contains(buscarLower) ||
                    p.Direccion.ToLower().Contains(buscarLower) ||
                    p.Telefono.ToLower().Contains(buscarLower)
                ).ToList();

                ViewBag.Buscar = buscar;
            }

            ViewBag.EstadoFiltro = estado;
            return View(lista);
        }

        // GET: Polleria/Create
        public ActionResult Create()
        {
            if (!TienePermiso("Pollerias"))
            {
                TempData["Error"] = "No tiene permisos para registrar pollerías.";
                return RedirectToAction("Index");
            }

            // Verificar si ya existe una pollería activa
            var polleriasActivas = cdPolleria.Listar().Where(p => p.Estado).Count();
            if (polleriasActivas > 0)
            {
                ViewBag.InfoMessage = "Ya existe una pollería activa en el sistema. Solo se permite una activa a la vez.";
            }

            return View(new CMPolleria { Estado = true });
        }

        // POST: Polleria/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMPolleria polleria)
        {
            try
            {
                if (!TienePermiso("Pollerias"))
                {
                    TempData["Error"] = "No tiene permisos para registrar pollerías.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    string mensaje = string.Empty;
                    int resultado = cdPolleria.Registrar(polleria, out mensaje);

                    if (resultado > 0)
                    {
                        TempData["Exito"] = "Polleria registrada correctamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = mensaje;
                        TempData["Error"] = mensaje;
                    }
                }

                return View(polleria);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error: " + ex.Message;
                TempData["Error"] = "Error: " + ex.Message;
                return View(polleria);
            }
        }

        // GET: Polleria/Edit/5
        public ActionResult Edit(int id)
        {
            if (!PuedeEditar("Pollerias"))
            {
                TempData["Error"] = "No tiene permisos para editar pollerías.";
                return RedirectToAction("Index");
            }

            var polleria = cdPolleria.ObtenerPorId(id);

            if (polleria == null)
            {
                TempData["Error"] = "Polleria no encontrada.";
                return RedirectToAction("Index");
            }

            return View(polleria);
        }

        // POST: Polleria/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CMPolleria polleria)
        {
            try
            {
                if (!PuedeEditar("Pollerias"))
                {
                    TempData["Error"] = "No tiene permisos para editar.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    string mensaje = string.Empty;
                    bool resultado = cdPolleria.Editar(polleria, out mensaje);

                    if (resultado)
                    {
                        TempData["Exito"] = "Polleria actualizada correctamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = mensaje;
                        TempData["Error"] = mensaje;
                    }
                }

                return View(polleria);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error: " + ex.Message;
                TempData["Error"] = "Error: " + ex.Message;
                return View(polleria);
            }
        }

        // GET: Polleria/Delete/5
        public ActionResult Delete(int id)
        {
            if (!PuedeEliminar("Pollerias"))
            {
                TempData["Error"] = "No tiene permisos para eliminar.";
                return RedirectToAction("Index");
            }

            var polleria = cdPolleria.ObtenerPorId(id);
            if (polleria == null)
            {
                TempData["Error"] = "Polleria no encontrada.";
                return RedirectToAction("Index");
            }

            return View(polleria);
        }

        // POST: Polleria/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (!PuedeEliminar("Pollerias"))
                {
                    TempData["Error"] = "No tiene permisos para eliminar.";
                    return RedirectToAction("Index");
                }

                string mensaje = string.Empty;
                bool resultado = cdPolleria.Eliminar(id, out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = "Polleria " + mensaje;
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar: " + mensaje;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Polleria/Activar/5
        public ActionResult Activar(int id)
        {
            try
            {
                if (!PuedeEditar("Pollerias"))
                {
                    TempData["Error"] = "No tiene permisos para activar.";
                    return RedirectToAction("Index");
                }

                var polleria = cdPolleria.ObtenerPorId(id);
                if (polleria == null)
                {
                    TempData["Error"] = "Polleria no encontrada.";
                    return RedirectToAction("Index");
                }

                // Verificar si ya existe una pollería activa
                var polleriasActivas = cdPolleria.Listar().Where(p => p.Estado && p.id_polleria != id).Count();
                if (polleriasActivas > 0)
                {
                    TempData["Error"] = "Ya existe una pollería activa en el sistema. Solo se permite una activa.";
                    return RedirectToAction("Index", new { estado = false });
                }

                polleria.Estado = true;
                string mensaje = string.Empty;
                bool resultado = cdPolleria.Editar(polleria, out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = "Polleria activada correctamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo activar: " + mensaje;
                }

                return RedirectToAction("Index", new { estado = false });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Polleria/Desactivar/5
        public ActionResult Desactivar(int id)
        {
            try
            {
                if (!PuedeEditar("Pollerias"))
                {
                    TempData["Error"] = "No tiene permisos para desactivar.";
                    return RedirectToAction("Index");
                }

                var polleria = cdPolleria.ObtenerPorId(id);
                if (polleria == null)
                {
                    TempData["Error"] = "Polleria no encontrada.";
                    return RedirectToAction("Index");
                }

                polleria.Estado = false;
                string mensaje = string.Empty;
                bool resultado = cdPolleria.Editar(polleria, out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = "Polleria desactivada correctamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo desactivar: " + mensaje;
                }

                return RedirectToAction("Index", new { estado = true });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}