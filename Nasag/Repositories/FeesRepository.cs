using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nasag.Data;
using Nasag.Models;
using Nasag.Services;

namespace Nasag.Repositories;

public sealed class FeesRepository : IFeesRepository
{
    // WHY: 0.01 matches the printed granularity (N2 / two decimals). Using 0.005
    // let a 0.5-halala under/over slip through balance checks silently.
    private const decimal Epsilon = 0.01m;

    // WHY: receipt number is UNIQUE in DB. Under concurrency two callers can read the
    // same daily counter and INSERT the same value; we re-generate + retry up to 3x.
    private const int ReceiptInsertMaxAttempts = 3;

    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly ICurrentUserService _currentUser;

    public FeesRepository(IDbContextFactory<NasaqDbContext> factory, ICurrentUserService currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<FeesLookups> GetLookupsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        var grades = await ctx.Grades
            .AsNoTracking()
            .Where(g => g.Sections.Any(s => yearId == null || s.AcademicYearId == yearId))
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .Select(g => new FeesGradeOption(g.Id, g.NameAr))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sections = await ctx.Sections
            .AsNoTracking()
            .Where(s => yearId == null || s.AcademicYearId == yearId)
            .OrderBy(s => s.Grade.SortOrder)
            .ThenBy(s => s.NameAr)
            .Select(s => new FeesSectionOption(
                s.Id,
                s.NameAr,
                s.GradeId,
                s.Grade.NameAr,
                s.Students.Count(st => st.Status == StudentStatus.Active)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new FeesLookups(grades, sections);
    }

    public async Task<IReadOnlyList<FeesStudentOption>> GetStudentsForSectionAsync(int sectionId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.Students
            .AsNoTracking()
            .Where(s => s.SectionId == sectionId && s.Status == StudentStatus.Active)
            .OrderBy(s => s.FullName)
            .Select(s => new FeesStudentOption(s.Id, s.FullName, s.StudentNumber))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<StudentFeeDetails?> GetStudentDetailsAsync(int studentId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var student = await ctx.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new
            {
                s.Id,
                s.StudentNumber,
                s.FullName,
                GradeName = s.Section.Grade.NameAr,
                SectionName = s.Section.NameAr
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (student is null) return null;

        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        // WHY: AsSplitQuery avoids cartesian explosion when joining Installments + Payments
        // for the same StudentFee (each grows the row count multiplicatively otherwise).
        // Payment.User is projected to a flat (Id, FullName) pair to avoid loading
        // sensitive columns (PasswordHash, future photo bytes).
        var fee = await ctx.StudentFees
            .AsNoTracking()
            .AsSplitQuery()
            .Where(f => f.StudentId == studentId && (yearId == null || f.FeePlan.AcademicYearId == yearId))
            .Select(f => new
            {
                f.Id,
                f.FeePlanId,
                FeePlanName = f.FeePlan.NameAr,
                f.TotalAmount,
                f.PaidAmount,
                Installments = f.Installments.Select(i => new
                {
                    i.Id,
                    i.Number,
                    i.Amount,
                    i.PaidAmount,
                    i.DueDate,
                    i.Status
                }).ToList(),
                Payments = f.Payments.Select(p => new
                {
                    p.Id,
                    p.ReceiptNumber,
                    p.Amount,
                    p.PaymentDate,
                    p.Method,
                    p.Notes,
                    p.InstallmentId,
                    p.UserId,
                    UserFullName = p.User != null ? p.User.FullName : null
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (fee is null)
        {
            return new StudentFeeDetails(
                student.Id,
                student.StudentNumber,
                student.FullName,
                student.GradeName,
                student.SectionName,
                null,
                null,
                null,
                0m,
                0m,
                0m,
                Array.Empty<FeeInstallmentRow>(),
                Array.Empty<FeePaymentRow>());
        }

        // WHY: compare against local "today" (DateTime.Today) so the user-visible
        // overdue flag matches their wall-clock day, matching what RecomputeOverdueAsync
        // persists. DueDate is a calendar date, not a UTC instant.
        var today = DateTime.Today;
        var paymentsByInstallment = fee.Payments
            .Where(p => p.InstallmentId.HasValue)
            .GroupBy(p => p.InstallmentId!.Value)
            .ToDictionary(g => g.Key, g => g.Max(p => p.PaymentDate));

        var installments = fee.Installments
            .OrderBy(i => i.Number)
            .Select(i =>
            {
                paymentsByInstallment.TryGetValue(i.Id, out var lastPaid);
                // Override stored status to Overdue for display when Due past its date.
                var status = (i.Status == InstallmentStatus.Due && i.DueDate.Date < today)
                    ? InstallmentStatus.Overdue
                    : i.Status;
                return new FeeInstallmentRow(
                    i.Id,
                    i.Number,
                    i.Amount,
                    i.PaidAmount,
                    i.Amount - i.PaidAmount,
                    i.DueDate,
                    status,
                    lastPaid == default ? null : lastPaid);
            })
            .ToList();

        var installmentNumbers = fee.Installments.ToDictionary(i => i.Id, i => i.Number);

        var payments = fee.Payments
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .Select(p => new FeePaymentRow(
                p.Id,
                p.ReceiptNumber,
                p.Amount,
                p.PaymentDate,
                p.Method,
                p.Notes,
                p.InstallmentId,
                p.InstallmentId.HasValue && installmentNumbers.TryGetValue(p.InstallmentId.Value, out var num) ? num : (int?)null,
                p.UserId,
                p.UserFullName ?? "—"))
            .ToList();

        return new StudentFeeDetails(
            student.Id,
            student.StudentNumber,
            student.FullName,
            student.GradeName,
            student.SectionName,
            fee.Id,
            fee.FeePlanId,
            fee.FeePlanName,
            fee.TotalAmount,
            fee.PaidAmount,
            fee.TotalAmount - fee.PaidAmount,
            installments,
            payments);
    }

    public async Task<PaymentSaveResult> RecordPaymentAsync(PaymentSaveModel model, CancellationToken ct = default)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));

        // Permission check at the repository boundary — the VM also guards, but a
        // direct caller (tests, future API) must not bypass it.
        EnsurePermission(Permission.ManageFees);

        if (model.Amount <= 0m)
            throw new InvalidOperationException("يجب أن يكون مبلغ الدفعة أكبر من صفر.");
        if (model.StudentFeeId <= 0)
            throw new InvalidOperationException("معرّف الرسوم غير صالح.");
        if (model.UserId <= 0)
            throw new InvalidOperationException("معرّف المستخدم غير صالح.");

        // WHY: reject future-dated and obviously-corrupted dates before opening a tx.
        var paymentDateRaw = model.PaymentDate == default ? DateTime.UtcNow : model.PaymentDate;
        var paymentDateLocal = paymentDateRaw.Kind == DateTimeKind.Utc ? paymentDateRaw.ToLocalTime() : paymentDateRaw;
        if (paymentDateLocal.Date > DateTime.Today)
            throw new InvalidOperationException("لا يمكن تسجيل دفعة بتاريخ مستقبلي.");
        if (paymentDateLocal.Year < 2000)
            throw new InvalidOperationException("تاريخ غير صالح.");

        int paymentId = 0;
        string receiptNumber = string.Empty;

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var strategy = ctx.Database.CreateExecutionStrategy();

        // WHY: a unique-violation on Payment.ReceiptNumber under concurrency means
        // two requests generated the same daily counter value. We re-roll the number
        // and replay the whole strategy (which also re-opens the tx because EF Core's
        // retrying strategy rejects user-managed transactions outside of ExecuteAsync).
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

                    var fee = await ctx.StudentFees
                        .FirstOrDefaultAsync(f => f.Id == model.StudentFeeId, ct)
                        .ConfigureAwait(false);

                    if (fee is null)
                        throw new InvalidOperationException("سجلّ الرسوم غير موجود.");

                    var remaining = fee.TotalAmount - fee.PaidAmount;
                    if (model.Amount - Epsilon > remaining)
                        throw new InvalidOperationException("المبلغ يتجاوز الرسوم المتبقية.");

                    Installment? installment = null;
                    if (model.InstallmentId.HasValue)
                    {
                        installment = await ctx.Installments
                            .FirstOrDefaultAsync(i => i.Id == model.InstallmentId.Value && i.StudentFeeId == fee.Id, ct)
                            .ConfigureAwait(false);
                        if (installment is null)
                            throw new InvalidOperationException("هذا القسط لا ينتمي لرسوم الطالب.");

                        var installmentRemaining = installment.Amount - installment.PaidAmount;
                        if (model.Amount - Epsilon > installmentRemaining)
                            throw new InvalidOperationException("المبلغ يتجاوز قيمة القسط المتبقية.");
                    }

                    receiptNumber = await GenerateNextReceiptNumberAsync(ctx, ct).ConfigureAwait(false);
                    EnsureReceiptNumberFormat(receiptNumber);

                    var notes = NormalizeNotes(model.Notes);

                    var payment = new Payment
                    {
                        ReceiptNumber = receiptNumber,
                        Amount = model.Amount,
                        PaymentDate = paymentDateLocal,
                        Method = model.Method,
                        Notes = notes,
                        StudentFeeId = fee.Id,
                        InstallmentId = installment?.Id,
                        UserId = model.UserId
                    };
                    ctx.Payments.Add(payment);

                    // Insert the receipt first so a unique-violation throws before the
                    // counter updates downstream — keeps the catch-and-retry loop clean.
                    await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

                    // WHY: use ExecuteUpdate so the increment runs as a single
                    // atomic SQL statement (SET PaidAmount = PaidAmount + @p), avoiding
                    // dirty-read / dirty-write races with another concurrent payment.
                    if (installment is not null)
                    {
                        await ctx.Installments
                            .Where(i => i.Id == installment.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(
                                i => i.PaidAmount,
                                i => i.PaidAmount + model.Amount), ct)
                            .ConfigureAwait(false);

                        var fresh = await ctx.Installments
                            .AsNoTracking()
                            .Where(i => i.Id == installment.Id)
                            .Select(i => new { i.Amount, i.PaidAmount })
                            .FirstAsync(ct)
                            .ConfigureAwait(false);

                        var newStatus = ComputeInstallmentStatus(fresh.Amount, fresh.PaidAmount);
                        await ctx.Installments
                            .Where(i => i.Id == installment.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, _ => newStatus), ct)
                            .ConfigureAwait(false);
                    }

                    await ctx.StudentFees
                        .Where(f => f.Id == fee.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(
                            f => f.PaidAmount,
                            f => f.PaidAmount + model.Amount), ct)
                        .ConfigureAwait(false);

                    await tx.CommitAsync(ct).ConfigureAwait(false);

                    paymentId = payment.Id;
                }).ConfigureAwait(false);

                return new PaymentSaveResult(paymentId, receiptNumber);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DbUpdateException dbEx) when (IsReceiptUniqueViolation(dbEx) && attempt < ReceiptInsertMaxAttempts)
            {
                // Detach any tracked entities so the next replay starts clean.
                foreach (var entry in ctx.ChangeTracker.Entries().ToList())
                    entry.State = EntityState.Detached;
                // Loop and retry — strategy/ctx remain valid.
            }
        }
    }

    public async Task DeletePaymentAsync(int paymentId, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageFees);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var strategy = ctx.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var payment = await ctx.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
                .ConfigureAwait(false);
            if (payment is null)
                throw new InvalidOperationException("الدفعة غير موجودة.");

            // WHY: decrement using SET PaidAmount = MAX(0, PaidAmount - @p) atomically.
            await ctx.StudentFees
                .Where(f => f.Id == payment.StudentFeeId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    f => f.PaidAmount,
                    f => f.PaidAmount - payment.Amount < 0m ? 0m : f.PaidAmount - payment.Amount), ct)
                .ConfigureAwait(false);

            if (payment.InstallmentId.HasValue)
            {
                await ctx.Installments
                    .Where(i => i.Id == payment.InstallmentId.Value)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        i => i.PaidAmount,
                        i => i.PaidAmount - payment.Amount < 0m ? 0m : i.PaidAmount - payment.Amount), ct)
                    .ConfigureAwait(false);

                var fresh = await ctx.Installments
                    .AsNoTracking()
                    .Where(i => i.Id == payment.InstallmentId.Value)
                    .Select(i => new { i.Amount, i.PaidAmount })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (fresh is not null)
                {
                    var newStatus = ComputeInstallmentStatus(fresh.Amount, fresh.PaidAmount);
                    await ctx.Installments
                        .Where(i => i.Id == payment.InstallmentId.Value)
                        .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, _ => newStatus), ct)
                        .ConfigureAwait(false);
                }
            }

            await ctx.Payments
                .Where(p => p.Id == paymentId)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<int> RecomputeOverdueAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var strategy = ctx.Database.CreateExecutionStrategy();

        // WHY: DateTime.Today (Local) matches DueDate which is stored as a calendar date
        // (not a UTC instant). Using UtcNow could mark or skip rows incorrectly at day-edges.
        var today = DateTime.Today;

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            var updated = await ctx.Installments
                .Where(i => i.DueDate < today
                            && i.Status == InstallmentStatus.Due
                            && i.PaidAmount == 0m)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, _ => InstallmentStatus.Overdue), ct)
                .ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return updated;
        }).ConfigureAwait(false);
    }

    private static async Task<string> GenerateNextReceiptNumberAsync(NasaqDbContext ctx, CancellationToken ct)
    {
        var today = DateTime.Today;
        var prefix = $"REC-{today:yyyyMMdd}-";

        var count = await ctx.Payments
            .Where(p => p.ReceiptNumber.StartsWith(prefix))
            .CountAsync(ct)
            .ConfigureAwait(false);

        var next = count + 1;
        return $"{prefix}{next:D4}";
    }

    private static void EnsureReceiptNumberFormat(string value)
    {
        // Sanity check: REC-yyyyMMdd-#### (#### is 0001..9999).
        if (string.IsNullOrWhiteSpace(value) || value.Length != 17)
            throw new InvalidOperationException("صيغة رقم السند غير صالحة.");
        if (!value.StartsWith("REC-", StringComparison.Ordinal))
            throw new InvalidOperationException("صيغة رقم السند غير صالحة.");
        var datePart = value.Substring(4, 8);
        var dashIdx = value.IndexOf('-', 12);
        if (dashIdx != 12)
            throw new InvalidOperationException("صيغة رقم السند غير صالحة.");
        var seq = value.Substring(13);
        if (seq.Length != 4 || !int.TryParse(seq, out var seqN) || seqN < 1)
            throw new InvalidOperationException("صيغة رقم السند غير صالحة.");
        if (!DateTime.TryParseExact(datePart, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
            throw new InvalidOperationException("صيغة رقم السند غير صالحة.");
    }

    private static bool IsReceiptUniqueViolation(DbUpdateException ex)
    {
        // 2601 / 2627 = SQL Server unique index / unique constraint violations.
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }
        return false;
    }

    private static InstallmentStatus ComputeInstallmentStatus(decimal amount, decimal paidAmount)
    {
        if (paidAmount + Epsilon >= amount) return InstallmentStatus.Paid;
        if (paidAmount > 0m) return InstallmentStatus.PartiallyPaid;
        return InstallmentStatus.Due;
    }

    private static string? NormalizeNotes(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length <= 300 ? text : text[..300];
    }

    public async Task<IReadOnlyList<FeePlanOption>> GetAssignablePlansAsync(int gradeId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

        return await ctx.FeePlans
            .AsNoTracking()
            .Where(p => p.GradeId == gradeId && (yearId == null || p.AcademicYearId == yearId))
            .OrderBy(p => p.TotalAmount)
            .Select(p => new FeePlanOption(p.Id, p.NameAr, p.TotalAmount, p.InstallmentsCount))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> AssignFeePlanAsync(int studentId, int feePlanId, CancellationToken ct = default)
    {
        EnsurePermission(Permission.ManageFees);

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var strategy = ctx.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var yearId = await GetCurrentYearIdAsync(ctx, ct).ConfigureAwait(false);

            var plan = await ctx.FeePlans
                .Include(p => p.AcademicYear)
                .FirstOrDefaultAsync(p => p.Id == feePlanId, ct)
                .ConfigureAwait(false);
            if (plan is null)
                throw new InvalidOperationException("خطة الرسوم المختارة غير موجودة.");

            // Sanity-validate the plan before doing anything else.
            if (plan.InstallmentsCount <= 0 || plan.TotalAmount <= 0m)
                throw new InvalidOperationException("خطة الرسوم غير صالحة (عدد الأقساط أو الإجمالي = 0).");

            var student = await ctx.Students
                .Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct)
                .ConfigureAwait(false);
            if (student is null)
                throw new InvalidOperationException("الطالب غير موجود.");

            if (student.Status != StudentStatus.Active)
                throw new InvalidOperationException("لا يمكن تعيين خطة لطالب غير نشط.");

            if (plan.GradeId != student.Section.GradeId)
                throw new InvalidOperationException("خطة الرسوم لا تطابق صف الطالب.");

            if (yearId.HasValue && plan.AcademicYearId != yearId.Value)
                throw new InvalidOperationException("خطة الرسوم لا تخص السنة الحالية.");

            var exists = await ctx.StudentFees
                .AnyAsync(f => f.StudentId == studentId && f.FeePlan.AcademicYearId == plan.AcademicYearId, ct)
                .ConfigureAwait(false);
            if (exists)
                throw new InvalidOperationException("يوجد سجل رسوم لهذا الطالب في السنة الحالية مسبقاً.");

            var fee = new StudentFee
            {
                StudentId = studentId,
                FeePlanId = feePlanId,
                TotalAmount = plan.TotalAmount,
                PaidAmount = 0m
            };
            ctx.StudentFees.Add(fee);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

            var count = plan.InstallmentsCount;
            var installmentAmount = Math.Round(plan.TotalAmount / count, 2);
            var roundedTotal = installmentAmount * count;
            var remainder = plan.TotalAmount - roundedTotal;

            // WHY: distribute installments evenly across the academic year instead of
            // hard-coding 15-September every-2-months. Falls back to UtcNow for years
            // that don't yet have start/end dates set.
            var startDate = plan.AcademicYear?.StartDate.Date ?? DateTime.UtcNow.Date;
            var endDate = plan.AcademicYear?.EndDate.Date ?? startDate.AddMonths(10);
            var totalMonths = Math.Max(1, (int)((endDate - startDate).TotalDays / 30));
            var intervalMonths = Math.Max(1, totalMonths / count);

            for (var n = 1; n <= count; n++)
            {
                // Roll any rounding remainder onto the last installment so Σ == TotalAmount.
                var amount = (n == count) ? installmentAmount + remainder : installmentAmount;
                ctx.Installments.Add(new Installment
                {
                    StudentFeeId = fee.Id,
                    Number = n,
                    Amount = amount,
                    DueDate = startDate.AddMonths((n - 1) * intervalMonths),
                    Status = InstallmentStatus.Due
                });
            }

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            return fee.Id;
        }).ConfigureAwait(false);
    }

    public async Task<StudentLocation?> LocateStudentAsync(int studentId, CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new StudentLocation(s.Section.GradeId, s.SectionId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<StudentLocateResult?> LocateStudentByNumberAsync(string studentNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(studentNumber)) return null;
        var n = studentNumber.Trim();

        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await ctx.Students
            .AsNoTracking()
            .Where(s => s.StudentNumber == n)
            .Select(s => new StudentLocateResult(s.Id, s.Section.GradeId, s.SectionId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<SchoolHeaderInfo> GetSchoolHeaderAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var info = await ctx.SchoolSettings
            .AsNoTracking()
            .Select(s => new SchoolHeaderInfo(s.NameAr, s.Address, s.Phone))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return info ?? new SchoolHeaderInfo("نَسَق لإدارة المدارس", null, null);
    }

    private static async Task<int?> GetCurrentYearIdAsync(NasaqDbContext ctx, CancellationToken ct)
    {
        var fromSettings = await ctx.SchoolSettings
            .AsNoTracking()
            .Select(s => s.CurrentAcademicYearId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (fromSettings.HasValue) return fromSettings.Value;

        return await ctx.AcademicYears
            .AsNoTracking()
            .Where(y => y.IsActive)
            .OrderByDescending(y => y.StartDate)
            .Select(y => (int?)y.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private void EnsurePermission(Permission required)
    {
        if (!_currentUser.HasPermission(required))
            throw new UnauthorizedAccessException("ليس لديك صلاحية إدارة الرسوم.");
    }
}
