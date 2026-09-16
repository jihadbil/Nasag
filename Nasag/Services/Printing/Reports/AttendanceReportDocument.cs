using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Nasag.Repositories;
using static Nasag.Services.Printing.Reports.ReportDocumentStyle;

namespace Nasag.Services.Printing.Reports;

/// <summary>
/// Builds an A4 RTL FlowDocument for the Attendance period report.
/// Columns: # / رقم / الاسم / الصف / الشعبة / حاضر / غائب / متأخر / إجازة / أيام / النسبة%.
/// Last row = totals (teal soft bg).
/// </summary>
public static class AttendanceReportDocument
{
    public static FlowDocument Build(AttendanceReportResult r)
    {
        var p = LoadPalette();
        var doc = CreateA4Document(p, landscape: true);

        AddSchoolHeader(doc, p, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone);
        AddTitleAndSubtitle(doc, p, "كشف الحضور", BuildSubtitle(r));

        if (r.Rows.Count == 0)
        {
            AddEmptyPlaceholder(doc, p);
            AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
            return doc;
        }

        // 11 columns — # / رقم / الاسم / الصف / الشعبة / حاضر / غائب / متأخر / إجازة / أيام / النسبة%
        var table = NewStarTable(p, 4, 7, 14, 11, 8, 6, 6, 6, 6, 6, 26);
        AddHeaderRow(table, p,
            "#", "رقم", "الاسم", "الصف", "الشعبة", "حاضر", "غائب", "متأخر", "إجازة", "أيام", "النسبة%");

        var idx = 1;
        foreach (var row in r.Rows)
        {
            AddBodyRow(table, p, idx,
                BodyCell(idx.ToString(ArSa), p),
                BodyCell(row.StudentNumber, p),
                BodyCell(row.FullName, p, TextAlignment.Right),
                BodyCell(row.GradeNameAr, p),
                BodyCell(row.SectionNameAr, p),
                BodyCell(row.Present.ToString(ArSa), p),
                BodyCell(row.Absent.ToString(ArSa), p),
                BodyCell(row.Late.ToString(ArSa), p),
                BodyCell(row.Excused.ToString(ArSa), p),
                BodyCell(row.RecordedDays.ToString(ArSa), p),
                BodyCell(FormatPercent(row.AttendancePercentage), p));
            idx++;
        }

        // Totals row
        var totals = r.Totals;
        AddTotalsRow(table, p,
            "الإجماليات",
            "",
            "",
            "",
            "",
            totals.TotalPresent.ToString(ArSa),
            totals.TotalAbsent.ToString(ArSa),
            totals.TotalLate.ToString(ArSa),
            totals.TotalExcused.ToString(ArSa),
            totals.RecordedDays.ToString(ArSa),
            FormatPercent(totals.AveragePercentage));

        doc.Blocks.Add(table);
        AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
        return doc;
    }

    private static string BuildSubtitle(AttendanceReportResult r)
    {
        var sb = new StringBuilder();
        sb.Append("السنة: ").Append(r.AcademicYearAr);
        sb.Append("  •  الصف: ").Append(string.IsNullOrWhiteSpace(r.GradeNameAr) ? "الكل" : r.GradeNameAr);
        sb.Append("  •  الشعبة: ").Append(string.IsNullOrWhiteSpace(r.SectionNameAr) ? "الكل" : r.SectionNameAr);
        sb.Append("  •  الفترة: من ")
          .Append(r.DateFrom.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture))
          .Append(" إلى ")
          .Append(r.DateTo.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}
