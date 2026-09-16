using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Nasag.Helpers;
using Nasag.Repositories;

namespace Nasag.Services;

/// <summary>
/// .xlsx writer/reader for the Students Import/Export feature.
/// Headers are Arabic and frozen; columns auto-fit; banded rows for legibility.
/// </summary>
public sealed class ExcelService : IExcelService
{
    private static readonly string[] Headers =
    {
        "رقم الطالب",
        "الاسم الكامل",
        "الجنس",
        "تاريخ الميلاد",
        "رقم الهوية",
        "جوال الطالب",
        "الصف",
        "الشعبة",
        "تاريخ التسجيل",
        "الحالة",
        "عنوان الطالب",
        "اسم ولي الأمر",
        "صلة القرابة",
        "جوال ولي الأمر",
        "جوال احتياطي",
        "البريد الإلكتروني",
        "هوية ولي الأمر",
        "مهنة ولي الأمر",
        "عنوان ولي الأمر",
        "ملاحظات",
    };

    public Task ExportStudentsAsync(string filePath, IReadOnlyList<StudentExportRow> rows, CancellationToken ct = default)
        => Task.Run(() => ExportInternal(filePath, rows, ct), ct);

    public Task<IReadOnlyList<StudentImportRow>> ReadStudentsAsync(string filePath, CancellationToken ct = default)
        => Task.Run(() => ReadInternal(filePath, ct), ct);

    public Task WriteTemplateAsync(string filePath, CancellationToken ct = default)
        => Task.Run(() => ExportInternal(filePath, Array.Empty<StudentExportRow>(), ct), ct);

    private static void ExportInternal(string filePath, IReadOnlyList<StudentExportRow> rows, CancellationToken ct)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("الطلاب");
        ws.RightToLeft = true;

        // Header
        for (var i = 0; i < Headers.Length; i++)
            ws.Cell(1, i + 1).Value = Headers[i];
        var headerRange = ws.Range(1, 1, 1, Headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1FB5A8");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.SheetView.FreezeRows(1);
        ws.Row(1).Height = 24;

        // Body
        for (var r = 0; r < rows.Count; r++)
        {
            ct.ThrowIfCancellationRequested();
            var row = rows[r];
            var i = r + 2; // 1-based, header row is 1
            ws.Cell(i, 1).Value = row.StudentNumber;
            ws.Cell(i, 2).Value = row.FullName;
            ws.Cell(i, 3).Value = row.Gender;
            ws.Cell(i, 4).Value = row.BirthDate;
            ws.Cell(i, 5).Value = row.NationalId;
            ws.Cell(i, 6).Value = row.Phone;
            ws.Cell(i, 7).Value = row.GradeName;
            ws.Cell(i, 8).Value = row.SectionName;
            ws.Cell(i, 9).Value = row.EnrollmentDate;
            ws.Cell(i, 10).Value = row.Status;
            ws.Cell(i, 11).Value = row.Address;
            ws.Cell(i, 12).Value = row.GuardianFullName;
            ws.Cell(i, 13).Value = row.GuardianRelation;
            ws.Cell(i, 14).Value = row.GuardianPhone;
            ws.Cell(i, 15).Value = row.GuardianAltPhone;
            ws.Cell(i, 16).Value = row.GuardianEmail;
            ws.Cell(i, 17).Value = row.GuardianNationalId;
            ws.Cell(i, 18).Value = row.GuardianOccupation;
            ws.Cell(i, 19).Value = row.GuardianAddress;
            ws.Cell(i, 20).Value = row.Notes;
        }

        if (rows.Count > 0)
        {
            var body = ws.Range(2, 1, rows.Count + 1, Headers.Length);
            body.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            body.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            body.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            body.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            body.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CFD6E0");
            body.Style.Border.InsideBorderColor = XLColor.FromHtml("#E5E9F0");
        }

        ws.Columns().AdjustToContents(minWidth: 12, maxWidth: 36);
        ws.SheetView.View = XLSheetViewOptions.Normal;

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        wb.SaveAs(filePath);
    }

    private static IReadOnlyList<StudentImportRow> ReadInternal(string filePath, CancellationToken ct)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var result = new List<StudentImportRow>(Math.Max(0, lastRow - 1));

        // Build a header→column lookup so import is resilient to column re-ordering.
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = ws.Row(1);
        for (var c = 1; c <= 20; c++)
        {
            var key = headerRow.Cell(c).GetString().Trim();
            if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key)) map[key] = c;
        }

        string? Get(IXLRow row, string headerKey)
        {
            if (!map.TryGetValue(headerKey, out var col)) return null;
            var cell = row.Cell(col);
            var s = cell.IsEmpty() ? null : cell.GetString().Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        for (var r = 2; r <= lastRow; r++)
        {
            ct.ThrowIfCancellationRequested();
            var row = ws.Row(r);
            if (row.IsEmpty()) continue;

            result.Add(new StudentImportRow
            {
                RowNumber = r,
                StudentNumber = Get(row, "رقم الطالب"),
                FullName = Get(row, "الاسم الكامل"),
                Gender = Get(row, "الجنس"),
                BirthDate = Get(row, "تاريخ الميلاد"),
                NationalId = Get(row, "رقم الهوية"),
                Phone = Get(row, "جوال الطالب"),
                GradeName = Get(row, "الصف"),
                SectionName = Get(row, "الشعبة"),
                EnrollmentDate = Get(row, "تاريخ التسجيل"),
                Address = Get(row, "عنوان الطالب"),
                GuardianFullName = Get(row, "اسم ولي الأمر"),
                GuardianRelation = Get(row, "صلة القرابة"),
                GuardianPhone = Get(row, "جوال ولي الأمر"),
                GuardianAltPhone = Get(row, "جوال احتياطي"),
                GuardianEmail = Get(row, "البريد الإلكتروني"),
                GuardianNationalId = Get(row, "هوية ولي الأمر"),
                GuardianOccupation = Get(row, "مهنة ولي الأمر"),
                GuardianAddress = Get(row, "عنوان ولي الأمر"),
                Notes = Get(row, "ملاحظات"),
            });
        }

        return result;
    }

    // ===========================================================================
    // Phase 11 — Report exports (Students / Attendance / Marks / Fees)
    // Each writes a single styled worksheet: filter header, frozen column row,
    // banded body, totals row, autofit. ar-SA culture for percentages so digits
    // render as Arabic. Currency cells are written as decimals so Excel handles
    // formatting (more useful than baked strings for downstream analysis).
    // ===========================================================================

    private const string CurrencyFormat = "#,##0.000 \"د.ل\"";
    private static readonly CultureInfo ArLy = TryGetArLyCulture();

    private static CultureInfo TryGetArLyCulture()
    {
        try { return CultureInfo.GetCultureInfo("ar-LY"); }
        catch { return CultureInfo.InvariantCulture; }
    }

    public Task ExportStudentsReportAsync(string filePath, StudentsReportResult result, CancellationToken ct = default)
        => Task.Run(() => ExportStudentsReportInternal(filePath, result, ct), ct);

    public Task ExportAttendanceReportAsync(string filePath, AttendanceReportResult result, CancellationToken ct = default)
        => Task.Run(() => ExportAttendanceReportInternal(filePath, result, ct), ct);

    public Task ExportMarksReportAsync(string filePath, MarksReportResult result, CancellationToken ct = default)
        => Task.Run(() => ExportMarksReportInternal(filePath, result, ct), ct);

    public Task ExportFeesReportAsync(string filePath, FeesReportResult result, CancellationToken ct = default)
        => Task.Run(() => ExportFeesReportInternal(filePath, result, ct), ct);

    // ---------- Students ----------

    private static void ExportStudentsReportInternal(string filePath, StudentsReportResult r, CancellationToken ct)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("تقرير الطلاب");
        ws.RightToLeft = true;

        var headers = new[] { "#", "رقم الطالب", "الاسم", "الجنس", "الصف", "الشعبة", "ولي الأمر", "الهاتف", "الحالة" };
        var colCount = headers.Length;

        WriteReportTopMatter(ws,
            colCount,
            schoolHeader: $"المدرسة: {r.SchoolNameAr} — السنة: {r.AcademicYearAr} — تاريخ الإنشاء: {r.GeneratedAt:yyyy/MM/dd HH:mm}",
            filterSummary: BuildStudentsSummary(r));

        for (var i = 0; i < headers.Length; i++) ws.Cell(3, i + 1).Value = headers[i];
        StyleHeaderRow(ws, 3, colCount);
        ws.SheetView.FreezeRows(3);

        var startRow = 4;
        for (var i = 0; i < r.Rows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var row = r.Rows[i];
            var rr = startRow + i;
            ws.Cell(rr, 1).Value = i + 1;
            ws.Cell(rr, 2).Value = row.StudentNumber;
            ws.Cell(rr, 3).Value = row.FullName;
            ws.Cell(rr, 4).Value = row.GenderAr;
            ws.Cell(rr, 5).Value = row.GradeNameAr;
            ws.Cell(rr, 6).Value = row.SectionNameAr;
            ws.Cell(rr, 7).Value = row.GuardianNameAr ?? string.Empty;
            ws.Cell(rr, 8).Value = row.GuardianPhone ?? string.Empty;
            ws.Cell(rr, 9).Value = row.StatusAr;
        }
        StyleBody(ws, startRow, r.Rows.Count, colCount);

        if (r.Rows.Count > 0)
        {
            var totalsRow = startRow + r.Rows.Count;
            ws.Cell(totalsRow, 1).Value = $"إجمالي: {r.Rows.Count} طالب";
            ws.Range(totalsRow, 1, totalsRow, colCount).Merge();
            StyleTotalsRow(ws, totalsRow, colCount);
        }

        ws.Columns().AdjustToContents(minWidth: 12, maxWidth: 36);
        SaveWorkbook(wb, filePath);
    }

    // ---------- Attendance ----------

    private static void ExportAttendanceReportInternal(string filePath, AttendanceReportResult r, CancellationToken ct)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("كشف الحضور");
        ws.RightToLeft = true;

        var headers = new[] { "#", "رقم", "الاسم", "الصف", "الشعبة", "حاضر", "غائب", "متأخر", "إجازة", "أيام", "النسبة%" };
        var colCount = headers.Length;

        WriteReportTopMatter(ws,
            colCount,
            schoolHeader: $"المدرسة: {r.SchoolNameAr} — السنة: {r.AcademicYearAr} — تاريخ الإنشاء: {r.GeneratedAt:yyyy/MM/dd HH:mm}",
            filterSummary: BuildAttendanceSummary(r));

        for (var i = 0; i < headers.Length; i++) ws.Cell(3, i + 1).Value = headers[i];
        StyleHeaderRow(ws, 3, colCount);
        ws.SheetView.FreezeRows(3);

        var startRow = 4;
        for (var i = 0; i < r.Rows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var row = r.Rows[i];
            var rr = startRow + i;
            ws.Cell(rr, 1).Value = i + 1;
            ws.Cell(rr, 2).Value = row.StudentNumber;
            ws.Cell(rr, 3).Value = row.FullName;
            ws.Cell(rr, 4).Value = row.GradeNameAr;
            ws.Cell(rr, 5).Value = row.SectionNameAr;
            ws.Cell(rr, 6).Value = row.Present;
            ws.Cell(rr, 7).Value = row.Absent;
            ws.Cell(rr, 8).Value = row.Late;
            ws.Cell(rr, 9).Value = row.Excused;
            ws.Cell(rr, 10).Value = row.RecordedDays;
            ws.Cell(rr, 11).Value = row.AttendancePercentage / 100.0;
            ws.Cell(rr, 11).Style.NumberFormat.Format = "0.0%";
        }
        StyleBody(ws, startRow, r.Rows.Count, colCount);

        if (r.Rows.Count > 0)
        {
            var totalsRow = startRow + r.Rows.Count;
            ws.Cell(totalsRow, 1).Value = "الإجماليات";
            ws.Cell(totalsRow, 6).Value = r.Totals.TotalPresent;
            ws.Cell(totalsRow, 7).Value = r.Totals.TotalAbsent;
            ws.Cell(totalsRow, 8).Value = r.Totals.TotalLate;
            ws.Cell(totalsRow, 9).Value = r.Totals.TotalExcused;
            ws.Cell(totalsRow, 10).Value = r.Totals.RecordedDays;
            ws.Cell(totalsRow, 11).Value = r.Totals.AveragePercentage / 100.0;
            ws.Cell(totalsRow, 11).Style.NumberFormat.Format = "0.0%";
            StyleTotalsRow(ws, totalsRow, colCount);
        }

        ws.Columns().AdjustToContents(minWidth: 10, maxWidth: 32);
        SaveWorkbook(wb, filePath);
    }

    // ---------- Marks ----------

    private static void ExportMarksReportInternal(string filePath, MarksReportResult r, CancellationToken ct)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("كشف الدرجات");
        ws.RightToLeft = true;

        var subjectCount = r.Columns.Count;
        // Layout: # / رقم / الاسم / [subjects] / المجموع / النسبة% / التقدير / الحالة
        var colCount = 3 + subjectCount + 4;

        WriteReportTopMatter(ws,
            colCount,
            schoolHeader: $"المدرسة: {r.SchoolNameAr} — السنة: {r.AcademicYearAr} — تاريخ الإنشاء: {r.GeneratedAt:yyyy/MM/dd HH:mm}",
            filterSummary: $"الصف: {r.GradeNameAr} • الشعبة: {r.SectionNameAr} • الامتحان: {r.ExamNameAr}");

        ws.Cell(3, 1).Value = "#";
        ws.Cell(3, 2).Value = "رقم";
        ws.Cell(3, 3).Value = "الاسم";
        for (var i = 0; i < subjectCount; i++)
        {
            var col = r.Columns[i];
            ws.Cell(3, 4 + i).Value = $"{col.SubjectNameAr} /{col.MaxMark}";
        }
        var tail = 3 + subjectCount;
        ws.Cell(3, tail + 1).Value = "المجموع";
        ws.Cell(3, tail + 2).Value = "النسبة%";
        ws.Cell(3, tail + 3).Value = "التقدير";
        ws.Cell(3, tail + 4).Value = "الحالة";

        StyleHeaderRow(ws, 3, colCount);
        ws.SheetView.FreezeRows(3);
        ws.Row(3).Style.Alignment.WrapText = true;

        var startRow = 4;
        for (var i = 0; i < r.Rows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var row = r.Rows[i];
            var rr = startRow + i;
            ws.Cell(rr, 1).Value = i + 1;
            ws.Cell(rr, 2).Value = row.StudentNumber;
            ws.Cell(rr, 3).Value = row.FullName;

            for (var c = 0; c < subjectCount; c++)
            {
                var col = r.Columns[c];
                var cell = row.Cells.FirstOrDefault(x => x.SubjectId == col.SubjectId);
                var xc = ws.Cell(rr, 4 + c);
                if (cell is null) xc.Value = string.Empty;
                else if (cell.IsAbsent) xc.Value = "غ";
                else if (cell.Mark.HasValue) xc.Value = (double)cell.Mark.Value;
                else xc.Value = string.Empty;
            }

            ws.Cell(rr, tail + 1).Value = (double)row.Total;
            ws.Cell(rr, tail + 2).Value = row.Percentage / 100.0;
            ws.Cell(rr, tail + 2).Style.NumberFormat.Format = "0.0%";
            ws.Cell(rr, tail + 3).Value = row.GradeLabelAr ?? string.Empty;
            ws.Cell(rr, tail + 4).Value = row.StatusAr;

            // Status colour tint
            var statusCell = ws.Cell(rr, tail + 4);
            if (row.IsPending) statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            else if (row.IsPassed) statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
            else statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
        }
        StyleBody(ws, startRow, r.Rows.Count, colCount);

        if (r.Rows.Count > 0)
        {
            var totalsRow = startRow + r.Rows.Count;
            ws.Cell(totalsRow, 1).Value =
                $"ناجح: {r.Totals.PassedCount} • راسب: {r.Totals.FailedCount} • غير مكتمل: {r.Totals.PendingCount} • المتوسط: {r.Totals.AveragePercentage.ToString("0.0", ArLy)}%";
            ws.Range(totalsRow, 1, totalsRow, colCount).Merge();
            StyleTotalsRow(ws, totalsRow, colCount);
        }

        ws.Columns().AdjustToContents(minWidth: 10, maxWidth: 32);
        SaveWorkbook(wb, filePath);
    }

    // ---------- Fees ----------

    private static void ExportFeesReportInternal(string filePath, FeesReportResult r, CancellationToken ct)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("كشف الرسوم");
        ws.RightToLeft = true;

        var headers = new[]
        {
            "#", "رقم", "الاسم", "الصف", "الشعبة", "الخطة",
            "الإجمالي", "المدفوع", "المتبقي", "متأخرات", "الحالة"
        };
        var colCount = headers.Length;

        WriteReportTopMatter(ws,
            colCount,
            schoolHeader: $"المدرسة: {r.SchoolNameAr} — السنة: {r.AcademicYearAr} — تاريخ الإنشاء: {r.GeneratedAt:yyyy/MM/dd HH:mm}",
            filterSummary: BuildFeesSummary(r));

        for (var i = 0; i < headers.Length; i++) ws.Cell(3, i + 1).Value = headers[i];
        StyleHeaderRow(ws, 3, colCount);
        ws.SheetView.FreezeRows(3);

        var startRow = 4;
        for (var i = 0; i < r.Rows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var row = r.Rows[i];
            var rr = startRow + i;
            ws.Cell(rr, 1).Value = i + 1;
            ws.Cell(rr, 2).Value = row.StudentNumber;
            ws.Cell(rr, 3).Value = row.FullName;
            ws.Cell(rr, 4).Value = row.GradeNameAr;
            ws.Cell(rr, 5).Value = row.SectionNameAr;
            ws.Cell(rr, 6).Value = row.FeePlanNameAr ?? string.Empty;
            ws.Cell(rr, 7).Value = (double)row.TotalAmount;
            ws.Cell(rr, 8).Value = (double)row.PaidAmount;
            ws.Cell(rr, 9).Value = (double)row.RemainingAmount;
            ws.Cell(rr, 7).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(rr, 8).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(rr, 9).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(rr, 10).Value = row.OverdueCount;
            ws.Cell(rr, 11).Value = row.StatusAr;
        }
        StyleBody(ws, startRow, r.Rows.Count, colCount);

        if (r.Rows.Count > 0)
        {
            var totalsRow = startRow + r.Rows.Count;
            ws.Cell(totalsRow, 1).Value = "الإجماليات";
            ws.Cell(totalsRow, 7).Value = (double)r.Totals.TotalAmount;
            ws.Cell(totalsRow, 8).Value = (double)r.Totals.TotalPaid;
            ws.Cell(totalsRow, 9).Value = (double)r.Totals.TotalRemaining;
            ws.Cell(totalsRow, 7).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(totalsRow, 8).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(totalsRow, 9).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(totalsRow, 10).Value = r.Totals.TotalOverdue;
            StyleTotalsRow(ws, totalsRow, colCount);
        }

        ws.Columns().AdjustToContents(minWidth: 12, maxWidth: 36);
        SaveWorkbook(wb, filePath);
    }

    // ---------- Shared helpers ----------

    private static void WriteReportTopMatter(IXLWorksheet ws, int colCount, string schoolHeader, string filterSummary)
    {
        ws.Cell(1, 1).Value = schoolHeader;
        ws.Range(1, 1, 1, colCount).Merge();
        var r1 = ws.Range(1, 1, 1, colCount);
        r1.Style.Font.Bold = true;
        r1.Style.Font.FontColor = XLColor.White;
        r1.Style.Fill.BackgroundColor = XLColor.FromHtml("#1FB5A8");
        r1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 22;

        ws.Cell(2, 1).Value = filterSummary;
        ws.Range(2, 1, 2, colCount).Merge();
        var r2 = ws.Range(2, 1, 2, colCount);
        r2.Style.Font.FontColor = XLColor.FromHtml("#1F2937");
        r2.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        r2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(2).Height = 18;
    }

    private static void StyleHeaderRow(IXLWorksheet ws, int rowIndex, int colCount)
    {
        var range = ws.Range(rowIndex, 1, rowIndex, colCount);
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1FB5A8");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CFD6E0");
        range.Style.Border.InsideBorderColor = XLColor.FromHtml("#CFD6E0");
        ws.Row(rowIndex).Height = 22;
    }

    private static void StyleBody(IXLWorksheet ws, int startRow, int rowCount, int colCount)
    {
        if (rowCount <= 0) return;
        var range = ws.Range(startRow, 1, startRow + rowCount - 1, colCount);
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CFD6E0");
        range.Style.Border.InsideBorderColor = XLColor.FromHtml("#E5E9F0");

        // Banded rows — light gray on every other body row.
        for (var i = 1; i < rowCount; i += 2)
        {
            ws.Range(startRow + i, 1, startRow + i, colCount).Style.Fill.BackgroundColor =
                XLColor.FromHtml("#F1F5F9");
        }
    }

    private static void StyleTotalsRow(IXLWorksheet ws, int rowIndex, int colCount)
    {
        var range = ws.Range(rowIndex, 1, rowIndex, colCount);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#CFF1ED"); // soft teal
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1FB5A8");
        ws.Row(rowIndex).Height = 20;
    }

    private static void SaveWorkbook(XLWorkbook wb, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        wb.SaveAs(filePath);
    }

    private static string BuildStudentsSummary(StudentsReportResult r)
        => $"الصف: {(string.IsNullOrWhiteSpace(r.GradeNameAr) ? "الكل" : r.GradeNameAr)}"
           + $" • الشعبة: {(string.IsNullOrWhiteSpace(r.SectionNameAr) ? "الكل" : r.SectionNameAr)}"
           + $" • الحالة: {r.StatusLabelAr}";

    private static string BuildAttendanceSummary(AttendanceReportResult r)
        => $"الصف: {(string.IsNullOrWhiteSpace(r.GradeNameAr) ? "الكل" : r.GradeNameAr)}"
           + $" • الشعبة: {(string.IsNullOrWhiteSpace(r.SectionNameAr) ? "الكل" : r.SectionNameAr)}"
           + $" • الفترة: من {r.DateFrom:yyyy/MM/dd} إلى {r.DateTo:yyyy/MM/dd}";

    private static string BuildFeesSummary(FeesReportResult r)
        => $"الصف: {(string.IsNullOrWhiteSpace(r.GradeNameAr) ? "الكل" : r.GradeNameAr)}"
           + $" • الشعبة: {(string.IsNullOrWhiteSpace(r.SectionNameAr) ? "الكل" : r.SectionNameAr)}"
           + $" • الحالة: {r.StatusLabelAr}";
}
