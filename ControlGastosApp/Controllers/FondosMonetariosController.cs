using ControlGastosApp.Data;
using ControlGastosApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlGastosApp.Controllers
{
    [Authorize]
    public class FondosMonetariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FondosMonetariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FondosMonetarios
        public async Task<IActionResult> Index()
        {
            return View(await _context.FondosMonetarios.ToListAsync());
        }

        // GET: FondosMonetarios/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FondoMonetario fondoMonetario)
        {
            if (ModelState.IsValid)
            {
                fondoMonetario.SaldoActual = 0; // Saldo inicial
                _context.Add(fondoMonetario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fondoMonetario);
        }

        // GET: FondosMonetarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var fondoMonetario = await _context.FondosMonetarios.FindAsync(id);
            if (fondoMonetario == null) return NotFound();

            return View(fondoMonetario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Tipo,SaldoActual")] FondoMonetario fondoMonetario)
        {
            if (id != fondoMonetario.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fondoMonetario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FondoMonetarioExists(fondoMonetario.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fondoMonetario);
        }

        // GET: FondosMonetarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var fondoMonetario = await _context.FondosMonetarios
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fondoMonetario == null) return NotFound();

            return View(fondoMonetario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fondoMonetario = await _context.FondosMonetarios.FindAsync(id);
            _context.FondosMonetarios.Remove(fondoMonetario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FondoMonetarioExists(int id)
        {
            return _context.FondosMonetarios.Any(e => e.Id == id);
        }
    }
}
