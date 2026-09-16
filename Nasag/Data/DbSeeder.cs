using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nasag.Models;
using Nasag.Services;

namespace Nasag.Data;

public sealed class DbSeeder : IDbSeeder
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly IPendingAdminSetupStore _pendingStore;

    public DbSeeder(IDbContextFactory<NasaqDbContext> factory, IPendingAdminSetupStore pendingStore)
    {
        _factory = factory;
        _pendingStore = pendingStore;
    }

    public async Task SeedIfEmptyAsync(CancellationToken ct = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // (1) Idempotence — لا نلمس قاعدة موجودة بها مستخدمون.
        if (await ctx.Users.AnyAsync(ct).ConfigureAwait(false))
            return;

        // (2) هل هناك حمولة مدير قادمة من معالج الإعداد؟ (تستهلك الملف وتحذفه)
        var pending = _pendingStore.ReadAndClear();

        if (pending is not null)
        {
            // وضع التثبيت الفعلي: أدوار + مدير من بيانات المعالج + Placeholder للمدرسة فقط.
            await SeedMinimalAsync(ctx, pending, ct).ConfigureAwait(false);
        }
        else
        {
            // وضع المطوّر / العرض التجريبي: البذرة الغنية كما كانت.
            await SeedFullDemoAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Minimal seed — يستخدم عند إنشاء قاعدة جديدة من معالج الإعداد.
    //   * يُنشئ الأدوار الأربعة.
    //   * يُنشئ مستخدم admin من بيانات المعالج (كلمة المرور مُجزّأة BCrypt).
    //   * يُنشئ Placeholder لـ SchoolSettings كي لا تنهار الشاشات
    //     التي تتوقع وجود صف واحد على الأقل (الإعدادات، التقارير، الرسوم).
    //   * لا سنة دراسية ولا صفوف ولا شعب ولا مواد ولا امتحانات ولا طلاب.
    //     المستخدم يُكمل البيانات من شاشات CRUD الموجودة.
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedMinimalAsync(
        NasaqDbContext ctx,
        PendingAdminSetup pending,
        CancellationToken ct)
    {
        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var adminRole = await SeedRolesAsync(ctx, ct).ConfigureAwait(false);
            await SeedAdminAsync(ctx, adminRole, pending, ct).ConfigureAwait(false);
            await SeedSchoolPlaceholderAsync(ctx, ct).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    // إنشاء الأدوار الأربعة الأساسية. يُرجع كائن دور «مدير النظام»
    // لأنه يُستخدم لاحقاً عند إنشاء مستخدم admin.
    private static async Task<Role> SeedRolesAsync(NasaqDbContext ctx, CancellationToken ct)
    {
        var adminRole = new Role { NameAr = "مدير النظام", Permissions = Permission.All, IsSystem = true };
        var principalRole = new Role
        {
            NameAr = "مدير المدرسة",
            Permissions = Permission.All & ~(Permission.ManageUsers | Permission.ManageBackup)
        };
        var teacherRole = new Role
        {
            NameAr = "معلم",
            Permissions = Permission.ViewDashboard | Permission.ManageAttendance
                        | Permission.ManageMarks | Permission.ViewResults
        };
        var accountantRole = new Role
        {
            NameAr = "محاسب",
            Permissions = Permission.ViewDashboard | Permission.ManageFees | Permission.ManageReports
        };
        ctx.Roles.AddRange(adminRole, principalRole, teacherRole, accountantRole);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return adminRole;
    }

    // إنشاء مستخدم admin من حمولة المعالج. كلمة المرور تُجزَّأ هنا بـ BCrypt
    // (workFactor=11 كي تتوافق مع باقي الكود) ثم تُمسح من الذاكرة قدر الإمكان.
    private static async Task SeedAdminAsync(
        NasaqDbContext ctx,
        Role adminRole,
        PendingAdminSetup pending,
        CancellationToken ct)
    {
        var admin = new User
        {
            Username = string.IsNullOrWhiteSpace(pending.Username) ? "admin" : pending.Username.Trim(),
            FullName = string.IsNullOrWhiteSpace(pending.FullName) ? "مدير النظام" : pending.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                string.IsNullOrEmpty(pending.Password) ? "admin123" : pending.Password,
                workFactor: 11),
            IsActive = true,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // صف SchoolSettings مبدئي حتى لا تنهار الصفحات التي تتوقع وجوده.
    // المستخدم يُحرّر الاسم والشعار والعنوان من «الإعدادات → المدرسة» بعد الدخول.
    private static async Task SeedSchoolPlaceholderAsync(NasaqDbContext ctx, CancellationToken ct)
    {
        ctx.SchoolSettings.Add(new SchoolSettings
        {
            NameAr = "مدرستي",
            CurrentAcademicYearId = null
        });
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Full demo seed — السلوك السابق نفسه (محفوظ لتجربة المطوّر).
    // يعمل فقط عندما لا توجد حمولة مدير من المعالج (مثلاً قاعدة LocalDB
    // فارغة في بيئة التطوير من غير المرور بالمعالج).
    // ────────────────────────────────────────────────────────────────────────
    private static async Task SeedFullDemoAsync(NasaqDbContext ctx, CancellationToken ct)
    {
        // 1) Roles
        var adminRole = new Role { NameAr = "مدير النظام", Permissions = Permission.All, IsSystem = true };
        var principalRole = new Role
        {
            NameAr = "مدير المدرسة",
            Permissions = Permission.All & ~(Permission.ManageUsers | Permission.ManageBackup)
        };
        var teacherRole = new Role
        {
            NameAr = "معلم",
            Permissions = Permission.ViewDashboard | Permission.ManageAttendance
                        | Permission.ManageMarks | Permission.ViewResults
        };
        var accountantRole = new Role
        {
            NameAr = "محاسب",
            Permissions = Permission.ViewDashboard | Permission.ManageFees | Permission.ManageReports
        };
        ctx.Roles.AddRange(adminRole, principalRole, teacherRole, accountantRole);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 2) Admin user
        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11),
            FullName = "مدير النظام",
            IsActive = true,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 3) Academic year
        var year = new AcademicYear
        {
            NameAr = "2025 - 2026",
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2026, 6, 30),
            IsActive = true
        };
        ctx.AcademicYears.Add(year);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 4) School settings
        ctx.SchoolSettings.Add(new SchoolSettings
        {
            NameAr = "مدرسة المنار الخاصة",
            Phone = "0213344556",
            Email = "info@almanar-school.ly",
            Address = "طرابلس - منطقة النوفليين",
            PrincipalName = "أ. عبدالسلام الترهوني",
            CurrentAcademicYearId = year.Id
        });
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 5) Grades (Libyan school structure: Basic 1-9, Secondary 1-3)
        var grades = new List<Grade>
        {
            new() { NameAr = "الصف الأول (تعليم أساسي)",   Level = GradeLevel.Primary, SortOrder = 1 },
            new() { NameAr = "الصف الثاني (تعليم أساسي)",  Level = GradeLevel.Primary, SortOrder = 2 },
            new() { NameAr = "الصف الثالث (تعليم أساسي)",  Level = GradeLevel.Primary, SortOrder = 3 },
            new() { NameAr = "الصف الرابع (تعليم أساسي)",  Level = GradeLevel.Primary, SortOrder = 4 },
            new() { NameAr = "الصف الخامس (تعليم أساسي)",  Level = GradeLevel.Primary, SortOrder = 5 },
            new() { NameAr = "الصف السادس (تعليم أساسي)",  Level = GradeLevel.Primary, SortOrder = 6 },
            new() { NameAr = "الصف السابع (تعليم أساسي)",  Level = GradeLevel.Middle,  SortOrder = 7 },
            new() { NameAr = "الصف الثامن (تعليم أساسي)",  Level = GradeLevel.Middle,  SortOrder = 8 },
            new() { NameAr = "الصف التاسع (شهادة أساسي)", Level = GradeLevel.Middle,  SortOrder = 9 },
            new() { NameAr = "السنة الأولى ثانوي (عام)",    Level = GradeLevel.High,    SortOrder = 10 },
            new() { NameAr = "السنة الثانية ثانوي",          Level = GradeLevel.High,    SortOrder = 11 },
            new() { NameAr = "السنة الثالثة ثانوي (شهادة)", Level = GradeLevel.High,    SortOrder = 12 }
        };
        ctx.Grades.AddRange(grades);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 6) Sections — اقتصر على شعب لأول 6 صفوف لتسريع البذرة، مع شعبتين لكل صف
        var sectionLetters = new[] { "أ", "ب" };
        var sections = new List<Section>();
        foreach (var g in grades.Take(6))
        {
            foreach (var letter in sectionLetters)
            {
                sections.Add(new Section
                {
                    NameAr = letter,
                    Capacity = 30,
                    GradeId = g.Id,
                    AcademicYearId = year.Id
                });
            }
        }
        ctx.Sections.AddRange(sections);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 7) Subjects — المواد الأساسية المعتمدة
        var subjectNames = new[]
        {
            "اللغة العربية", "الرياضيات", "العلوم", "الدراسات الاجتماعية",
            "اللغة الإنجليزية", "التربية الإسلامية"
        };
        var subjects = new List<Subject>();
        foreach (var g in grades.Take(6))
        {
            foreach (var name in subjectNames)
                subjects.Add(new Subject { NameAr = name, GradeId = g.Id, MaxMark = 100m, PassMark = 50m });
        }
        ctx.Subjects.AddRange(subjects);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 8) Exams (نظام الفترات والامتحانات)
        var exams = new[]
        {
            new Exam { NameAr = "أعمال السنة - الفصل الأول", Weight = 1m, AcademicYearId = year.Id },
            new Exam { NameAr = "امتحان نهاية الفصل الأول", Weight = 2m, AcademicYearId = year.Id },
            new Exam { NameAr = "امتحان نهاية الفصل الثاني", Weight = 3m, AcademicYearId = year.Id }
        };
        ctx.Exams.AddRange(exams);

        // 9) Fee plans (واحد لكل صف من أول 6 صفوف)
        var feePlans = new List<FeePlan>();
        var idx = 0;
        foreach (var g in grades.Take(6))
        {
            feePlans.Add(new FeePlan
            {
                NameAr = $"رسوم {g.NameAr} - {year.NameAr}",
                TotalAmount = 2500m + idx * 300m,
                InstallmentsCount = 4,
                GradeId = g.Id,
                AcademicYearId = year.Id
            });
            idx++;
        }
        ctx.FeePlans.AddRange(feePlans);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        // 10) Guardians + Students (~30 student demo set) - Libyan Surnames & Phone prefixes
        var maleFirstNames = new[] { "أحمد", "محمد", "عبدالله", "خالد", "يوسف", "سالم", "ياسين", "بدر", "زياد", "عمر", "حسن", "إبراهيم", "فهد", "مازن", "علي", "طارق", "أسامة", "مصطفى" };
        var femaleFirstNames = new[] { "نور", "سارة", "فاطمة", "ليلى", "مريم", "هدى", "ريم", "لمى", "جنى", "روان", "آلاء", "هيا", "تالة", "ميس", "دانة", "خديجة", "عائشة" };
        var familyNames = new[] { "الترهوني", "الورفلي", "الزوي", "العبيدي", "الفيتوري", "الجراري", "القمودي", "الطرابلسي", "المصري", "السويحلي", "الزوي", "المقرحي" };
        var libyanMobilePrefixes = new[] { "091", "092", "094", "093" };

        var rng = new Random(42);
        var studentNumberSeq = 20250001;

        // Track (StudentFee, its Installments) per student so we can layer
        // demo payments after all rows exist.
        var seededFees = new List<(StudentFee Fee, List<Installment> Installments)>();

        for (var i = 0; i < 30; i++)
        {
            var isMale = i % 2 == 0;
            var first = isMale ? maleFirstNames[rng.Next(maleFirstNames.Length)] : femaleFirstNames[rng.Next(femaleFirstNames.Length)];
            var family = familyNames[rng.Next(familyNames.Length)];
            var fatherFirst = maleFirstNames[rng.Next(maleFirstNames.Length)];

            var phonePrefix = libyanMobilePrefixes[rng.Next(libyanMobilePrefixes.Length)];
            var phoneNum = phonePrefix + rng.Next(1000000, 9999999).ToString();
            var birthYear = 2015 - rng.Next(0, 6);
            var genderDigit = isMale ? "1" : "2";
            var nationalId = $"{genderDigit}{birthYear}{rng.Next(1000000, 9999999)}";

            var guardian = new Guardian
            {
                FullName = $"{fatherFirst} {family}",
                Relation = GuardianRelation.Father,
                Phone = phoneNum,
                NationalId = nationalId,
                Occupation = "موظف",
                Address = "طرابلس"
            };
            ctx.Guardians.Add(guardian);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

            var section = sections[rng.Next(sections.Count)];
            var student = new Student
            {
                StudentNumber = (studentNumberSeq++).ToString(),
                FullName = $"{first} {fatherFirst} {family}",
                Gender = isMale ? Gender.Male : Gender.Female,
                BirthDate = new DateTime(2015 - rng.Next(0, 6), rng.Next(1, 13), rng.Next(1, 28)),
                EnrollmentDate = new DateTime(2025, 9, 1),
                Status = StudentStatus.Active,
                SectionId = section.Id,
                GuardianId = guardian.Id,
                NationalId = "1" + rng.Next(100000000, 999999999).ToString(),
                Phone = guardian.Phone
            };
            ctx.Students.Add(student);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

            // Attach a StudentFee row matching that grade's plan
            var plan = feePlans.First(p => p.GradeId == section.GradeId);
            var fee = new StudentFee
            {
                StudentId = student.Id,
                FeePlanId = plan.Id,
                TotalAmount = plan.TotalAmount,
                PaidAmount = 0m
            };
            ctx.StudentFees.Add(fee);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

            var installmentAmount = Math.Round(plan.TotalAmount / plan.InstallmentsCount, 2);
            var due = new DateTime(2025, 9, 15);
            var feeInstallments = new List<Installment>();
            for (var n = 1; n <= plan.InstallmentsCount; n++)
            {
                var inst = new Installment
                {
                    StudentFeeId = fee.Id,
                    Number = n,
                    Amount = installmentAmount,
                    DueDate = due.AddMonths((n - 1) * 2),
                    Status = InstallmentStatus.Due
                };
                ctx.Installments.Add(inst);
                feeInstallments.Add(inst);
            }
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

            seededFees.Add((fee, feeInstallments));
        }

        // 11) Demo payments — pick 10-12 students from the first 30 and add
        //     1-2 partial payments per student (30-50% of one installment).
        //     Wrapped in a single ExecutionStrategy-compatible transaction to
        //     keep payment+installment+fee updates atomic.
        var paymentRng = new Random(20260519);
        var receiptCounter = 1;
        var receiptDate = new DateTime(2025, 10, 1);

        var paymentTargetCount = paymentRng.Next(10, 13); // 10..12
        var candidateIndexes = Enumerable.Range(0, seededFees.Count)
            .OrderBy(_ => paymentRng.Next())
            .Take(paymentTargetCount)
            .ToList();

        var strategy = ctx.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            foreach (var idxFee in candidateIndexes)
            {
                var (fee, installments) = seededFees[idxFee];
                if (installments.Count == 0) continue;

                var paymentCount = paymentRng.Next(1, 3); // 1..2
                for (var k = 0; k < paymentCount; k++)
                {
                    // Apply to the earliest still-unfinished installment.
                    var target = installments.FirstOrDefault(i => i.PaidAmount < i.Amount - 0.01m);
                    if (target is null) break;

                    var ratio = 0.30 + paymentRng.NextDouble() * 0.20; // 0.30..0.50
                    var amount = Math.Round(target.Amount * (decimal)ratio, 2);
                    if (amount <= 0m) continue;
                    // Never overshoot a single installment in seed data.
                    var remainingOnInst = target.Amount - target.PaidAmount;
                    if (amount > remainingOnInst) amount = remainingOnInst;

                    var receipt = new Payment
                    {
                        ReceiptNumber = $"REC-{receiptDate:yyyyMMdd}-{receiptCounter:0000}",
                        Amount = amount,
                        PaymentDate = receiptDate,
                        Method = PaymentMethod.Cash,
                        StudentFeeId = fee.Id,
                        InstallmentId = target.Id,
                        UserId = admin.Id,
                        Notes = "بذرة تجريبية"
                    };
                    ctx.Payments.Add(receipt);

                    target.PaidAmount += amount;
                    fee.PaidAmount += amount;

                    target.Status = target.PaidAmount >= target.Amount - 0.01m
                        ? InstallmentStatus.Paid
                        : target.PaidAmount > 0m
                            ? InstallmentStatus.PartiallyPaid
                            : InstallmentStatus.Due;

                    receiptCounter++;
                    receiptDate = receiptDate.AddDays(paymentRng.Next(1, 6));
                }
            }

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

            // 12) Overdue: any past-due installment that hasn't been paid at all.
            var today = DateTime.Today;
            foreach (var (_, installments) in seededFees)
            {
                foreach (var inst in installments)
                {
                    if (inst.Status == InstallmentStatus.Due
                        && inst.PaidAmount == 0m
                        && inst.DueDate < today)
                    {
                        inst.Status = InstallmentStatus.Overdue;
                    }
                }
            }

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);

        // 13) Attendance — last 30 weekdays (Sun..Thu) per Active student.
        //     Deterministic via per-student Random(student.Id).
        if (!await ctx.AttendanceRecords.AnyAsync(ct).ConfigureAwait(false))
        {
            // Build a list of the last 30 school weekdays ending today.
            var schoolDays = new List<DateTime>(30);
            var cursor = DateTime.Today;
            while (schoolDays.Count < 30)
            {
                var dow = cursor.DayOfWeek;
                // Skip Friday & Saturday (Arabic school week is Sun..Thu).
                if (dow != DayOfWeek.Friday && dow != DayOfWeek.Saturday)
                    schoolDays.Add(cursor.Date);
                cursor = cursor.AddDays(-1);
            }
            schoolDays.Reverse(); // chronological

            var activeStudents = await ctx.Students
                .Where(s => s.Status == StudentStatus.Active)
                .ToListAsync(ct).ConfigureAwait(false);

            var attendanceRows = new List<AttendanceRecord>(activeStudents.Count * schoolDays.Count);
            foreach (var s in activeStudents)
            {
                var attRng = new Random(s.Id); // deterministic per-student seed
                foreach (var d in schoolDays)
                {
                    var roll = attRng.Next(0, 100); // 0..99
                    AttendanceStatus status;
                    if (roll < 90) status = AttendanceStatus.Present;       // ~90%
                    else if (roll < 95) status = AttendanceStatus.Absent;   // ~5%
                    else if (roll < 98) status = AttendanceStatus.Late;     // ~3%
                    else status = AttendanceStatus.Excused;                 // ~2%

                    attendanceRows.Add(new AttendanceRecord
                    {
                        StudentId = s.Id,
                        Date = d,
                        Status = status
                    });
                }
            }

            await ctx.AttendanceRecords.AddRangeAsync(attendanceRows, ct).ConfigureAwait(false);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // 14) Marks — every Active student × each Subject in their grade × each Exam.
        //     Deterministic via Random(student.Id * 100003 + subject.Id * 97 + exam.Id).
        if (!await ctx.Marks.AnyAsync(ct).ConfigureAwait(false))
        {
            var activeStudents = await ctx.Students
                .Where(s => s.Status == StudentStatus.Active)
                .Include(s => s.Section)
                .ToListAsync(ct).ConfigureAwait(false);

            var allSubjects = await ctx.Subjects.ToListAsync(ct).ConfigureAwait(false);
            var subjectsByGrade = allSubjects.GroupBy(x => x.GradeId)
                                             .ToDictionary(g => g.Key, g => g.ToList());

            var allExams = await ctx.Exams.ToListAsync(ct).ConfigureAwait(false);

            var markRows = new List<Mark>(activeStudents.Count * 6 * allExams.Count);
            foreach (var s in activeStudents)
            {
                if (!subjectsByGrade.TryGetValue(s.Section.GradeId, out var subjList))
                    continue;

                foreach (var subj in subjList)
                {
                    foreach (var ex in allExams)
                    {
                        var mRng = new Random(unchecked(s.Id * 100003 + subj.Id * 97 + ex.Id));

                        // ~5% missing -> leave the mark unwritten.
                        if (mRng.Next(0, 100) < 5) continue;

                        var bucket = mRng.Next(0, 100);
                        decimal value;
                        var max = subj.MaxMark;
                        var pass = subj.PassMark;

                        if (bucket < 5)
                        {
                            // ~5% fail: [0, pass)
                            var range = (double)pass;
                            value = (decimal)Math.Round(mRng.NextDouble() * Math.Max(range - 1, 0), 2);
                        }
                        else if (bucket < 20)
                        {
                            // ~15% high pass: [75% of max, max]
                            var lo = (double)max * 0.75;
                            var hi = (double)max;
                            value = (decimal)Math.Round(lo + mRng.NextDouble() * (hi - lo), 2);
                        }
                        else
                        {
                            // ~80% normal pass: [pass, 75% of max)
                            var lo = (double)pass;
                            var hi = (double)max * 0.75;
                            if (hi <= lo) hi = lo + 1;
                            value = (decimal)Math.Round(lo + mRng.NextDouble() * (hi - lo), 2);
                        }

                        if (value < 0m) value = 0m;
                        if (value > max) value = max;

                        markRows.Add(new Mark
                        {
                            StudentId = s.Id,
                            SubjectId = subj.Id,
                            ExamId = ex.Id,
                            Value = value,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await ctx.Marks.AddRangeAsync(markRows, ct).ConfigureAwait(false);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
