using System.Collections.Generic;

namespace Nasag.Models;

public class Role
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public Permission Permissions { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
