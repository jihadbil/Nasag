using System.Collections.Generic;

namespace Nasag.Models;

public class FeePlan
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InstallmentsCount { get; set; } = 1;

    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;

    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public ICollection<StudentFee> StudentFees { get; set; } = new List<StudentFee>();
}
