using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Fixtrack.Models;


public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    // Lets a receptionist be deactivated without deleting the account,
    // so the jobs they recorded keep their author.
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
