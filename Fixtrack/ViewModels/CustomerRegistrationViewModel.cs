using System.ComponentModel.DataAnnotations;
using Fixtrack.Models;

namespace Fixtrack.ViewModels;

/// <summary>
/// What the receptionist fills in when registering a new customer.
/// Bundles the customer's details with one or more devices so all
/// of it is saved together from one screen.
/// </summary>
public class CustomerRegistrationViewModel
{
    // ----- Customer -----

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

    [StringLength(200)]
    [Display(Name = "Location")]
    public string? Address { get; set; }

    // ----- Devices (one or more) -----

    public List<DeviceEntryViewModel> Devices { get; set; } = new() { new DeviceEntryViewModel() };
}

/// <summary>One device row within the registration form.</summary>
public class DeviceEntryViewModel
{
    [Required(ErrorMessage = "Device type is required.")]
    [Display(Name = "Device Type")]
    public DeviceType DeviceType { get; set; }

    [Required(ErrorMessage = "Brand is required.")]
    [StringLength(50)]
    [Display(Name = "Brand")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [StringLength(50)]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;
}