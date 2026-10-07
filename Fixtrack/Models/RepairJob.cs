using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fixtrack.Models;

/// <summary>Where a job sits in the workflow.</summary>
public enum RepairStatus
{
    [Display(Name = "Received")]          Received = 1,
    [Display(Name = "Assigned")]          Assigned = 2,
    [Display(Name = "Diagnosing")]        Diagnosing = 3,
    [Display(Name = "Awaiting Approval")] AwaitingApproval = 4,
    [Display(Name = "Repairing")]         Repairing = 5,
    [Display(Name = "Awaiting Parts")]    AwaitingParts = 6,
    [Display(Name = "Ready for Pickup")]  ReadyForPickup = 7,
    [Display(Name = "Completed")]         Completed = 8,
    [Display(Name = "Cancelled")]         Cancelled = 9
}

/// <summary>
/// One repair from intake to collection. Notes, parts, payments,
/// contact attempts and complaints all hang off this.
/// </summary>
public class RepairJob
{
    public int Id { get; set; }

    /// <summary>Reference quoted to the customer, e.g. RF-1025.</summary>
    [StringLength(20)]
    [Display(Name = "Repair ID")]
    public string JobNumber { get; set; } = string.Empty;

    // ----- who and what -----
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Device")]
    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    /// <summary>Null means no suitable technician was free - the job waits.</summary>
    [Display(Name = "Technician")]
    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    /// <summary>The receptionist who took the device in.</summary>
    [StringLength(450)]
    [Display(Name = "Received By")]
    public string? ReceivedByUserId { get; set; }

    // ----- intake -----
    [Required(ErrorMessage = "Describe the problem the customer reported.")]
    [StringLength(1000)]
    [Display(Name = "Reported Problem")]
    public string ReportedProblem { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Device Condition")]
    public string? DeviceCondition { get; set; }

    /// <summary>Drives automatic assignment. Defaulted from the device type
    /// but the receptionist can override it.</summary>
    [Display(Name = "Required Specialization")]
    public Specialization RequiredSpecialization { get; set; } = Specialization.General;

    [StringLength(1000)]
    [Display(Name = "Additional Notes")]
    public string? AdditionalNotes { get; set; }

    // ----- technician findings -----
    [StringLength(2000)]
    [Display(Name = "Diagnosis")]
    public string? Diagnosis { get; set; }

    [StringLength(2000)]
    [Display(Name = "Work Performed")]
    public string? WorkPerformed { get; set; }

    // ----- state -----
    public RepairStatus Status { get; set; } = RepairStatus.Received;

    // ----- money -----
    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    [Display(Name = "Estimated Cost")]
    public decimal? EstimatedCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    [Display(Name = "Final Cost")]
    public decimal? FinalCost { get; set; }

    // ----- customer approval -----
    // null = not asked, true = approved, false = rejected
    [Display(Name = "Customer Approved")]
    public bool? CustomerApproved { get; set; }

    [StringLength(500)]
    [Display(Name = "Approval Notes")]
    public string? ApprovalNotes { get; set; }

    // ----- timeline -----
    [Display(Name = "Received On")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Assigned On")]
    public DateTime? AssignedAt { get; set; }

    [Display(Name = "Diagnosed On")]
    public DateTime? DiagnosedAt { get; set; }

    [Display(Name = "Approved On")]
    public DateTime? ApprovedAt { get; set; }

    [Display(Name = "Ready On")]
    public DateTime? ReadyAt { get; set; }

    [Display(Name = "Completed On")]
    public DateTime? CompletedAt { get; set; }

    // ----- children -----
    public ICollection<RepairNote> Notes { get; set; } = new List<RepairNote>();
    public ICollection<PartUsed> PartsUsed { get; set; } = new List<PartUsed>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    public ICollection<NotificationLog> ContactHistory { get; set; } = new List<NotificationLog>();

    // ----- helpers (not columns) -----
    [NotMapped]
    public bool IsClosed => Status is RepairStatus.Completed or RepairStatus.Cancelled;

    [NotMapped]
    public decimal AmountPaid => Payments?.Sum(p => p.Amount) ?? 0m;

    [NotMapped]
    public decimal Balance => (FinalCost ?? EstimatedCost ?? 0m) - AmountPaid;
}
