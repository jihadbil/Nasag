using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Nasag.Helpers;
using Nasag.Repositories;
using static Nasag.Services.Printing.Reports.ReportDocumentStyle;

namespace Nasag.Services.Printing.Reports;

/// <summary>
/// Builds an A4 RTL FlowDocument for the Fees summary report.
/// Columns: # / رقم / الاسم / الصف / الشعبة / الخطة / الإجمالي / المدفوع / المتبقي / متأخرات / الحالة.
/// Totals row tinted teal-soft.
/// </summary>
public static class FeesReportDocument
{
    public static FlowDocument Build(FeesReportResult r)
    {
        var p = LoadPalette();
        var doc = CreateA4Document(p, landscape: true);

        AddSchoolHeader(doc, p, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone);
        AddTitleAndSubtitle(doc, p, "كشف الرسوم", BuildSubtitle(r));

        if (r.Rows.Count == 0)
        {
            AddEmptyPlaceholder(doc, p);
            AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
            return doc;
        }

        // # / رقم / الاسم / الصف / الشعبة / الخطة / الإجمالي / المدفوع / المتبقي / متأخرات / الحالة
        var table = NewStarTable(p, 4, 7, 14, 10, 6, 16, 10, 10, 10, 5, 8);
        AddHeaderRow(table, p,
            "#", "رقم", "الاسم", "الصف", "الشعبة", "الخطة",
            "الإجمالي", "المدفوع", "المتبقي", "متأخرات", "الحالة");

        var idx = 1;
        foreach (var row in r.Rows)
        {
            AddBodyRow(table, p, idx,
                BodyCell(idx.ToString(ArSa), p),
                BodyCell(row.StudentNumber, p),
                BodyCell(row.FullName, p, TextAlignment.Right),
                BodyCell(row.GradeNameAr, p),
                BodyCell(row.SectionNameAr, p),
                BodyCell(row.FeePlanNameAr ?? "—", p, TextAlignment.Right),
                BodyCell(MoneyFormatter.Format(row.TotalAmount), p),
                BodyCell(MoneyFormatter.Format(row.PaidAmount), p),
                BodyCell(MoneyFormatter.Format(row.RemainingAmount), p),
                BodyCell(row.OverdueCount.ToString(ArSa), p),
                BodyCell(row.StatusAr, p));
            idx++;
        }

        var totals = r.Totals;
        AddTotalsRow(table, p,
            "الإجماليات",
            "",
            "",
            "",
            "",
            "",
            MoneyFormatter.Format(totals.TotalAmount),
            MoneyFormatter.Format(totals.TotalPaid),
            MoneyFormatter.Format(totals.TotalRemaining),
            totals.TotalOverdue.ToString(ArSa),
            "");

        doc.Blocks.Add(table);

        var summary = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 6, 0, 0)
        };
        summary.Inlines.Add(new Run($"الإجماليات: {totals.StudentCount.ToString(ArSa)} طالب") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run("  •  "));
        summary.Inlines.Add(new Run($"الإجمالي {MoneyFormatter.Format(totals.TotalAmount)}") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run("  •  "));
        summary.Inlines.Add(new Run($"المدفوع {MoneyFormatter.Format(totals.TotalPaid)}") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run("  •  "));
        summary.Inlines.Add(new Run($"المتبقي {MoneyFormatter.Format(totals.TotalRemaining)}") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run("  •  "));
        summary.Inlines.Add(new Run($"متأخرات {totals.TotalOverdue.ToString(ArSa)}") { FontWeight = FontWeights.Bold });
        doc.Blocks.Add(summary);

        AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
        return doc;
    }

    private static string BuildSubtitle(FeesReportResult r)
    {
        var sb = new StringBuilder();
        sb.Append("السنة: ").Append(r.AcademicYearAr);
        sb.Append("  •  الصف: ").Append(string.IsNullOrWhiteSpace(r.GradeNameAr) ? "الكل" : r.GradeNameAr);
        sb.Append("  •  الشعبة: ").Append(string.IsNullOrWhiteSpace(r.SectionNameAr) ? "الكل" : r.SectionNameAr);
        sb.Append("  •  الحالة: ").Append(r.StatusLabelAr);
        return sb.ToString();
    }
}
