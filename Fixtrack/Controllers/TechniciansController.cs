using System.Security.Cryptography;
using Fixtrack.Data;
using Fixtrack.Models;
using Fixtrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixtrack.Controllers;


[Authorize(Roles = "Receptionist")]
public class TechniciansController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TechniciansController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Technicians
    public async Task<IActionResult> Index()
    {
        var technicians = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.RepairJobs)
            .ToListAsync();

        // FullName lives on the user, so sort in memory.
        return View(technicians.OrderBy(t => t.User?.FullName).ToList());
    }

    // GET: Technicians/Create
    public IActionResult Create() => View(new TechnicianRegistrationViewModel());

    // POST: Technicians/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TechnicianRegistrationViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email),
                "An account with this email already exists.");
            return View(model);
        }

        var password = GeneratePassword();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, DbSeeder.TechnicianRole);

        try
        {
            _context.Technicians.Add(new Technician
            {
                UserId = user.Id,
                Specialization = model.Specialization,
                Status = TechnicianStatus.Available,
                BaseLocation = model.BaseLocation?.Trim()
            });
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Don't leave a login with no technician profile behind.
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty,
                "Could not save the technician. Nothing was created - please try again.");
            return View(model);
        }

        // Returned directly (no redirect) so the password never goes into
        // TempData or a URL. Refreshing will not show it again.
        return View("Credentials", new TechnicianCredentialsViewModel
        {
            FullName = user.FullName,
            Email = email,
            TemporaryPassword = password
        });
    }

    // GET: Technicians/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var technician = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (technician == null) return NotFound();
        return View(technician);
    }

    // POST: Technicians/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id)
    {
        var technician = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (technician == null) return NotFound();

        // Whitelist: login details and coordinates cannot be changed by
        // posting extra fields.
        if (await TryUpdateModelAsync(technician, "",
                t => t.Specialization, t => t.Status, t => t.BaseLocation))
        {
            await _context.SaveChangesAsync();
            TempData["Status"] = $"{technician.User?.FullName} updated.";
            return RedirectToAction(nameof(Index));
        }

        return View(technician);
    }

    // GET: Technicians/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var technician = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.RepairJobs)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (technician == null) return NotFound();
        return View(technician);
    }

    // POST: Technicians/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var technician = await _context.Technicians
            .Include(t => t.User)
            .Include(t => t.RepairJobs)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (technician == null) return RedirectToAction(nameof(Index));

        // Silently orphaning live jobs would leave repairs with nobody on them.
        var openJobs = technician.RepairJobs.Count(j => !j.IsClosed);
        if (openJobs > 0)
        {
            TempData["Warning"] =
                $"{technician.User?.FullName} still has {openJobs} open job(s). "
              + "Reassign them before removing this technician.";
            return RedirectToAction(nameof(Index));
        }

        var user = technician.User;

        // Profile goes; the account is kept (past jobs record who did them)
        // but is deactivated so the login stops working - Login already
        // refuses inactive accounts.
        _context.Technicians.Remove(technician);
        await _context.SaveChangesAsync();

        if (user is not null)
        {
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            if (await _userManager.IsInRoleAsync(user, DbSeeder.TechnicianRole))
                await _userManager.RemoveFromRoleAsync(user, DbSeeder.TechnicianRole);
        }

        TempData["Status"] = "Technician removed and login deactivated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>12 chars with upper, lower, digit and symbol; no look-alikes.</summary>
    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "@#$%&*!";
        var all = upper + lower + digits + symbols;

        var chars = new List<char>
        {
            Pick(upper), Pick(lower), Pick(digits), Pick(symbols)
        };
        while (chars.Count < 12) chars.Add(Pick(all));

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());

        static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
    }
}