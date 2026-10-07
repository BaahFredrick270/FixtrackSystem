using System.ComponentModel.DataAnnotations;

namespace Fixtrack.Models;

/// <summary>
/// A dated note added by a technician while working. Append-only:
/// notes are never edited, so the history stays truthful.
/// </summary>
public class RepairNote
{
    public int Id { get; set; }

    [Display(Name = "Repair")]
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    /// <summary>Who wrote it. Null if written by a receptionist rather than a technician.</summary>
    [Display(Name = "Technician")]
    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    [Required(ErrorMessage = "The note cannot be empty.")]
    [StringLength(2000)]
    [Display(Name = "Note")]
    public string Note { get; set; } = string.Empty;

    [Display(Name = "Added")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
