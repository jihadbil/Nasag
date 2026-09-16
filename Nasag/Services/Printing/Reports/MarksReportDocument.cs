using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Nasag.Repositories;
using static Nasag.Services.Printing.Reports.ReportDocumentStyle;

namespace Nasag.Services.Printing.Reports;

/// <summary>
/// Builds a landscape A4 RTL FlowDocument for the Marks/Results report.
/// Columns: # / رقم / الاسم / [dynamic subjects] / المجموع / النسبة% / التقدير / الحالة.
/// Status cell tinted green/red/amber based on row state.
/// </summary>
public static class MarksReportDocument
{
    public static FlowDocument Build(MarksReportResult r)
    {
        var p = LoadPalette();
        var doc = CreateA4Document(p, landscape: true);

        AddSchoolHeader(doc, p, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone);
        AddTitleAndSubtitle(doc, p, "كشف الدرجات", BuildSubtitle(r));

        if (r.Rows.Count == 0)
        {
            AddEmptyPlaceholder(doc, p);
            AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
            return doc;
        }

        var subjectCount = r.Columns.Count;
        // Star weights: # (3) / رقم (5) / الاسم (12) / [subjects each 8] / المجموع (6) / النسبة (5) / التقدير (7) / الحالة (8)
        var widths = new System.Collections.Generic.List<double> { 3, 5, 12 };
        for (var i = 0; i < subjectCount; i++) widths.Add(8);
        widths.Add(6); // المجموع
        widths.Add(5); // النسبة
        widths.Add(7); // التقدير
        widths.Add(8); // الحالة

        var table = NewStarTable(p, widths.ToArray());

        // Header row — first 3 columns plain, then subject multi-line cells, then 4 trailing.
        var hdr = new TableRow { Background = p.HeaderBg };
        hdr.Cells.Add(HeaderCell("#", p));
        hdr.Cells.Add(HeaderCell("رقم", p));
        hdr.Cells.Add(HeaderCell("الاسم", p));
        foreach (var col in r.Columns)
            hdr.Cells.Add(MultilineHeaderCell(col.SubjectNameAr, "/" + col.MaxMark.ToString(ArSa), p));
        hdr.Cells.Add(HeaderCell("المجموع", p));
        hdr.Cells.Add(HeaderCell("النسبة%", p));
        hdr.Cells.Add(HeaderCell("التقدير", p));
        hdr.Cells.Add(HeaderCell("الحالة", p));
        table.RowGroups[0].Rows.Add(hdr);

        var idx = 1;
        foreach (var row in r.Rows)
        {
            var trow = new TableRow();
            if (idx % 2 == 1) trow.Background = p.AltRowBg;

            trow.Cells.Add(BodyCell(idx.ToString(ArSa), p));
            trow.Cells.Add(BodyCell(row.StudentNumber, p));
            trow.Cells.Add(BodyCell(row.FullName, p, TextAlignment.Right));

            foreach (var col in r.Columns)
            {
                var cell = row.Cells.FirstOrDefault(c => c.SubjectId == col.SubjectId);
                string text;
                if (cell is null || cell.IsAbsent) text = "غ";
                else if (cell.Mark.HasValue) text = cell.Mark.Value.ToString("0.##", ArSa);
                else text = "—";
                trow.Cells.Add(BodyCell(text, p));
            }

            trow.Cells.Add(BodyCell(row.Total.ToString("0.##", ArSa), p));
            trow.Cells.Add(BodyCell(FormatPercent(row.Percentage), p));
            trow.Cells.Add(BodyCell(row.GradeLabelAr ?? string.Empty, p));

            var statusBg = row.IsPending ? p.WarnSoft : (row.IsPassed ? p.PassSoft : p.FailSoft);
            trow.Cells.Add(BodyCell(row.StatusAr, p, TextAlignment.Center, background: statusBg, bold: true));

            table.RowGroups[0].Rows.Add(trow);
            idx++;
        }

        doc.Blocks.Add(table);

        // Summary line
        var totals = r.Totals;
        var summary = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 6, 0, 0)
        };
        summary.Inlines.Add(new Run($"ناجح: {totals.PassedCount.ToString(ArSa)}") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run("  •  "));
        summary.Inlines.Add(new Run($"راسب: {totals.FailedCount.ToString(ArSa)}") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run("  •  "));
        summary.Inlines.Add(new Run($"غير مكتمل: {totals.PendingCount.ToString(ArSa)}") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run("  •  "));
        summary.Inlines.Add(new Run($"المتوسط: {FormatPercent(totals.AveragePercentage)}") { FontWeight = FontWeights.Bold });
        doc.Blocks.Add(summary);

        AddFooter(doc, p, r.GeneratedAt, r.SchoolNameAr);
        return doc;
    }

    private static string BuildSubtitle(MarksReportResult r)
    {
        var sb = new StringBuilder();
        sb.Append("السنة: ").Append(r.AcademicYearAr);
        sb.Append("  •  الصف: ").Append(r.GradeNameAr);
        sb.Append("  •  الشعبة: ").Append(r.SectionNameAr);
        sb.Append("  •  الامتحان: ").Append(r.ExamNameAr);
        return sb.ToString();
    }
}
