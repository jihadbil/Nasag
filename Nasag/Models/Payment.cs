using System;

namespace Nasag.Models;

public class Payment
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }

    public int StudentFeeId { get; set; }
    public StudentFee StudentFee { get; set; } = null!;

    public int? InstallmentId { get; set; }
    public Installment? Installment { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
