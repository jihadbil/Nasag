using System.Collections.Generic;

namespace Nasag.Models;

public class Subject
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public decimal MaxMark { get; set; } = 100m;
    public decimal PassMark { get; set; } = 50m;

    public int GradeId { get; set; }
    public Grade Grade { get; set; } = null!;

    public ICollection<Mark> Marks { get; set; } = new List<Mark>();
}
