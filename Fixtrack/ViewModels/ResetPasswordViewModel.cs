using System.ComponentModel.DataAnnotations;

namespace Fixtrack.ViewModels;

public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least {2} characters.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}