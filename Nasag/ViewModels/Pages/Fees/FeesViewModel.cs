using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nasag.Helpers;
using Nasag.Models;
using Nasag.Repositories;
using Nasag.Services;
using Nasag.ViewModels.Pages;
using Nasag.Views.Pages.Fees.Dialogs;

namespace Nasag.ViewModels.Pages.Fees;

public sealed partial class FeesViewModel : PageViewModel
{
    private readonly IFeesRepository _repo;
    private readonly IToastService _toasts;
    private readonly IErrorReporter _errors;
    private readonly IDialogService _dialogs;
    private readonly ICurrentUserService _currentUser;
    private readonly IConnectionMonitor _connection;
    private readonly List<FeesSectionOption> _allSections = new();
    private bool _isInitializing = true;
    private bool _reloadInFlight;
    private bool _reloadPending;
    private int? _currentStudentFeeId;
    private int _studentsLoadToken;

    // WHY: RecomputeOverdueAsync needs to run once per session (it scans the whole
    // Installments table). Re-running it on every refresh is wasteful and noisy in logs.
    private bool _overdueRecomputed;

    public FeesViewModel(
        IFeesRepository repo,
        IToastService toasts,
        IErrorReporter errors,
        IDialogService dialogs,
        ICurrentUserService currentUser,
        IConnectionMonitor connection)
    {
        _repo = repo;
        _toasts = toasts;
        _errors = errors;
        _dialogs = dialogs;
        _currentUser = currentUser;
        _connection = connection;
        _currentUser.SignedIn  += OnCurrentUserChanged;
        _currentUser.SignedOut += OnCurrentUserChanged;
        _isInitializing = false;

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsLoading))
            {
                OnPropertyChanged(nameof(ShowEmptyStateCta));
                NotifyCommandStates();
            }
        };
    }

    public override string TitleAr => "الرسوم والأقساط";

    public override string SubtitleAr => SelectedStudent is null
        ? "إدارة رسوم الطلاب والأقساط وتسجيل الدفعات"
        : $"{GradeName} - {SectionName} • {StudentFullName}";

    public ObservableCollection<FeesGradeOption> Grades { get; } = new();
    public ObservableCollection<FeesSectionOption> AvailableSections { get; } = new();
    public ObservableCollection<FeesStudentOption> AvailableStudents { get; } = new();
    public ObservableCollection<FeeInstallmentRowViewModel> Installments { get; } = new();
    public ObservableCollection<FeePaymentRowViewModel> Payments { get; } = new();

    [ObservableProperty] private FeesGradeOption? _selectedGrade;
    [ObservableProperty] private FeesSectionOption? _selectedSection;
    [ObservableProperty] private FeesStudentOption? _selectedStudent;

    [ObservableProperty] private string _studentFullName = string.Empty;
    [ObservableProperty] private string _studentNumberDisplay = string.Empty;
    [ObservableProperty] private string _gradeName = string.Empty;
    [ObservableProperty] private string _sectionName = string.Empty;
    [ObservableProperty] private string _feePlanName = string.Empty;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private decimal _remainingAmount;

    // === Stream A: quick locate-by-student-number (VM only — UI is wired later) ===
    [ObservableProperty] private string _quickStudentNumber = string.Empty;

    public int? CurrentStudentFeeId
    {
        get => _currentStudentFeeId;
        private set
        {
            if (_currentStudentFeeId == value) return;
            _currentStudentFeeId = value;
            OnPropertyChanged(nameof(CurrentStudentFeeId));
            OnPropertyChanged(nameof(HasFeePlan));
            OnPropertyChanged(nameof(CanRecordPayment));
            OnPropertyChanged(nameof(ShowEmptyStateCta));
            NotifyCommandStates();
        }
    }

    public bool HasStudentSelected => SelectedStudent is not null;
    public bool HasFeePlan => CurrentStudentFeeId.HasValue;
    public bool CanRecordPayment => HasFeePlan && RemainingAmount > 0m && !IsLoading && CanManageFees;
    public bool HasInstallments => Installments.Count > 0;
    public bool HasPayments => Payments.Count > 0;

    public string TotalAmountText => FormatCurrency(TotalAmount);
    public string PaidAmountText => FormatCurrency(PaidAmount);
    public string RemainingAmountText => FormatCurrency(RemainingAmount);

    public string PaidPercentText
    {
        get
        {
            if (TotalAmount <= 0m) return "0.00%";
            var pct = (double)(PaidAmount / TotalAmount) * 100.0;
            return pct.ToString("0.00", CultureInfo.InvariantCulture) + "%";
        }
    }

    public string InitialLetter
    {
        get
        {
            if (string.IsNullOrWhiteSpace(StudentFullName)) return "?";
            var trimmed = StudentFullName.TrimStart();
            return trimmed.Length == 0 ? "?" : trimmed[0].ToString();
        }
    }

    public string InstallmentsCountText => Installments.Count.ToString("N0");
    public string PaymentsCountText => Payments.Count.ToString("N0");

    private static string FormatCurrency(decimal value) => MoneyFormatter.Format(value);

    partial void OnSelectedGradeChanged(FeesGradeOption? value)
    {
        AvailableSections.Clear();
        if (value is not null)
        {
            foreach (var section in _allSections.Where(s => s.GradeId == value.Id))
                AvailableSections.Add(section);
        }

        SelectedSection = AvailableSections.FirstOrDefault();
        OnPropertyChanged(nameof(SubtitleAr));
    }

    partial void OnSelectedSectionChanged(FeesSectionOption? value)
    {
        OnPropertyChanged(nameof(SubtitleAr));
        if (_isInitializing) return;
        _ = ReloadStudentsForSectionAsync(value);
    }

    partial void OnSelectedStudentChanged(FeesStudentOption? value)
    {
        OnPropertyChanged(nameof(HasStudentSelected));
        OnPropertyChanged(nameof(SubtitleAr));
        OnPropertyChanged(nameof(ShowEmptyStateCta));
        NotifyCommandStates();
        if (_isInitializing) return;
        _ = ReloadDetailsAsync();
    }

    partial void OnStudentFullNameChanged(string value)
    {
        OnPropertyChanged(nameof(InitialLetter));
        OnPropertyChanged(nameof(SubtitleAr));
    }

    partial void OnGradeNameChanged(string value) => OnPropertyChanged(nameof(SubtitleAr));
    partial void OnSectionNameChanged(string value) => OnPropertyChanged(nameof(SubtitleAr));

    partial void OnTotalAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(TotalAmountText));
        OnPropertyChanged(nameof(PaidPercentText));
    }

    partial void OnPaidAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(PaidAmountText));
        OnPropertyChanged(nameof(PaidPercentText));
    }

    partial void OnRemainingAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(RemainingAmountText));
        OnPropertyChanged(nameof(CanRecordPayment));
        NotifyCommandStates();
    }

    public override async Task ActivateAsync(CancellationToken ct = default)
    {
        if (Grades.Count == 0)
            await LoadLookupsAsync(ct).ConfigureAwait(true);
        if (SelectedStudent is not null)
            await ReloadDetailsAsync(ct).ConfigureAwait(true);
    }

    [RelayCommand]
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await LoadLookupsAsync(ct).ConfigureAwait(true);
        if (SelectedStudent is not null)
            await ReloadDetailsAsync(ct).ConfigureAwait(true);
    }

    private async Task LoadLookupsAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            NotifyCommandStates();
            StatusMessage = null;
            _isInitializing = true;

            // Run the once-per-session overdue sweep up-front so installments shown
            // in the picker reflect today's status. Failures here are non-fatal —
            // the screen can still load with stale "Due" rows.
            if (!_overdueRecomputed)
            {
                try
                {
                    await _repo.RecomputeOverdueAsync(ct).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _errors.Report("تعذر تحديث حالة الأقساط المتأخرة", ex.Message, ex);
                }
                finally
                {
                    _overdueRecomputed = true;
                }
            }

            var keepGradeId = SelectedGrade?.Id;
            var keepSectionId = SelectedSection?.Id;
            var keepStudentId = SelectedStudent?.Id;

            var lookups = await _repo.GetLookupsAsync(ct).ConfigureAwait(true);

            Grades.Clear();
            foreach (var g in lookups.Grades) Grades.Add(g);

            _allSections.Clear();
            _allSections.AddRange(lookups.Sections);

            SelectedGrade = (keepGradeId.HasValue ? Grades.FirstOrDefault(g => g.Id == keepGradeId.Value) : null)
                ?? Grades.FirstOrDefault();

            AvailableSections.Clear();
            if (SelectedGrade is not null)
            {
                foreach (var s in _allSections.Where(s => s.GradeId == SelectedGrade.Id))
                    AvailableSections.Add(s);
            }

            SelectedSection = (keepSectionId.HasValue
                    ? AvailableSections.FirstOrDefault(s => s.Id == keepSectionId.Value)
                    : null)
                ?? AvailableSections.FirstOrDefault();

            // Repopulate students for the selected section (synchronously inside this lookup load)
            await ReloadStudentsForSectionAsync(SelectedSection, ct, restoreStudentId: keepStudentId).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل قوائم الرسوم.";
            _errors.Report("تعذر تحميل قوائم الرسوم", ex.Message, ex);
        }
        finally
        {
            _isInitializing = false;
            IsLoading = false;
            OnPropertyChanged(nameof(SubtitleAr));
            NotifyCommandStates();
        }
    }

    private async Task ReloadStudentsForSectionAsync(
        FeesSectionOption? section,
        CancellationToken ct = default,
        int? restoreStudentId = null)
    {
        // Race guard: capture token; if SelectedSection changes mid-flight, drop the stale result.
        var token = ++_studentsLoadToken;
        try
        {
            var previousInitializing = _isInitializing;
            _isInitializing = true;

            AvailableStudents.Clear();
            if (section is null)
            {
                SelectedStudent = null;
                ClearStudentDetails();
                _isInitializing = previousInitializing;
                return;
            }

            var students = await _repo.GetStudentsForSectionAsync(section.Id, ct).ConfigureAwait(true);

            // Drop result if a newer load was requested or the user moved off this section.
            if (token != _studentsLoadToken || SelectedSection?.Id != section.Id)
            {
                _isInitializing = previousInitializing;
                return;
            }

            foreach (var s in students) AvailableStudents.Add(s);

            // Restore selection or clear details when none.
            FeesStudentOption? next = null;
            if (restoreStudentId.HasValue)
                next = AvailableStudents.FirstOrDefault(s => s.Id == restoreStudentId.Value);

            SelectedStudent = next;
            if (next is null)
                ClearStudentDetails();

            _isInitializing = previousInitializing;

            if (next is not null && !previousInitializing)
                await ReloadDetailsAsync(ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر تحميل قائمة الطلاب", ex.Message, ex);
        }
    }

    private void ClearStudentDetails()
    {
        Installments.Clear();
        Payments.Clear();
        StudentFullName = string.Empty;
        StudentNumberDisplay = string.Empty;
        GradeName = string.Empty;
        SectionName = string.Empty;
        FeePlanName = string.Empty;
        TotalAmount = 0m;
        PaidAmount = 0m;
        RemainingAmount = 0m;
        CurrentStudentFeeId = null;
        OnPropertyChanged(nameof(HasInstallments));
        OnPropertyChanged(nameof(HasPayments));
        OnPropertyChanged(nameof(InstallmentsCountText));
        OnPropertyChanged(nameof(PaymentsCountText));
        OnPropertyChanged(nameof(HasFeePlan));
        OnPropertyChanged(nameof(ShowEmptyStateCta));
    }

    private async Task ReloadDetailsAsync(CancellationToken ct = default)
    {
        if (_reloadInFlight)
        {
            _reloadPending = true;
            return;
        }

        if (SelectedStudent is null)
        {
            ClearStudentDetails();
            return;
        }

        _reloadInFlight = true;
        try
        {
            IsLoading = true;
            NotifyCommandStates();
            StatusMessage = null;

            do
            {
                _reloadPending = false;
                var requestedStudent = SelectedStudent;
                if (requestedStudent is null)
                {
                    ClearStudentDetails();
                    return;
                }

                var details = await _repo.GetStudentDetailsAsync(requestedStudent.Id, ct).ConfigureAwait(true);

                if (SelectedStudent?.Id != requestedStudent.Id)
                {
                    _reloadPending = true;
                    continue;
                }

                if (details is null)
                {
                    ClearStudentDetails();
                    StudentFullName = requestedStudent.FullName;
                    StudentNumberDisplay = requestedStudent.StudentNumber;
                    continue;
                }

                StudentFullName = details.FullName;
                StudentNumberDisplay = details.StudentNumber;
                GradeName = details.GradeName;
                SectionName = details.SectionName;
                FeePlanName = details.FeePlanName ?? string.Empty;
                TotalAmount = details.TotalAmount;
                PaidAmount = details.PaidAmount;
                RemainingAmount = details.RemainingAmount;
                CurrentStudentFeeId = details.StudentFeeId;

                Installments.Clear();
                foreach (var row in details.Installments)
                    Installments.Add(new FeeInstallmentRowViewModel(row));

                Payments.Clear();
                foreach (var p in details.Payments)
                    Payments.Add(new FeePaymentRowViewModel(p));

                OnPropertyChanged(nameof(HasInstallments));
                OnPropertyChanged(nameof(HasPayments));
                OnPropertyChanged(nameof(InstallmentsCountText));
                OnPropertyChanged(nameof(PaymentsCountText));
            } while (_reloadPending);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StatusMessage = "تعذر تحميل بيانات الرسوم.";
            _errors.Report("تعذر تحميل بيانات الرسوم", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            _reloadInFlight = false;
            NotifyCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRecordPayment))]
    private async Task RecordPaymentAsync(object? parameter)
    {
        if (!CurrentStudentFeeId.HasValue)
        {
            _toasts.Warning("لا توجد خطة رسوم", "لا توجد خطة رسوم نشطة لهذا الطالب.");
            return;
        }

        if (!await EnsureConnectedAsync().ConfigureAwait(true)) return;

        int? installmentId = parameter switch
        {
            int i => i,
            FeeInstallmentRowViewModel row => row.Id,
            _ => null
        };

        var choices = Installments
            .Select(i => new InstallmentChoice(
                i.Id,
                i.Number,
                i.RemainingAmount,
                $"القسط {i.Number} — متبقي {FormatCurrency(i.RemainingAmount)}"))
            .ToList();

        var userId = _currentUser.User?.Id ?? 0;

        var model = PaymentDialog.Show(
            CurrentStudentFeeId.Value,
            StudentFullName,
            RemainingAmount,
            choices,
            installmentId,
            userId);

        if (model is null) return;

        try
        {
            IsLoading = true;
            var result = await _repo.RecordPaymentAsync(model).ConfigureAwait(true);
            _toasts.Success("تم تسجيل الدفعة", $"رقم السند: {result.ReceiptNumber}");
            await ReloadDetailsAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException uae)
        {
            _toasts.Warning("صلاحية مرفوضة", uae.Message);
        }
        catch (InvalidOperationException ex)
        {
            _toasts.Warning("تحقق الإدخال", ex.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر تسجيل الدفعة", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeletePayment))]
    private async Task DeletePaymentAsync(FeePaymentRowViewModel? row)
    {
        if (row is null) return;

        if (!await EnsureConnectedAsync().ConfigureAwait(true)) return;

        var ok = await _dialogs.ConfirmDestructiveAsync(
            "حذف الدفعة",
            $"سيتم حذف الإيصال {row.ReceiptNumber} بمبلغ {row.AmountText}. هل تريد المتابعة؟");
        if (!ok) return;

        try
        {
            IsLoading = true;
            await _repo.DeletePaymentAsync(row.Id).ConfigureAwait(true);
            _toasts.Success("تم حذف الدفعة", $"رقم السند: {row.ReceiptNumber}");
            await ReloadDetailsAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException uae)
        {
            _toasts.Warning("صلاحية مرفوضة", uae.Message);
        }
        catch (InvalidOperationException ex)
        {
            _toasts.Warning("تحقق", ex.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر حذف الدفعة", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NotifyCommandStates()
    {
        OnPropertyChanged(nameof(CanRecordPayment));
        RecordPaymentCommand.NotifyCanExecuteChanged();
        DeletePaymentCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanManageFees));
        AssignFeePlanCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ShowEmptyStateCta));
        PrintStatementCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanPrintStatement));
    }

    // === Stream C: Permission + cross-link ===

    public bool CanManageFees => _currentUser.HasPermission(Permission.ManageFees);

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        _overdueRecomputed = false;
        OnPropertyChanged(nameof(CanManageFees));
        NotifyCommandStates();
    }

    private bool CanDeletePayment(FeePaymentRowViewModel? row) =>
        row is not null && CanManageFees && !IsLoading;

    public async Task PrepareForStudentAsync(int studentId, CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;
            if (Grades.Count == 0)
                await LoadLookupsAsync(ct).ConfigureAwait(true);
            var loc = await _repo.LocateStudentAsync(studentId, ct).ConfigureAwait(true);
            if (loc is null)
            {
                _toasts.Warning("الطالب غير موجود", "تعذر تحديد موقع الطالب.");
                return;
            }
            _isInitializing = true;
            SelectedGrade = Grades.FirstOrDefault(g => g.Id == loc.GradeId);
            // SelectedGradeChanged populated AvailableSections synchronously above.
            SelectedSection = AvailableSections.FirstOrDefault(s => s.Id == loc.SectionId);
            _isInitializing = false;

            await ReloadStudentsForSectionAsync(SelectedSection, ct, restoreStudentId: studentId)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر فتح رسوم الطالب", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
            NotifyCommandStates();
        }
    }

    // === Stream A: quick locate by student number ===

    [RelayCommand]
    private async Task QuickLocateAsync(CancellationToken ct = default)
    {
        var raw = QuickStudentNumber?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            _toasts.Warning("بحث سريع", "اكتب رقم الطالب أولاً.");
            return;
        }
        try
        {
            IsLoading = true;
            var hit = await _repo.LocateStudentByNumberAsync(raw, ct).ConfigureAwait(true);
            if (hit is null)
            {
                _toasts.Warning("بحث سريع", $"لم يُعثر على طالب برقم {raw}.");
                return;
            }
            await PrepareForStudentAsync(hit.StudentId, ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر البحث السريع", ex.Message, ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // === Stream A: Assign FeePlan ===

    public bool ShowEmptyStateCta =>
        HasStudentSelected && !HasFeePlan && !IsLoading && CanManageFees;

    private bool CanAssignFeePlan() =>
        SelectedStudent is not null
        && SelectedGrade is not null
        && !HasFeePlan
        && !IsLoading
        && CanManageFees;

    [RelayCommand(CanExecute = nameof(CanAssignFeePlan))]
    private async Task AssignFeePlanAsync(CancellationToken ct = default)
    {
        if (SelectedStudent is null || SelectedGrade is null) return;
        if (!await EnsureConnectedAsync().ConfigureAwait(true)) return;

        try
        {
            IsLoading = true; NotifyCommandStates();
            var plans = await _repo.GetAssignablePlansAsync(SelectedGrade.Id, ct).ConfigureAwait(true);
            var result = AssignFeePlanDialog.Show(StudentFullName, GradeName, plans);
            if (result is null) return;
            await _repo.AssignFeePlanAsync(SelectedStudent.Id, result.FeePlanId, ct).ConfigureAwait(true);
            _toasts.Success("تم تعيين خطة الرسوم", StudentFullName);
            await ReloadDetailsAsync(ct).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException uae)
        {
            _toasts.Warning("صلاحية مرفوضة", uae.Message);
        }
        catch (InvalidOperationException ioe)
        {
            _toasts.Warning("تعذر تعيين الخطة", ioe.Message);
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر تعيين خطة الرسوم", ex.Message, ex);
        }
        finally
        {
            IsLoading = false; NotifyCommandStates();
        }
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (_connection.IsConnected) return true;
        await _dialogs.ShowWarningAsync("تعذّر الاتصال بقاعدة البيانات.", "يرجى التحقق من الاتصال ثم إعادة المحاولة.")
            .ConfigureAwait(true);
        return false;
    }

    // === Stream B: Receipt & Statement printing ===
    //
    // WHY: school header is fetched fresh on every print (no caching). Printing is a
    // rare user action so the extra round-trip is negligible, and it guarantees a
    // settings change in another window is reflected immediately.

    private static string FormatMethod(Nasag.Models.PaymentMethod m) => m switch
    {
        Nasag.Models.PaymentMethod.Cash => "نقدي",
        Nasag.Models.PaymentMethod.BankTransfer => "تحويل بنكي",
        Nasag.Models.PaymentMethod.Card => "بطاقة",
        Nasag.Models.PaymentMethod.Cheque => "شيك",
        _ => "أخرى"
    };

    [RelayCommand]
    private async Task PrintReceiptAsync(FeePaymentRowViewModel? row)
    {
        if (row is null) return;
        try
        {
            var header = await _repo.GetSchoolHeaderAsync(CancellationToken.None).ConfigureAwait(true);
            var paymentDateLocal = row.PaymentDate.Kind == DateTimeKind.Utc
                ? row.PaymentDate.ToLocalTime()
                : row.PaymentDate;
            var model = new Nasag.Services.Printing.ReceiptModel(
                header.NameAr,
                header.Address,
                header.Phone,
                row.ReceiptNumber,
                paymentDateLocal,
                StudentFullName,
                StudentNumberDisplay,
                GradeName,
                SectionName,
                row.Amount,
                Nasag.Services.Printing.ArabicNumberWords.Convert(row.Amount),
                row.Method,
                FormatMethod(row.Method),
                row.InstallmentNumber,
                row.Notes,
                row.UserName);
            var doc = Nasag.Services.Printing.ReceiptDocument.Build(model);
            Nasag.Services.Printing.PrintService.PreviewAndPrint(doc, $"سند قبض رقم {row.ReceiptNumber}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر تجهيز سند القبض", ex.Message, ex);
        }
    }

    public bool CanPrintStatement => HasFeePlan && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanPrintStatement))]
    private async Task PrintStatementAsync(CancellationToken ct = default)
    {
        if (!HasFeePlan) return;
        try
        {
            var header = await _repo.GetSchoolHeaderAsync(ct).ConfigureAwait(true);
            var installments = Installments
                .Select(i => new Nasag.Services.Printing.StatementInstallmentRow(
                    i.Number, i.Amount, i.PaidAmount, i.RemainingAmount,
                    i.DueDate, i.StatusLabelAr))
                .ToList();
            var payments = Payments
                .Select(p =>
                {
                    var local = p.PaymentDate.Kind == DateTimeKind.Utc ? p.PaymentDate.ToLocalTime() : p.PaymentDate;
                    return new Nasag.Services.Printing.StatementPaymentRow(
                        p.ReceiptNumber, local, p.Amount,
                        FormatMethod(p.Method), p.InstallmentNumber, p.Notes);
                })
                .ToList();
            var model = new Nasag.Services.Printing.StatementModel(
                header.NameAr, header.Address, header.Phone,
                DateTime.Now,
                StudentFullName, StudentNumberDisplay, GradeName, SectionName,
                string.IsNullOrWhiteSpace(FeePlanName) ? "—" : FeePlanName,
                TotalAmount, PaidAmount, RemainingAmount,
                installments, payments);
            var doc = Nasag.Services.Printing.StatementDocument.Build(model);
            Nasag.Services.Printing.PrintService.PreviewAndPrint(doc, $"كشف حساب — {StudentFullName}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errors.Report("تعذر تجهيز كشف الحساب", ex.Message, ex);
        }
    }
}

public sealed class FeeInstallmentRowViewModel
{
    public FeeInstallmentRowViewModel(FeeInstallmentRow row)
    {
        Id = row.Id;
        Number = row.Number;
        Amount = row.Amount;
        PaidAmount = row.PaidAmount;
        RemainingAmount = row.RemainingAmount;
        DueDate = row.DueDate;
        Status = row.Status;
        LastPaymentDate = row.LastPaymentDate;
    }

    public int Id { get; }
    public int Number { get; }
    public decimal Amount { get; }
    public decimal PaidAmount { get; }
    public decimal RemainingAmount { get; }
    public DateTime DueDate { get; }
    public InstallmentStatus Status { get; }
    public DateTime? LastPaymentDate { get; }

    public string AmountText => MoneyFormatter.Format(Amount);
    public string PaidAmountText => MoneyFormatter.Format(PaidAmount);
    public string RemainingAmountText => MoneyFormatter.Format(RemainingAmount);
    public string DueDateText => DueDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
    public string LastPaymentDateText => LastPaymentDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? "—";

    public string StatusLabelAr => Status switch
    {
        InstallmentStatus.Due => "مستحق",
        InstallmentStatus.Paid => "مدفوع",
        InstallmentStatus.PartiallyPaid => "مدفوع جزئياً",
        InstallmentStatus.Overdue => "متأخر",
        _ => string.Empty
    };
}

public sealed class FeePaymentRowViewModel
{
    public FeePaymentRowViewModel(FeePaymentRow row)
    {
        Id = row.Id;
        ReceiptNumber = row.ReceiptNumber;
        Amount = row.Amount;
        PaymentDate = row.PaymentDate;
        Method = row.Method;
        Notes = row.Notes;
        InstallmentId = row.InstallmentId;
        InstallmentNumber = row.InstallmentNumber;
        UserId = row.UserId;
        UserName = row.UserName;
    }

    public int Id { get; }
    public string ReceiptNumber { get; }
    public decimal Amount { get; }
    public DateTime PaymentDate { get; }
    public PaymentMethod Method { get; }
    public string? Notes { get; }
    public int? InstallmentId { get; }
    public int? InstallmentNumber { get; }
    public int UserId { get; }
    public string UserName { get; }

    public string AmountText => MoneyFormatter.Format(Amount);
    public string PaymentDateText
    {
        get
        {
            var local = PaymentDate.Kind == DateTimeKind.Utc ? PaymentDate.ToLocalTime() : PaymentDate;
            return local.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }
    }

    public string MethodLabelAr => Method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.BankTransfer => "تحويل بنكي",
        PaymentMethod.Card => "بطاقة",
        PaymentMethod.Cheque => "شيك",
        PaymentMethod.Other => "أخرى",
        _ => string.Empty
    };

    public string InstallmentLabel => InstallmentNumber.HasValue ? $"القسط {InstallmentNumber.Value}" : "—";
}

public sealed record InstallmentChoice(int Id, int Number, decimal Remaining, string Display);
