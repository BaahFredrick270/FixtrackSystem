using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fixtrack.Models;

public enum PaymentMethod
{
    Cash = 1,
    [Display(Name = "Mobile Money")] MobileMoney = 2,
    Card = 3
}

public class Payment
{
    public int Id { get; set; }

    [Display(Name = "Repair")]
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    [Range(0.01, 1_000_000, ErrorMessage = "Enter an amount greater than zero.")]
    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>Mobile money transaction id, card slip number, receipt number.</summary>
    [StringLength(100)]
    public string? Reference { get; set; }

    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.Now;

    /// <summary>The receptionist who took the money.</summary>
    [StringLength(450)]
    [Display(Name = "Received By")]
    public string? ReceivedByUserId { get; set; }
}
