using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fixtrack.Models;

public enum ComplaintStatus
{
    [Display(Name = "Submitted")]           Submitted = 1,
    [Display(Name = "Under Review")]        UnderReview = 2,
    [Display(Name = "Approved for Rework")] ApprovedForRework = 3,
    [Display(Name = "In Progress")]         InProgress = 4,
    [Display(Name = "Resolved")]            Resolved = 5,
    [Display(Name = "Rejected")]            Rejected = 6,
    [Display(Name = "Closed")]              Closed = 7
}

/// <summary>
/// Raised when a customer returns because the problem was not properly
/// fixed. If rework is approved the repair goes back to a technician.
/// </summary>
public class Complaint
{
    public int Id { get; set; }

    [Display(Name = "Repair")]
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    [Required(ErrorMessage = "Describe what the customer reported.")]
    [StringLength(2000)]
    [Display(Name = "Complaint")]
    public string Description { get; set; } = string.Empty;

    public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;

    /// <summary>What was done about it. Filled in when the complaint closes.</summary>
    [StringLength(2000)]
    public string? Resolution { get; set; }

    /// <summary>The receptionist who logged it.</summary>
    [StringLength(450)]
    [Display(Name = "Logged By")]
    public string? LoggedByUserId { get; set; }

    [Display(Name = "Raised On")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Resolved On")]
    public DateTime? ResolvedAt { get; set; }

    [NotMapped]
    public bool IsOpen => Status is not (ComplaintStatus.Resolved
                                      or ComplaintStatus.Rejected
                                      or ComplaintStatus.Closed);
}
