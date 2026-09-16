using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nasag.Models;

public class StudentFee
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Notes { get; set; }

    [NotMapped]
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int FeePlanId { get; set; }
    public FeePlan FeePlan { get; set; } = null!;

    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
