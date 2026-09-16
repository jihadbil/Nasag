using System;
using System.Collections.Generic;

namespace Nasag.Models;

public class Student
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string? NationalId { get; set; }
    public string? PhotoPath { get; set; }
    public byte[]? PhotoBytes { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public StudentStatus Status { get; set; } = StudentStatus.Active;

    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public int GuardianId { get; set; }
    public Guardian Guardian { get; set; } = null!;

    public ICollection<Mark> Marks { get; set; } = new List<Mark>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<StudentFee> StudentFees { get; set; } = new List<StudentFee>();
}
