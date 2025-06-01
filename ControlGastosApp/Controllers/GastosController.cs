using ControlGastosApp.Data;
using ControlGastosApp.Models;
using ControlGastosApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Transactions;

namespace ControlGastosApp.Controllers
{
    [Authorize]
    public class GastosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public GastosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Gastos
        // Agregar estos métodos al controlador existente

        [HttpGet]
        public async Task<IActionResult> Index(string filter = null, string month = null)
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.Detalles)
                    .ThenInclude(d => d.TipoGasto)
                .Where(g => g.UserId == userId)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(filter))
            {
                var today = DateTime.Today;
                switch (filter)
                {
                    case "today":
                        query = query.Where(g => g.Fecha == today);
                        break;
                    case "week":
                        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                        var endOfWeek = startOfWeek.AddDays(6);
                        query = query.Where(g => g.Fecha >= startOfWeek && g.Fecha <= endOfWeek);
                        break;
                    case "month":
                        query = query.Where(g => g.Fecha.Month == today.Month && g.Fecha.Year == today.Year);
                        break;
                }
            }

            // Filtrar por mes específico
            if (!string.IsNullOrEmpty(month))
            {
                var date = DateTime.ParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture);
                query = query.Where(g => g.Fecha.Month == date.Month && g.Fecha.Year == date.Year);
            }

            var gastos = await query
                .OrderByDescending(g => g.Fecha)
                .ThenByDescending(g => g.Id)
                .ToListAsync();

            return View(gastos);
        }

        // Acción para cargar detalles en modal
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var gasto = await _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.Detalles)
                    .ThenInclude(d => d.TipoGasto)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gasto == null)
            {
                return NotFound();
            }

            return PartialView("_DetailsPartial", gasto);
        }

        public IActionResult Create()
        {
            bool hayFondos = _context.FondosMonetarios.Any();
            bool hayTiposGasto = _context.TiposGasto.Any();

            ViewBag.FondoMonetarioId = hayFondos
                ? new SelectList(_context.FondosMonetarios, "Id", "Nombre")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.TiposGasto = hayTiposGasto
                ? new SelectList(_context.TiposGasto, "Codigo", "Nombre")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            // Pasar estados a la vista
            ViewBag.HayFondos = hayFondos;
            ViewBag.HayTiposGasto = hayTiposGasto;

            if (!hayFondos)
            {
                TempData["Alerta"] = "Debe crear fondos monetarios primero.";
            }
            else if (!hayTiposGasto)
            {
                TempData["Alerta"] = "Debe crear tipos de gasto primero.";
            }

            var gasto = new Gasto
            {
                Fecha = DateTime.Today,
                Detalles = new List<DetalleGasto> { new DetalleGasto() }
            };

            ViewBag.TiposDocumento = new List<SelectListItem>
            {
                new SelectListItem("Factura", "Factura"),
                new SelectListItem("Comprobante", "Comprobante"),
                new SelectListItem("Otro", "Otro")
            };

            return View(gasto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GastoFormModel gasto)
        {
            // 1. Validación de número de documento único
            if (await _context.Gastos.AnyAsync(g =>
                g.TipoDocumento == gasto.TipoDocumento &&
                g.NumeroDocumento == gasto.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "Este documento ya está registrado");
            }

            // 2. Filtrar y validar detalles - VERSIÓN REFORZADA
            gasto.Detalles = gasto.Detalles
                .Where(d => d.Monto > 0 && !string.IsNullOrWhiteSpace(d.TipoGastoCodigo))
                .Select(d => new DetalleGastoFormModel
                {
                    TipoGastoCodigo = d.TipoGastoCodigo?.Trim(), // Limpiar espacios
                    Monto = d.Monto
                })
                .ToList();

            if (!gasto.Detalles.Any())
            {
                ModelState.AddModelError("", "Debe ingresar al menos un detalle de gasto.");
            }

            // 3. Validar existencia de fondo y saldo
            var fondo = await _context.FondosMonetarios
                .FirstOrDefaultAsync(f => f.Id == gasto.FondoMonetarioId);

            if (fondo == null)
            {
                ModelState.AddModelError("FondoMonetarioId", "El fondo monetario no existe.");
            }
            else if (gasto.Detalles.Any()) // Solo validar saldo si hay detalles
            {
                decimal sumaDetalles = gasto.Detalles.Sum(d => d.Monto);
                if (fondo.SaldoActual < sumaDetalles)
                {
                    ModelState.AddModelError("", "El fondo seleccionado no tiene saldo suficiente.");
                }
            }

            // 4. Detectar sobregiro de presupuesto
            var userId = _userManager.GetUserId(User);
            var alertas = new List<string>();

            foreach (var detalleVm in gasto.Detalles)
            {
                if (!string.IsNullOrWhiteSpace(detalleVm.TipoGastoCodigo))
                {
                    var presupuesto = await _context.Presupuestos
                        .FirstOrDefaultAsync(p =>
                            p.UserId == userId &&
                            p.TipoGastoCodigo == detalleVm.TipoGastoCodigo &&
                            p.Mes == gasto.Fecha.Month &&
                            p.Anio == gasto.Fecha.Year);

                    if (presupuesto != null)
                    {
                        decimal nuevoEjecutado = presupuesto.MontoEjecutado + detalleVm.Monto;
                        if (nuevoEjecutado > presupuesto.MontoPresupuestado)
                        {
                            decimal exceso = nuevoEjecutado - presupuesto.MontoPresupuestado;
                            var tipoGasto = await _context.TiposGasto
                                .FirstOrDefaultAsync(t => t.Codigo == presupuesto.TipoGastoCodigo);

                            alertas.Add($"⚠️ {tipoGasto?.Nombre ?? presupuesto.TipoGastoCodigo}: Excede {exceso:C}");
                        }
                    }
                }
            }

            // 5. Validar modelo
            if (!ModelState.IsValid)
            {
                ViewBag.DebugErrores = ModelState
                    .Where(m => m.Value.Errors.Count > 0)
                    .Select(m => new { Campo = m.Key, Errores = m.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                CargarViewBags(gasto.FondoMonetarioId);
                return View(gasto);
            }

            // 6. Guardar en transacción
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 6.1 Crear encabezado
                var entidadGasto = new Gasto
                {
                    Fecha = gasto.Fecha,
                    FondoMonetarioId = gasto.FondoMonetarioId,
                    Observaciones = gasto.Observaciones,
                    Comercio = gasto.Comercio,
                    TipoDocumento = gasto.TipoDocumento,
                    NumeroDocumento = gasto.NumeroDocumento,
                    UserId = userId,
                    MontoTotal = gasto.Detalles.Sum(d => d.Monto)
                };
                entidadGasto.Detalles = null;
                _context.Gastos.Add(entidadGasto);
                await _context.SaveChangesAsync();

                // 6.2 Crear detalles con validación reforzada
                var detallesEntities = new List<DetalleGasto>();

                foreach (var detalleVm in gasto.Detalles)
                {
                    // Validación final de seguridad
                    if (!string.IsNullOrWhiteSpace(detalleVm.TipoGastoCodigo) && detalleVm.Monto > 0)
                    {
                        detallesEntities.Add(new DetalleGasto
                        {
                            GastoId = entidadGasto.Id,
                            TipoGastoCodigo = detalleVm.TipoGastoCodigo.Trim(),
                            Monto = detalleVm.Monto
                        });

                        // Actualizar presupuesto
                        var presupuesto = await _context.Presupuestos
                            .FirstOrDefaultAsync(p =>
                                p.UserId == userId &&
                                p.TipoGastoCodigo == detalleVm.TipoGastoCodigo &&
                                p.Mes == entidadGasto.Fecha.Month &&
                                p.Anio == entidadGasto.Fecha.Year);

                        if (presupuesto != null)
                        {
                            presupuesto.MontoEjecutado += detalleVm.Monto;
                        }
                    }
                }

                // Guardar todos los detalles en una sola operación
                _context.DetallesGasto.AddRange(detallesEntities);

                // 6.3 Actualizar saldo del fondo
                fondo.SaldoActual -= entidadGasto.MontoTotal;
                _context.Update(fondo);

                // 6.4 Guardar todos los cambios
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Mostrar alertas de sobregiro
                if (alertas.Any())
                {
                    TempData["AlertaSobregiro"] = string.Join("<br/>", alertas);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Mensaje de error más detallado
                var errorMessage = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nDetalles: {ex.InnerException.Message}";
                }

                ModelState.AddModelError("", errorMessage);

                CargarViewBags(gasto.FondoMonetarioId);
                return View(gasto);
            }
        }

        private void CargarViewBags(int? fondoId = null)
        {
            ViewBag.FondoMonetarioId = new SelectList(_context.FondosMonetarios, "Id", "Nombre", fondoId);
            ViewBag.TiposGasto = new SelectList(_context.TiposGasto, "Codigo", "Nombre");
            ViewBag.TiposDocumento = new List<SelectListItem>
            {
                new SelectListItem("Factura", "Factura"),
                new SelectListItem("Comprobante", "Comprobante"),
                new SelectListItem("Otro", "Otro")
            };
        }


        // GET: Gastos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var gasto = await _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.Detalles)
                    .ThenInclude(d => d.TipoGasto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (gasto == null) return NotFound();

            return View(gasto);
        }

        // GET: Gastos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var gasto = await _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.Detalles)
                    .ThenInclude(d => d.TipoGasto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (gasto == null) return NotFound();

            return View(gasto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var gasto = await _context.Gastos
                    .Include(g => g.Detalles)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (gasto == null) return NotFound();

                // Revertir presupuestos
                var userId = _userManager.GetUserId(User);
                foreach (var detalle in gasto.Detalles)
                {
                    var presupuesto = await _context.Presupuestos
                        .FirstOrDefaultAsync(p => p.UserId == userId &&
                                                 p.TipoGastoCodigo == detalle.TipoGastoCodigo &&
                                                 p.Mes == gasto.Fecha.Month &&
                                                 p.Anio == gasto.Fecha.Year);

                    if (presupuesto != null)
                    {
                        presupuesto.MontoEjecutado -= detalle.Monto;
                        _context.Update(presupuesto);
                    }
                }

                // Revertir saldo del fondo
                var fondo = await _context.FondosMonetarios.FindAsync(gasto.FondoMonetarioId);
                if (fondo != null)
                {
                    fondo.SaldoActual += gasto.MontoTotal;
                    _context.Update(fondo);
                }

                // Eliminar gasto y detalles
                _context.Gastos.Remove(gasto);
                await _context.SaveChangesAsync();

                transaction.Commit();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // GET: Gastos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var gasto = await _context.Gastos
                .Include(g => g.Detalles)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gasto == null) return NotFound();

            CargarViewBags(gasto.FondoMonetarioId);
            return View(gasto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Gasto gasto)
        {
            if (id != gasto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    // 1. Eliminar detalles antiguos
                    var detallesEliminar = await _context.DetallesGasto
                        .Where(d => d.GastoId == id)
                        .ToListAsync();
                    _context.DetallesGasto.RemoveRange(detallesEliminar);

                    // 2. Actualizar encabezado
                    _context.Update(gasto);

                    // 3. Actualizar detalles
                    foreach (var detalle in gasto.Detalles)
                    {
                        detalle.GastoId = gasto.Id;
                        _context.Add(detalle);
                    }

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    if (!GastoExists(gasto.Id))
                        return NotFound();
                    throw;
                }
            }

            CargarViewBags(gasto.FondoMonetarioId);
            return View(gasto);
        }

        private bool GastoExists(int id)
        {
            return _context.Gastos.Any(e => e.Id == id);
        }
    }
}
