using Fixtrack.Data;
using Fixtrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixtrack.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBellViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId((System.Security.Claims.ClaimsPrincipal)UserClaimsPrincipal);

        if (userId is null)
        {
            return View(new List<Notification>());
        }

        var recent = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(8)
            .ToListAsync();

        return View(recent);
    }

    private System.Security.Claims.ClaimsPrincipal UserClaimsPrincipal => (HttpContext.User);
}

