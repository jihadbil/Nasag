using Microsoft.EntityFrameworkCore;
using Nasag.Models;

namespace Nasag.Data;

public class NasaqDbContext : DbContext
{
    public NasaqDbContext(DbContextOptions<NasaqDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SchoolSettings> SchoolSettings => Set<SchoolSettings>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<FeePlan> FeePlans => Set<FeePlan>();
    public DbSet<StudentFee> StudentFees => Set<StudentFee>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Role>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(80);
            e.HasIndex(x => x.NameAr).IsUnique();
            e.Property(x => x.Permissions).HasConversion<int>();
        });

        b.Entity<User>(e =>
        {
            e.Property(x => x.Username).IsRequired().HasMaxLength(60);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.PhotoPath).HasMaxLength(260);
            e.HasOne(x => x.Role).WithMany(r => r.Users)
             .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SchoolSettings>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(160);
            e.Property(x => x.LogoPath).HasMaxLength(260);
            e.Property(x => x.Address).HasMaxLength(260);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.Website).HasMaxLength(160);
            e.Property(x => x.PrincipalName).HasMaxLength(120);
            e.Property(x => x.LogoBytes).HasColumnType("varbinary(max)");
            e.HasOne(x => x.CurrentAcademicYear).WithMany()
             .HasForeignKey(x => x.CurrentAcademicYearId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<AcademicYear>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(40);
            e.HasIndex(x => x.NameAr).IsUnique();
        });

        b.Entity<Grade>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(60);
            e.Property(x => x.Level).HasConversion<int>();
        });

        b.Entity<Section>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(40);
            e.HasOne(x => x.Grade).WithMany(g => g.Sections)
             .HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AcademicYear).WithMany(y => y.Sections)
             .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.GradeId, x.AcademicYearId, x.NameAr }).IsUnique();
        });

        b.Entity<Guardian>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.AltPhone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.NationalId).HasMaxLength(40);
            e.Property(x => x.Occupation).HasMaxLength(80);
            e.Property(x => x.Address).HasMaxLength(260);
            e.Property(x => x.Relation).HasConversion<int>();
            e.HasIndex(x => x.NationalId);
        });

        b.Entity<Student>(e =>
        {
            e.Property(x => x.StudentNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(x => x.StudentNumber).IsUnique();
            e.Property(x => x.FullName).IsRequired().HasMaxLength(120);
            e.Property(x => x.NationalId).HasMaxLength(40);
            e.Property(x => x.PhotoPath).HasMaxLength(260);
            e.Property(x => x.PhotoBytes).HasColumnType("varbinary(max)");
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Address).HasMaxLength(260);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.Gender).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => x.NationalId);
            e.HasIndex(x => x.FullName);
            e.HasOne(x => x.Section).WithMany(s => s.Students)
             .HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Guardian).WithMany(g => g.Students)
             .HasForeignKey(x => x.GuardianId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Subject>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(80);
            e.Property(x => x.MaxMark).HasPrecision(6, 2);
            e.Property(x => x.PassMark).HasPrecision(6, 2);
            e.HasOne(x => x.Grade).WithMany(g => g.Subjects)
             .HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.GradeId, x.NameAr }).IsUnique();
        });

        b.Entity<Exam>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(80);
            e.Property(x => x.Weight).HasPrecision(5, 2);
            e.HasOne(x => x.AcademicYear).WithMany(y => y.Exams)
             .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Mark>(e =>
        {
            e.Property(x => x.Value).HasPrecision(6, 2);
            e.Property(x => x.Notes).HasMaxLength(300);
            e.HasOne(x => x.Student).WithMany(s => s.Marks)
             .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Subject).WithMany(s => s.Marks)
             .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Exam).WithMany(x => x.Marks)
             .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.StudentId, x.SubjectId, x.ExamId }).IsUnique();
        });

        b.Entity<AttendanceRecord>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(300);
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.Student).WithMany(s => s.AttendanceRecords)
             .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StudentId, x.Date }).IsUnique();
        });

        b.Entity<FeePlan>(e =>
        {
            e.Property(x => x.NameAr).IsRequired().HasMaxLength(120);
            e.Property(x => x.TotalAmount).HasPrecision(12, 2);
            e.HasOne(x => x.Grade).WithMany()
             .HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AcademicYear).WithMany(y => y.FeePlans)
             .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<StudentFee>(e =>
        {
            e.Property(x => x.TotalAmount).HasPrecision(12, 2);
            e.Property(x => x.PaidAmount).HasPrecision(12, 2);
            e.Property(x => x.Notes).HasMaxLength(300);
            e.HasOne(x => x.Student).WithMany(s => s.StudentFees)
             .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FeePlan).WithMany(p => p.StudentFees)
             .HasForeignKey(x => x.FeePlanId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Installment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.PaidAmount).HasPrecision(12, 2);
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.StudentFee).WithMany(f => f.Installments)
             .HasForeignKey(x => x.StudentFeeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(40);
            e.HasIndex(x => x.ReceiptNumber).IsUnique();
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.Method).HasConversion<int>();
            e.Property(x => x.Notes).HasMaxLength(300);
            e.HasOne(x => x.StudentFee).WithMany(f => f.Payments)
             .HasForeignKey(x => x.StudentFeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Installment).WithMany(i => i.Payments)
             .HasForeignKey(x => x.InstallmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<BackupLog>(e =>
        {
            e.Property(x => x.FilePath).IsRequired().HasMaxLength(400);
            e.Property(x => x.Notes).HasMaxLength(300);
            e.Property(x => x.Kind).HasConversion<int>().HasDefaultValue(BackupKind.Backup);
            e.HasOne(x => x.CreatedByUser).WithMany()
             .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
