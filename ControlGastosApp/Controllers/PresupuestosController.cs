using ControlGastosApp.Data;
using ControlGastosApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ControlGastosApp.Controllers
{
    [Authorize]
    public class PresupuestosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PresupuestosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Presupuestos
        public async Task<IActionResult> Index()
        {
            // Traer todos los presupuestos del usuario actual, por ejemplo:
            var userId = _userManager.GetUserId(User);
            var presupuestos = await _context.Presupuestos
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var dictTipoNombres = new Dictionary<int, string>();
            var dictFondoNombres = new Dictionary<int, string>();

            // Cargar todos los fondos en un diccionario <Id, Nombre>
            var fondosAll = await _context.FondosMonetarios
                .ToDictionaryAsync(f => f.Id, f => f.Nombre);

            var tiposAll = await _context.TiposGasto
                .ToDictionaryAsync(t => t.Codigo, t => t.Nombre);

            foreach (var pres in presupuestos)
            {
                // FondoMonetarioNombre
                if (fondosAll.TryGetValue(pres.FondoMonetarioId, out var fundNombre))
                    dictFondoNombres[pres.Id] = fundNombre;
                else
                    dictFondoNombres[pres.Id] = $"(ID {pres.FondoMonetarioId})";

                // TipoGastoNombre
                if (tiposAll.TryGetValue(pres.TipoGastoCodigo, out var tipoNombre))
                    dictTipoNombres[pres.Id] = tipoNombre;
                else
                    dictTipoNombres[pres.Id] = pres.TipoGastoCodigo;
            }

            ViewBag.DictTipoNombres = dictTipoNombres;
            ViewBag.DictFondoNombres = dictFondoNombres;

            return View(presupuestos);
        }

        // GET: Presupuestos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pres = await _context.Presupuestos.FindAsync(id.Value);
            if (pres == null) return NotFound();

            // Cargar nombre del fondo
            var fondo = await _context.FondosMonetarios.FindAsync(pres.FondoMonetarioId);
            ViewBag.FondoNombre = fondo?.Nombre ?? $"(ID {pres.FondoMonetarioId})";

            // Cargar nombre del tipo de gasto
            var tipo = await _context.TiposGasto.FindAsync(pres.TipoGastoCodigo);
            ViewBag.TipoGastoNombre = tipo?.Nombre ?? pres.TipoGastoCodigo;

            return View(pres);
        }

        // GET: Presupuesto/Create
        public IActionResult Create()
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // Obtener los meses con nombres
                var meses = Enumerable.Range(1, 12)
                    .Select(i => new SelectListItem
                    {
                        Value = i.ToString(),
                        Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
                    })
                    .ToList();

                // Años desde 2025 hasta 2030
                var anios = Enumerable.Range(2025, 6)
                    .Select(a => new SelectListItem
                    {
                        Value = a.ToString(),
                        Text = a.ToString()
                    })
                    .ToList();

                // Tipos de gasto y fondos monetarios ordenados
                var tiposGasto = _context.TiposGasto
                    .OrderBy(t => t.Nombre)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Codigo,
                        Text = t.Nombre
                    })
                    .ToList();

                var fondosMonetarios = _context.FondosMonetarios
                    .OrderBy(f => f.Nombre)
                    .Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = f.Nombre
                    })
                    .ToList();

                ViewBag.Meses = new SelectList(meses, "Value", "Text");
                ViewBag.Anios = new SelectList(anios, "Value", "Text");
                ViewBag.TiposGasto = new SelectList(tiposGasto, "Value", "Text");
                ViewBag.FondosMonetarios = new SelectList(fondosMonetarios, "Value", "Text");

                // Establecer valores por defecto
                var model = new Presupuesto
                {
                    Anio = DateTime.Now.Year,
                    Mes = DateTime.Now.Month,
                    UserId = userId
                };

                return View(model);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error al cargar la vista de creación de presupuesto");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Presupuesto presupuesto)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var fondoM = await _context.FondosMonetarios.FirstOrDefaultAsync(p => p.Id == presupuesto.FondoMonetarioId);
                presupuesto.UserId = userId;
                presupuesto.MontoEjecutado = 0;
                presupuesto.FondoMonetario = fondoM;

                // Validar si ya existe un presupuesto para este tipo de gasto, mes y año
                var existePresupuesto = await _context.Presupuestos
                    .AnyAsync(p => p.UserId == userId &&
                                 p.TipoGastoCodigo == presupuesto.TipoGastoCodigo &&
                                 p.Mes == presupuesto.Mes &&
                                 p.Anio == presupuesto.Anio);

               

                if (existePresupuesto)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe un presupuesto para este tipo de gasto en el mes y año seleccionados");
                }

                foreach (var entry in ModelState)
                {
                    var key = entry.Key;
                    var errors = entry.Value.Errors;
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"Error en el campo '{key}': {error.ErrorMessage}");
                        // O usa logging si estás en producción:
                        // _logger.LogWarning("Error en el campo '{Key}': {Error}", key, error.ErrorMessage);
                    }
                }

                ModelState.Clear();
                TryValidateModel(presupuesto);

                if (ModelState.IsValid)
                {
                    _context.Add(presupuesto);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Presupuesto creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }

                // Recargar los dropdowns si hay errores
                await CargarDropdowns();
                return View(presupuesto);
            }
            catch (Exception ex)
            {
                    //_logger.LogError(ex, "Error al crear presupuesto");
                TempData["ErrorMessage"] = "Ocurrió un error al crear el presupuesto";
                await CargarDropdowns();
                return View(presupuesto);
            }
        }

        private async Task CargarDropdowns()
        {
            var meses = Enumerable.Range(1, 12)
                .Select(i => new SelectListItem
                {
                    Value = i.ToString(),
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
                })
                .ToList();

            var anios = Enumerable.Range(2025, 6)
                .Select(a => new SelectListItem
                {
                    Value = a.ToString(),
                    Text = a.ToString()
                })
                .ToList();

            var tiposGasto = await _context.TiposGasto
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.Codigo,
                    Text = t.Nombre
                })
                .ToListAsync();

            var fondosMonetarios = await _context.FondosMonetarios
                .OrderBy(f => f.Nombre)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Nombre
                })
                .ToListAsync();

            ViewBag.Meses = new SelectList(meses, "Value", "Text");
            ViewBag.Anios = new SelectList(anios, "Value", "Text");
            ViewBag.TiposGasto = new SelectList(tiposGasto, "Value", "Text");
            ViewBag.FondosMonetarios = new SelectList(fondosMonetarios, "Value", "Text");
        }


        // GET: Presupuestos/Edit/5
        // GET: Presupuestos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                var presupuesto = await _context.Presupuestos
                    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

                if (presupuesto == null)
                {
                    return NotFound();
                }

                await CargarDropdowns(presupuesto);
                return View(presupuesto);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error al cargar vista de edición de presupuesto");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar el formulario de edición";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Presupuestos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Presupuesto presupuesto)
        {
            if (id != presupuesto.Id)
            {
                return NotFound();
            }

            try
            {
                var userId = _userManager.GetUserId(User);

                // Validar que el presupuesto pertenece al usuario
                var presupuestoExistente = await _context.Presupuestos
                    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

                if (presupuestoExistente == null)
                {
                    return NotFound();
                }

                // Validar si ya existe otro presupuesto para este tipo de gasto, mes y año
                var existeDuplicado = await _context.Presupuestos
                    .AnyAsync(p => p.UserId == userId &&
                                 p.Id != id &&
                                 p.TipoGastoCodigo == presupuesto.TipoGastoCodigo &&
                                 p.Mes == presupuesto.Mes &&
                                 p.Anio == presupuesto.Anio);

                if (existeDuplicado)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe un presupuesto para este tipo de gasto en el mes y año seleccionados");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        // Actualizar solo los campos editables (no el MontoEjecutado ni UserId)
                        presupuestoExistente.TipoGastoCodigo = presupuesto.TipoGastoCodigo;
                        presupuestoExistente.Mes = presupuesto.Mes;
                        presupuestoExistente.Anio = presupuesto.Anio;
                        presupuestoExistente.MontoPresupuestado = presupuesto.MontoPresupuestado;
                        presupuestoExistente.FondoMonetarioId = presupuesto.FondoMonetarioId;

                        _context.Update(presupuestoExistente);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Presupuesto actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        //if (!PresupuestoExists(presupuesto.Id))
                        //{
                        //    return NotFound();
                        //}
                        //_logger.LogError(ex, "Error de concurrencia al editar presupuesto");
                        throw;
                    }
                }

                await CargarDropdowns(presupuesto);
                return View(presupuesto);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error al editar presupuesto");
                TempData["ErrorMessage"] = "Ocurrió un error al actualizar el presupuesto";
                await CargarDropdowns(presupuesto);
                return View(presupuesto);
            }
        }

        private async Task CargarDropdowns(Presupuesto presupuesto = null)
        {
            // Obtener los meses con nombres
            var meses = Enumerable.Range(1, 12)
                .Select(i => new SelectListItem
                {
                    Value = i.ToString(),
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i),
                    Selected = presupuesto?.Mes == i
                })
                .ToList();

            // Años desde 2025 hasta 2030
            var anios = Enumerable.Range(2025, 6)
                .Select(a => new SelectListItem
                {
                    Value = a.ToString(),
                    Text = a.ToString(),
                    Selected = presupuesto?.Anio == a
                })
                .ToList();

            // Tipos de gasto y fondos monetarios ordenados
            var tiposGasto = await _context.TiposGasto
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem
                {
                    Value = t.Codigo,
                    Text = t.Nombre,
                    Selected = presupuesto.TipoGastoCodigo == t.Codigo
                })
                .ToListAsync();

            var fondosMonetarios = await _context.FondosMonetarios
                .OrderBy(f => f.Nombre)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Nombre,
                    Selected = presupuesto.FondoMonetarioId == f.Id
                })
                .ToListAsync();

            ViewBag.Meses = new SelectList(meses, "Value", "Text", presupuesto?.Mes);
            ViewBag.Anios = new SelectList(anios, "Value", "Text", presupuesto?.Anio);
            ViewBag.TiposGasto = new SelectList(tiposGasto, "Value", "Text", presupuesto?.TipoGastoCodigo);
            ViewBag.FondosMonetarios = new SelectList(fondosMonetarios, "Value", "Text", presupuesto?.FondoMonetarioId);
        }

        private bool PresupuestoExists(int id)
        {
            return _context.Presupuestos.Any(e => e.Id == id);
        }

        // GET: Presupuestos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pres = await _context.Presupuestos.FindAsync(id.Value);
            if (pres == null) return NotFound();

            // Cargar nombre legible para Delete
            var fondo = await _context.FondosMonetarios.FindAsync(pres.FondoMonetarioId);
            ViewBag.FondoNombre = fondo?.Nombre ?? $"(ID {pres.FondoMonetarioId})";

            var tipo = await _context.TiposGasto.FindAsync(pres.TipoGastoCodigo);
            ViewBag.TipoGastoNombre = tipo?.Nombre ?? pres.TipoGastoCodigo;

            return View(pres);
        }

        // POST: Presupuestos/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pres = await _context.Presupuestos.FindAsync(id);
            if (pres != null)
            {
                _context.Presupuestos.Remove(pres);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        //private bool PresupuestoExists(int id)
        //    => _context.Presupuestos.Any(e => e.Id == id);
    }
}
