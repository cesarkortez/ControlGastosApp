using ControlGastosApp.Data;
using ControlGastosApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlGastosApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TiposGastoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TiposGastoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TiposGasto
        public async Task<IActionResult> Index()
        {
            return View(await _context.TiposGasto.ToListAsync());
        }

        // GET: TiposGasto/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Descripcion")] TipoGasto tipoGasto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Generar código automático
                    var lastTipo = await _context.TiposGasto
                        .OrderByDescending(t => t.Codigo)
                        .FirstOrDefaultAsync();

                    int nextNumber = 1;
                    if (lastTipo != null && int.TryParse(lastTipo.Codigo, out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }

                    tipoGasto.Codigo = nextNumber.ToString("D4");

                    _context.Add(tipoGasto);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
            }

            // Log o inspección de errores (opcional)
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            return View(tipoGasto);
        }

        // GET: TiposGasto/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var tipoGasto = await _context.TiposGasto.FindAsync(id);
            if (tipoGasto == null) return NotFound();

            return View(tipoGasto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Codigo,Nombre,Descripcion")] TipoGasto tipoGasto)
        {
            if (id != tipoGasto.Codigo) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tipoGasto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TipoGastoExists(tipoGasto.Codigo))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tipoGasto);
        }

        // GET: TiposGasto/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var tipoGasto = await _context.TiposGasto
                .FirstOrDefaultAsync(m => m.Codigo == id);

            if (tipoGasto == null) return NotFound();

            return View(tipoGasto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var tipoGasto = await _context.TiposGasto.FindAsync(id);
            _context.TiposGasto.Remove(tipoGasto);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TipoGastoExists(string id)
        {
            return _context.TiposGasto.Any(e => e.Codigo == id);
        }
    }
}
