using System.ComponentModel.DataAnnotations;

namespace Fixtrack.Models;


public class NotificationLog
{
    public int Id { get; set; }

    [Display(Name = "Repair Job")]
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    /// <summary>The receptionist who made contact.</summary>
    [StringLength(450)]
    [Display(Name = "Receptionist")]
    public string? ReceptionistId { get; set; }

    [StringLength(1000)]
    [Display(Name = "Note")]
    public string? Note { get; set; }

    /// <summary>Outcome of the contact attempt.</summary>
    public int Status { get; set; }

    [Display(Name = "Contacted On")]
    public DateTime ContactedAt { get; set; } = DateTime.Now;
}