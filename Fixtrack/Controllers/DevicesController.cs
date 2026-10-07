using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fixtrack.Data;
using Fixtrack.Models;

namespace Fixtrack.Controllers
{
    public class DevicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Devices
        public async Task<IActionResult> Index()
        {
            var devices = await _context.Devices
                .Include(d => d.Customer)
                .OrderBy(d => d.Customer!.LastName)
                .ThenBy(d => d.Brand)
                .ToListAsync();

            return View(devices);
        }

        // GET: Devices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Devices
                .Include(d => d.Customer)
                .Include(d => d.RepairJobs).ThenInclude(j => j.Technician).ThenInclude(t => t!.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }
    }
}