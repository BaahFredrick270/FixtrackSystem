using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fixtrack.Models;

/// <summary>
/// A part consumed by one repair. UnitPrice is copied from the Part at
/// the time it was used - if the catalogue price changes next month, this
/// repair's bill must not change with it.
/// </summary>
public class PartUsed
{
    public int Id { get; set; }

    [Display(Name = "Repair")]
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    [Display(Name = "Part")]
    public int PartId { get; set; }
    public Part? Part { get; set; }

    [Range(1, 1000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 1;

    [Range(0.01, 1_000_000)]
    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Used On")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [NotMapped]
    [Display(Name = "Line Total")]
    public decimal LineTotal => Quantity * UnitPrice;
}
