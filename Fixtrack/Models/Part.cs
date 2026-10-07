using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fixtrack.Models;

/// <summary>
/// A spare part the shop stocks. Quantity here is stock on hand;
/// what a repair consumed is recorded on PartUsed.
/// </summary>
public class Part
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Part name is required.")]
    [StringLength(100)]
    [Display(Name = "Part Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
    [Display(Name = "Quantity in Stock")]
    public int Quantity { get; set; }

    [Range(0.01, 1_000_000, ErrorMessage = "Enter a price greater than zero.")]
    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    public ICollection<PartUsed> UsedIn { get; set; } = new List<PartUsed>();

    [NotMapped]
    public bool IsOutOfStock => Quantity <= 0;
}
