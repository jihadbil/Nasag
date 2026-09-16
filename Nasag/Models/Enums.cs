using System;

namespace Nasag.Models;

public enum Gender
{
    Male = 1,
    Female = 2
}

public enum StudentStatus
{
    Active = 1,
    Archived = 2,
    Graduated = 3
}

public enum GradeLevel
{
    Primary = 1,
    Middle = 2,
    High = 3
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4
}

public enum InstallmentStatus
{
    Due = 1,
    Paid = 2,
    PartiallyPaid = 3,
    Overdue = 4
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Cheque = 4,
    Other = 5
}

public enum GuardianRelation
{
    Father = 1,
    Mother = 2,
    Brother = 3,
    Sister = 4,
    Uncle = 5,
    Aunt = 6,
    Grandfather = 7,
    Grandmother = 8,
    Other = 99
}

[Flags]
public enum Permission
{
    None = 0,
    ViewDashboard       = 1 << 0,
    ManageStudents      = 1 << 1,
    ManageClasses       = 1 << 2,
    ManageAttendance    = 1 << 3,
    ManageSubjects      = 1 << 4,
    ManageMarks         = 1 << 5,
    ViewResults         = 1 << 6,
    ManageFees          = 1 << 7,
    ManageReports       = 1 << 8,
    ManageUsers         = 1 << 9,
    ManageSettings      = 1 << 10,
    ManageBackup        = 1 << 11,

    All = ViewDashboard | ManageStudents | ManageClasses | ManageAttendance
        | ManageSubjects | ManageMarks | ViewResults | ManageFees
        | ManageReports | ManageUsers | ManageSettings | ManageBackup
}

/// <summary>
/// Discriminator for a <see cref="BackupLog"/> row: was the file produced by a
/// <c>BACKUP DATABASE</c> (regular backup) or written as an audit trail for a
/// <c>RESTORE DATABASE</c> operation? Stored as <c>int</c> via EF Core HasConversion.
/// </summary>
public enum BackupKind
{
    Backup = 0,
    Restore = 1
}
