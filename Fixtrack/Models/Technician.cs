using System.ComponentModel.DataAnnotations;

namespace Fixtrack.Models;

/// <summary>What a technician is trained to repair.</summary>
public enum Specialization
{
    [Display(Name = "Phone Repair")]        Phone = 1,
    [Display(Name = "Laptop Hardware")]     Laptop = 2,
    [Display(Name = "Desktop Hardware")]    Desktop = 3,
    [Display(Name = "Tablet Repair")]       Tablet = 4,
    [Display(Name = "Printer Repair")]      Printer = 5,
    [Display(Name = "Television Repair")]   Television = 6,
    [Display(Name = "General Technician")]  General = 99
}

/// <summary>
/// Whether a technician can take work right now. One enum rather than
/// separate flags, so "Busy -> Available" on completion is a single change.
/// </summary>
public enum TechnicianStatus
{
    Available = 1,
    Busy = 2,
    Offline = 3
}

/// <summary>
/// The work-related half of a technician. The login half lives in
/// AspNetUsers. Receptionists are users with no Technician row.
/// </summary>
public class Technician
{
    public int Id { get; set; }

    /// <summary>FK to the Identity account. A string because Identity uses GUIDs.</summary>
    [Required]
    [StringLength(450)]
    [Display(Name = "Staff Account")]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    public Specialization Specialization { get; set; } = Specialization.General;

    public TechnicianStatus Status { get; set; } = TechnicianStatus.Available;

    // Used to pick the NEAREST suitable technician.
    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Display(Name = "Location Updated")]
    public DateTime? LastLocationUpdate { get; set; }

    [StringLength(100)]
    [Display(Name = "Base / Workshop")]
    public string? BaseLocation { get; set; }

    public ICollection<RepairJob> RepairJobs { get; set; } = new List<RepairJob>();

   
    public bool CanHandle(Specialization required)
        => Specialization == Specialization.General || Specialization == required;

    /// <summary>Free to be assigned new work.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsAssignable => Status == TechnicianStatus.Available;
}
