using System.Collections.Generic;

namespace Nasag.Models;

public class Grade
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public GradeLevel Level { get; set; }
    public int SortOrder { get; set; }

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
