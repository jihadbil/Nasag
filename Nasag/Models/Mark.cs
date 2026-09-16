using System;

namespace Nasag.Models;

public class Mark
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
}
