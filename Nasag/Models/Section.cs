using System.Collections.Generic;

namespace Nasag.Models;

public class Section
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public int Capacity { get; set; } = 30;

    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;

    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
