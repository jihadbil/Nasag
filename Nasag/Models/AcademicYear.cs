using System;
using System.Collections.Generic;

namespace Nasag.Models;

public class AcademicYear
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    public ICollection<FeePlan> FeePlans { get; set; } = new List<FeePlan>();
}
