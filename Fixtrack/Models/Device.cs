using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fixtrack.Models;

/// <summary>The kinds of device the shop accepts.</summary>
public enum DeviceType
{
    Phone = 1,
    Laptop = 2,
    Desktop = 3,
    Tablet = 4,
    Printer = 5,
    Television = 6,
    Other = 99
}

/// <summary>
/// A physical item belonging to a customer. One device can come back
/// for repair many times, so devices are stored separately from jobs.
/// </summary>
public class Device
{
    public int Id { get; set; }

    [Display(Name = "Customer")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [Display(Name = "Device Type")]
    public DeviceType DeviceType { get; set; }

    [Required(ErrorMessage = "Brand is required.")]
    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Serial Number")]
    public string? SerialNumber { get; set; }

    /// <summary>Cosmetic and physical state at hand-over, e.g. "Screen has
    /// scratches". Recorded so the shop is not blamed for existing damage.</summary>
    [StringLength(500)]
    [Display(Name = "Condition")]
    public string? Condition { get; set; }

    [Display(Name = "Added On")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<RepairJob> RepairJobs { get; set; } = new List<RepairJob>();

    /// <summary>"HP EliteBook 840 (Laptop)" - for dropdowns and lists.</summary>
    [NotMapped]
    public string DisplayName => $"{Brand} {Model} ({DeviceType})";
}
