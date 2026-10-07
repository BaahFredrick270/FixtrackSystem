using System.ComponentModel.DataAnnotations;

namespace Fixtrack.Models;

/// <summary>How the customer prefers to be reached when a repair is ready.</summary>
public enum PreferredContactMethod
{
    Phone = 1,
    WhatsApp = 2,
    SMS = 3,
    Email = 4,
    [Display(Name = "No Remote Contact")] NoRemoteContact = 5
}

/// <summary>A person who brings devices in for repair.</summary>
public class Customer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(60)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(60)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A phone number is required to contact the customer.")]
    [StringLength(20)]
    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Preferred Contact Method")]
    public PreferredContactMethod PreferredContactMethod { get; set; } = PreferredContactMethod.Phone;

    [Display(Name = "Registered On")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ----- navigation -----
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<RepairJob> RepairJobs { get; set; } = new List<RepairJob>();

    /// <summary>Convenience for lists and dropdowns. Not a column.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [Display(Name = "Customer")]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
