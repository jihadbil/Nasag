using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Repositories;

public interface IFeesRepository
{
    Task<FeesLookups> GetLookupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FeesStudentOption>> GetStudentsForSectionAsync(int sectionId, CancellationToken ct = default);
    Task<StudentFeeDetails?> GetStudentDetailsAsync(int studentId, CancellationToken ct = default);
    Task<PaymentSaveResult> RecordPaymentAsync(PaymentSaveModel model, CancellationToken ct = default);
    Task DeletePaymentAsync(int paymentId, CancellationToken ct = default);

    Task<IReadOnlyList<FeePlanOption>> GetAssignablePlansAsync(int gradeId, CancellationToken ct = default);
    Task<int> AssignFeePlanAsync(int studentId, int feePlanId, CancellationToken ct = default);
    Task<StudentLocation?> LocateStudentAsync(int studentId, CancellationToken ct = default);
    Task<StudentLocateResult?> LocateStudentByNumberAsync(string studentNumber, CancellationToken ct = default);
    Task<SchoolHeaderInfo> GetSchoolHeaderAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks any <see cref="Nasag.Models.Installment"/> whose <c>DueDate</c> is strictly
    /// before today (local) and is still <c>Due</c> with no payments as <c>Overdue</c>.
    /// Idempotent; safe to call at session start.
    /// </summary>
    Task<int> RecomputeOverdueAsync(CancellationToken ct = default);
}

public sealed record FeesLookups(
    IReadOnlyList<FeesGradeOption> Grades,
    IReadOnlyList<FeesSectionOption> Sections);

public sealed record FeesGradeOption(int Id, string NameAr);

public sealed record FeesSectionOption(int Id, string NameAr, int GradeId, string GradeName, int StudentCount)
{
    public string DisplayName => $"{NameAr} ({StudentCount})";
}

public sealed record FeesStudentOption(int Id, string FullName, string StudentNumber)
{
    public string DisplayName => $"{FullName} ({StudentNumber})";
}

public sealed record StudentFeeDetails(
    int StudentId,
    string StudentNumber,
    string FullName,
    string GradeName,
    string SectionName,
    int? StudentFeeId,
    int? FeePlanId,
    string? FeePlanName,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<FeeInstallmentRow> Installments,
    IReadOnlyList<FeePaymentRow> Payments);

public sealed record FeeInstallmentRow(
    int Id,
    int Number,
    decimal Amount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime DueDate,
    Nasag.Models.InstallmentStatus Status,
    DateTime? LastPaymentDate);

public sealed record FeePaymentRow(
    int Id,
    string ReceiptNumber,
    decimal Amount,
    DateTime PaymentDate,
    Nasag.Models.PaymentMethod Method,
    string? Notes,
    int? InstallmentId,
    int? InstallmentNumber,
    int UserId,
    string UserName);

public sealed class PaymentSaveModel
{
    public int StudentFeeId { get; set; }
    public int? InstallmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } // UTC
    public Nasag.Models.PaymentMethod Method { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }
}

public sealed record PaymentSaveResult(int PaymentId, string ReceiptNumber);

public sealed record FeePlanOption(int Id, string NameAr, decimal TotalAmount, int InstallmentsCount)
{
    public string Display => $"{NameAr} — {Nasag.Helpers.MoneyFormatter.Format(TotalAmount)} / {InstallmentsCount} قسط";
}

public sealed record StudentLocation(int GradeId, int SectionId);

/// <summary>Returned by <see cref="IFeesRepository.LocateStudentByNumberAsync"/>.</summary>
public sealed record StudentLocateResult(int StudentId, int GradeId, int SectionId);

public sealed record SchoolHeaderInfo(string NameAr, string? Address, string? Phone);
