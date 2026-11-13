using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CapaDatos;
using CapaModelo;
using PagedList;

namespace PTesis.Controllers
{
    public class ClienteController : BaseController
    {
        private CDClientes cdCliente = new CDClientes();

        public ActionResult Index(string buscar = "", bool? estado = null, int? pagina = null)
        {
            if (!TienePermiso("Clientes"))
            {
                TempData["Error"] = "No tiene permisos para acceder al módulo Clientes.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PuedeCrear = PuedeCrear("Clientes");
            ViewBag.PuedeEditar = PuedeEditar("Clientes");
            ViewBag.PuedeEliminar = PuedeEliminar("Clientes");

            List<CMClientes> lista;

            if (!string.IsNullOrEmpty(buscar))
            {
                lista = cdCliente.Buscar(buscar);
                ViewBag.Buscar = buscar;
            }
            else
            {
                lista = cdCliente.Listar(estado);
            }

            if (lista == null)
            {
                lista = new List<CMClientes>();
            }

            ViewBag.EstadoFiltro = estado;

            int pageSize = 10;
            int pageNumber = pagina ?? 1;

            var listaPaginada = lista
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .ToPagedList(pageNumber, pageSize);

            return View(listaPaginada);
        }

        // GET: Cliente/Create
        public ActionResult Create()
        {
            if (!PuedeCrear("Clientes"))
            {
                TempData["Error"] = "No tiene permisos para registrar clientes.";
                return RedirectToAction("Index");
            }

            return View(new CMClientes { Estado = true });
        }

        // POST: Cliente/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CMClientes cliente)
        {
            try
            {
                if (!PuedeCrear("Clientes"))
                {
                    TempData["Error"] = "No tiene permisos para registrar clientes.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    string mensaje = string.Empty;
                    int idGenerado = cdCliente.Registrar(cliente, out mensaje);

                    if (idGenerado > 0)
                    {
                        TempData["Exito"] = "Cliente registrado correctamente";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.Error = mensaje;
                        TempData["Error"] = mensaje;
                    }
                }
                return View(cliente);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(cliente);
            }
        }

        // GET: Cliente/Edit/5
        public ActionResult Edit(int id)
        {
            if (!PuedeEditar("Clientes"))
            {
                TempData["Error"] = "No tiene permisos para editar clientes.";
                return RedirectToAction("Index");
            }

            var listaClientes = cdCliente.Listar(null); // Traer todos incluyendo inactivos
            var cliente = listaClientes.FirstOrDefault(c => c.id_cliente == id);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            return View(cliente);
        }

        // POST: Cliente/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CMClientes cliente)
        {
            try
            {
                if (!PuedeEditar("Clientes"))
                {
                    TempData["Error"] = "No tiene permisos para editar clientes.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    cliente.id_cliente = id;
                    string mensaje = string.Empty;
                    bool resultado = cdCliente.Editar(cliente, out mensaje);

                    if (resultado)
                    {
                        TempData["Exito"] = "Cliente actualizado correctamente.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.Error = mensaje;
                        TempData["Error"] = mensaje;
                    }
                }
                return View(cliente);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(cliente);
            }
        }

        // GET: Cliente/Delete/5 (Cambia estado a inactivo)
        public ActionResult Delete(int id)
        {
            if (!PuedeEliminar("Clientes"))
            {
                TempData["Error"] = "No tiene permisos para eliminar clientes.";
                return RedirectToAction("Index");
            }

            var listaClientes = cdCliente.Listar(null); // Traer todos para encontrar el cliente
            var cliente = listaClientes.FirstOrDefault(c => c.id_cliente == id);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Index");
            }

            return View(cliente);
        }

        // POST: Cliente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (!PuedeEliminar("Clientes"))
                {
                    TempData["Error"] = "No tiene permisos para eliminar clientes.";
                    return RedirectToAction("Index");
                }

                // Obtener el cliente actual
                var listaClientes = cdCliente.Listar(null);
                var cliente = listaClientes.FirstOrDefault(c => c.id_cliente == id);

                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado.";
                    return RedirectToAction("Index");
                }

                // Cambiar estado a inactivo (0)
                cliente.Estado = false;
                string mensaje = string.Empty;
                bool resultado = cdCliente.Editar(cliente, out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = "Cliente desactivado correctamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo desactivar el cliente: " + mensaje;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Cliente/Activar/5
        public ActionResult Activar(int id)
        {
            try
            {
                if (!PuedeEditar("Clientes"))
                {
                    TempData["Error"] = "No tiene permisos para activar clientes.";
                    return RedirectToAction("Index");
                }

                // Obtener el cliente actual
                var listaClientes = cdCliente.Listar(null);
                var cliente = listaClientes.FirstOrDefault(c => c.id_cliente == id);

                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado.";
                    return RedirectToAction("Index");
                }

                // Cambiar estado a activo (1)
                cliente.Estado = true;
                string mensaje = string.Empty;
                bool resultado = cdCliente.Editar(cliente, out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = "Cliente activado correctamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo activar el cliente: " + mensaje;
                }

                return RedirectToAction("Index", new { estado = false });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Cliente/Desactivar/5
        public ActionResult Desactivar(int id)
        {
            try
            {
                if (!PuedeEditar("Clientes"))
                {
                    TempData["Error"] = "No tiene permisos para desactivar clientes.";
                    return RedirectToAction("Index");
                }

                // Obtener el cliente actual
                var listaClientes = cdCliente.Listar(null);
                var cliente = listaClientes.FirstOrDefault(c => c.id_cliente == id);

                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado.";
                    return RedirectToAction("Index");
                }

                // Cambiar estado a inactivo (0)
                cliente.Estado = false;
                string mensaje = string.Empty;
                bool resultado = cdCliente.Editar(cliente, out mensaje);

                if (resultado)
                {
                    TempData["Exito"] = "Cliente desactivado correctamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo desactivar el cliente: " + mensaje;
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