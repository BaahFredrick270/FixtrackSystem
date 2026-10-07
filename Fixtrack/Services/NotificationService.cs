using Fixtrack.Data;
using Fixtrack.Models;

namespace Fixtrack.Services;

/// <summary>
/// One place that knows how to create a notification, so every controller
/// action that triggers one does it the same way.
/// </summary>
public class NotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task NotifyAsync(string? userId, string message, int? repairJobId = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            RepairJobId = repairJobId,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }
}