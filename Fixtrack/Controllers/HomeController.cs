using System.Diagnostics;
using Fixtrack.Data;
using Fixtrack.Models;
using Fixtrack.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixtrack.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return View(new HomeDashboardViewModel());
        }

        var user = await _userManager.GetUserAsync(User);
        var roles = user is null
            ? new List<string>()
            : (await _userManager.GetRolesAsync(user)).ToList();

        var isTechnician = roles.Contains("Technician");
        var isReceptionist = roles.Contains("Receptionist");

        // A technician's own dashboard is scoped to their work; a
        // receptionist (or anyone holding both roles) gets the shop-wide
        // view, since they need the full picture to triage intake.
        if (isTechnician && !isReceptionist)
        {
            return await BuildTechnicianDashboardAsync(user, roles);
        }

        return await BuildReceptionistDashboardAsync(user, roles);
    }

    private async Task<IActionResult> BuildReceptionistDashboardAsync(ApplicationUser? user, List<string> roles)
    {
        var closed = new[] { RepairStatus.Completed, RepairStatus.Cancelled };

        var model = new HomeDashboardViewModel
        {
            StaffName = user?.FullName ?? "there",
            Role = roles.FirstOrDefault() ?? "Staff",

            CustomerCount = await _context.Customers.CountAsync(),
            DeviceCount = await _context.Devices.CountAsync(),

            OpenJobs = await _context.RepairJobs
                .CountAsync(j => !closed.Contains(j.Status)),

            UnassignedJobs = await _context.RepairJobs
                .CountAsync(j => j.TechnicianId == null && !closed.Contains(j.Status)),

            AwaitingApproval = await _context.RepairJobs
                .CountAsync(j => j.Status == RepairStatus.AwaitingApproval),

            ReadyForPickup = await _context.RepairJobs
                .CountAsync(j => j.Status == RepairStatus.ReadyForPickup)
        };

        return View(model);
    }

    private async Task<IActionResult> BuildTechnicianDashboardAsync(ApplicationUser? user, List<string> roles)
    {
        var technician = user is null
            ? null
            : await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == user.Id);

        var model = new TechnicianDashboardViewModel
        {
            StaffName = user?.FullName ?? "there",
            Role = roles.FirstOrDefault() ?? "Technician",
            Specialization = technician?.Specialization.ToString() ?? "—",
            Status = technician?.Status.ToString() ?? "—"
        };

        if (technician is not null)
        {
            var jobs = _context.RepairJobs.Where(j => j.TechnicianId == technician.Id);

            model.AssignedCount = await jobs.CountAsync(j =>
                j.Status != RepairStatus.Completed && j.Status != RepairStatus.Cancelled);

            model.NeedsDiagnosisCount = await jobs.CountAsync(j =>
                j.Status == RepairStatus.Assigned || j.Status == RepairStatus.Diagnosing);

            model.RepairingCount = await jobs.CountAsync(j =>
                j.Status == RepairStatus.Repairing || j.Status == RepairStatus.AwaitingParts);

            model.CompletedCount = await jobs.CountAsync(j => j.Status == RepairStatus.Completed);
        }

        return View("TechnicianIndex", model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}