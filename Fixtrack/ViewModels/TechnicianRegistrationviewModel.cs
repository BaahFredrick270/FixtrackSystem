using System.ComponentModel.DataAnnotations;
using Fixtrack.Models;

namespace Fixtrack.ViewModels;

public class TechnicianRegistrationViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required - it is the login username.")]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Specialization Specialization { get; set; } = Specialization.General;

    [StringLength(100)]
    [Display(Name = "Base / Workshop")]
    public string? BaseLocation { get; set; }
}

/// <summary>Shown once, right after creation. Never stored.</summary>
public class TechnicianCredentialsViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
}