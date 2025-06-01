using ControlGastosApp.Data;
using ControlGastosApp.Models;
using ControlGastosApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ControlGastosApp.Controllers
{
    [Authorize]
    public class ConsultasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ConsultasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Consultas/Movimientos
        public IActionResult Movimientos()
        {
            // Fechas por defecto (últimos 30 días)
            var fechaFin = DateTime.Today;
            var fechaInicio = fechaFin.AddDays(-30);

            ViewData["FechaInicio"] = fechaInicio.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Movimientos(DateTime fechaInicio, DateTime fechaFin)
        {
            var userId = _userManager.GetUserId(User);
            ViewData["FechaInicio"] = fechaInicio.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin.ToString("yyyy-MM-dd");

            // Obtener gastos del usuario
            var gastos = await _context.Gastos
                .Include(g => g.FondoMonetario)
                .Include(g => g.Detalles)
                    .ThenInclude(d => d.TipoGasto)
                .Where(g => g.Fecha >= fechaInicio &&
                            g.Fecha <= fechaFin &&
                            g.UserId == userId)
                .ToListAsync();

            // Obtener depósitos del usuario
            var depositos = await _context.Depositos
                .Include(d => d.FondoMonetario)
                .Where(d => d.Fecha >= fechaInicio &&
                            d.Fecha <= fechaFin &&
                            d.UserId == userId)
                .ToListAsync();

            var movimientos = new List<MovimientoViewModel>();

            // Convertir gastos a ViewModel (un movimiento por detalle)
            foreach (var gasto in gastos)
            {
                foreach (var detalle in gasto.Detalles)
                {
                    movimientos.Add(new MovimientoViewModel
                    {
                        Fecha = gasto.Fecha,
                        Tipo = "Gasto",
                        FondoMonetario = gasto.FondoMonetario?.Nombre,
                        Descripcion = $"{gasto.Comercio} - {detalle.TipoGasto?.Nombre}",
                        Monto = -detalle.Monto,
                        Referencia = $"{gasto.TipoDocumento} {gasto.NumeroDocumento}"
                    });
                }
            }

            // Convertir depósitos a ViewModel
            foreach (var deposito in depositos)
            {
                movimientos.Add(new MovimientoViewModel
                {
                    Fecha = deposito.Fecha,
                    Tipo = "Depósito",
                    FondoMonetario = deposito.FondoMonetario?.Nombre,
                    Descripcion = "Depósito",
                    Monto = deposito.Monto,
                    Referencia = "DEP" + deposito.Id.ToString("D5")
                });
            }

            // Ordenar por fecha
            movimientos = movimientos.OrderByDescending(m => m.Fecha).ToList();

            return View("ResultadosMovimientos", movimientos);
        }

        // GET: Consultas/GraficoComparativo
        public IActionResult GraficoComparativo()
        {
            // Fechas por defecto (últimos 3 meses)
            var fechaFin = DateTime.Today;
            var fechaInicio = fechaFin.AddMonths(-3);

            ViewData["FechaInicio"] = fechaInicio.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GraficoComparativo(DateTime fechaInicio, DateTime fechaFin)
        {
            var userId = _userManager.GetUserId(User);
            ViewData["FechaInicio"] = fechaInicio.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin.ToString("yyyy-MM-dd");

            // Consulta de presupuestos agrupados
            var presupuestosQuery = _context.Presupuestos
                .Where(p => p.UserId == userId);

            // Filtro por rango de fechas
            if (fechaInicio.Year == fechaFin.Year)
            {
                presupuestosQuery = presupuestosQuery
                    .Where(p => p.Anio == fechaInicio.Year &&
                                p.Mes >= fechaInicio.Month &&
                                p.Mes <= fechaFin.Month);
            }
            else
            {
                presupuestosQuery = presupuestosQuery
                    .Where(p => (p.Anio == fechaInicio.Year && p.Mes >= fechaInicio.Month) ||
                                (p.Anio > fechaInicio.Year && p.Anio < fechaFin.Year) ||
                                (p.Anio == fechaFin.Year && p.Mes <= fechaFin.Month));
            }

            var presupuestos = await presupuestosQuery
                .GroupBy(p => p.TipoGastoCodigo)
                .Select(g => new {
                    TipoGastoCodigo = g.Key,
                    Total = g.Sum(p => p.MontoPresupuestado)
                })
                .ToListAsync();

            // Consulta de gastos ejecutados agrupados
            var ejecutados = await _context.DetallesGasto
                .Include(d => d.Gasto)
                .Where(d => d.Gasto.Fecha >= fechaInicio &&
                           d.Gasto.Fecha <= fechaFin &&
                           d.Gasto.UserId == userId)
                .GroupBy(d => d.TipoGastoCodigo)
                .Select(g => new {
                    TipoGastoCodigo = g.Key,
                    Total = g.Sum(d => d.Monto)
                })
                .ToListAsync();

            // Obtener nombres de tipos de gasto
            var tiposGasto = await _context.TiposGasto
                .Where(t => presupuestos.Select(p => p.TipoGastoCodigo).Contains(t.Codigo) ||
                           ejecutados.Select(e => e.TipoGastoCodigo).Contains(t.Codigo))
                .ToListAsync();

            var datos = tiposGasto.Select(t => new GraficoComparativoViewModel
            {
                TipoGasto = t.Nombre,
                Presupuestado = presupuestos.FirstOrDefault(p => p.TipoGastoCodigo == t.Codigo)?.Total ?? 0,
                Ejecutado = ejecutados.FirstOrDefault(e => e.TipoGastoCodigo == t.Codigo)?.Total ?? 0
            }).OrderByDescending(d => d.Ejecutado).ToList();

            return View("ResultadoGraficoComparativo", datos);
        }
    }
}