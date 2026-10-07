using System.ComponentModel.DataAnnotations;

namespace Fixtrack.Models;

/// <summary>
/// A message for one specific person about something that happened on a
/// job. Separate from RepairNote: a note is part of the job's permanent
/// history and everyone sees it; a notification is personal and gets
/// dismissed once read.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [StringLength(300)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Which job this is about, so the notification can link straight to it.</summary>
    public int? RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}