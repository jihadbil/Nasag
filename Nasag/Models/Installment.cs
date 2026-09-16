using System;
using System.Collections.Generic;

namespace Nasag.Models;

public class Installment
{
    public int Id { get; set; }
    public int Number { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Due;

    public int StudentFeeId { get; set; }
    public StudentFee StudentFee { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
