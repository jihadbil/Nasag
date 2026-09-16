using System.Collections.Generic;

namespace Nasag.Models;

public class Guardian
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public GuardianRelation Relation { get; set; }
    public string? Phone { get; set; }
    public string? AltPhone { get; set; }
    public string? Email { get; set; }
    public string? NationalId { get; set; }
    public string? Occupation { get; set; }
    public string? Address { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
