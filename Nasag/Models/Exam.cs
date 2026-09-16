using System.Collections.Generic;

namespace Nasag.Models;

public class Exam
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public decimal Weight { get; set; } = 1m;

    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public ICollection<Mark> Marks { get; set; } = new List<Mark>();
}
