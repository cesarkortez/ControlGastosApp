using ControlGastosApp.Data;
using ControlGastosApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlGastosApp.Controllers
{
    [Authorize]
    public class DepositosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepositosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Depositos
        public async Task<IActionResult> Index()
        {
            var depositos = await _context.Depositos
                .Include(d => d.FondoMonetario)  // Incluir la relación con FondoMonetario
                .OrderByDescending(d => d.Fecha)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            return View(depositos);
        }

        // GET: Depositos/Create
        public IActionResult Create()
        {
            ViewData["FondoMonetarioId"] = new SelectList(_context.FondosMonetarios, "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Fecha,FondoMonetarioId,Monto")] Deposito deposito)
        {
            var fondoM = await _context.FondosMonetarios.FirstOrDefaultAsync(p => p.Id == deposito.FondoMonetarioId);
            deposito.FondoMonetario = fondoM;
           
            ModelState.Clear();
            TryValidateModel(deposito);

            if (ModelState.IsValid)
            {
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    _context.Add(deposito);
                    await _context.SaveChangesAsync();

                    // Actualizar saldo del fondo
                    var fondo = await _context.FondosMonetarios.FindAsync(deposito.FondoMonetarioId);
                    if (fondo != null)
                    {
                        fondo.SaldoActual += deposito.Monto;
                        _context.Update(fondo);
                        await _context.SaveChangesAsync();
                    }

                    transaction.Commit();
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            ViewData["FondoMonetarioId"] = new SelectList(_context.FondosMonetarios, "Id", "Nombre", deposito.FondoMonetarioId);
            return View(deposito);
        }

        [HttpGet]
        public async Task<IActionResult> GetSaldoActual(int id)
        {
            var fondo = await _context.FondosMonetarios.FindAsync(id);
            if (fondo == null)
            {
                return NotFound();
            }

            return Json(new { saldoActual = fondo.SaldoActual });
        }

        // GET: Depositos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var deposito = await _context.Depositos
                .Include(d => d.FondoMonetario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (deposito == null) return NotFound();

            return View(deposito);
        }

        // GET: Depositos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var deposito = await _context.Depositos
                .Include(d => d.FondoMonetario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (deposito == null) return NotFound();

            return View(deposito);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var deposito = await _context.Depositos.FindAsync(id);
                if (deposito == null) return NotFound();

                // Revertir saldo del fondo
                var fondo = await _context.FondosMonetarios.FindAsync(deposito.FondoMonetarioId);
                if (fondo != null)
                {
                    fondo.SaldoActual -= deposito.Monto;
                    _context.Update(fondo);
                }

                _context.Depositos.Remove(deposito);
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
    }
}
