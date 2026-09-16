using System;

namespace Nasag.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Notes { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
}
