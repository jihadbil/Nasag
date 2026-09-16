using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Nasag.Repositories;
using static Nasag.Services.Printing.Reports.ReportDocumentStyle;

namespace Nasag.Services.Printing.Reports;

/// <summary>
/// Builds an A4 RTL FlowDocument for the Students filtered list report.
/// Columns: # / رقم الطالب / الاسم / الجنس / الصف / الشعبة / ولي الأمر / الهاتف / الحالة.
/// </summary>
public static class StudentsReportDocument
{
    public static FlowDocument Build(StudentsReportResult r)
    {
        var p = LoadPalette();
        var doc = CreateA4Document(p, landscape: true);

        AddSchoolHeader(doc, p, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone);
        AddTitleAndSubtitle(doc, p, "تقرير الطلاب", BuildSubtitle(r));

        if (r.Rows.Count == 0)
        {
            AddEmptyPlaceholder(doc, p);
            AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
            return doc;
        }

        // Star weights: # / رقم / الاسم / الجنس / الصف / الشعبة / ولي الأمر / الهاتف / الحالة
        var table = NewStarTable(p, 4, 8, 18, 7, 14, 7, 18, 12, 12);
        AddHeaderRow(table, p,
            "#", "رقم الطالب", "الاسم", "الجنس", "الصف", "الشعبة", "ولي الأمر", "الهاتف", "الحالة");

        var idx = 1;
        foreach (var row in r.Rows)
        {
            AddBodyRow(table, p, idx,
                BodyCell(idx.ToString(ArSa), p),
                BodyCell(row.StudentNumber, p),
                BodyCell(row.FullName, p, System.Windows.TextAlignment.Right),
                BodyCell(row.GenderAr, p),
                BodyCell(row.GradeNameAr, p),
                BodyCell(row.SectionNameAr, p),
                BodyCell(row.GuardianNameAr ?? "—", p, System.Windows.TextAlignment.Right),
                BodyCell(row.GuardianPhone ?? "—", p),
                BodyCell(row.StatusAr, p));
            idx++;
        }
        doc.Blocks.Add(table);

        var summary = new Paragraph(new Run($"إجمالي: {r.Rows.Count.ToString(ArSa)} طالب")
        {
            FontWeight = FontWeights.Bold,
            Foreground = p.Navy
        })
        {
            TextAlignment = System.Windows.TextAlignment.Right,
            Margin = new Thickness(0, 4, 0, 0)
        };
        doc.Blocks.Add(summary);

        AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
        return doc;
    }

    private static string BuildSubtitle(StudentsReportResult r)
    {
        var sb = new StringBuilder();
        sb.Append("السنة: ").Append(r.AcademicYearAr);
        sb.Append("  •  الصف: ").Append(string.IsNullOrWhiteSpace(r.GradeNameAr) ? "الكل" : r.GradeNameAr);
        sb.Append("  •  الشعبة: ").Append(string.IsNullOrWhiteSpace(r.SectionNameAr) ? "الكل" : r.SectionNameAr);
        sb.Append("  •  الحالة: ").Append(r.StatusLabelAr);
        return sb.ToString();
    }
}
