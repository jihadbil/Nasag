using System;
using System.Globalization;
using System.Linq;
using Nasag.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nasag.Services.Reports.Pdf;

internal static class MarksPdfDocument
{
    private static readonly CultureInfo ArSa = CultureInfo.GetCultureInfo("ar-SA");

    public static void Generate(string filePath, MarksReportResult r)
    {
        // Scale font down when many subjects are present.
        var subjectCount = r.Columns.Count;
        var bodySize = subjectCount > 8 ? 8f : 9f;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20, Unit.Millimetre);
                page.DefaultTextStyle(t => t
                    .FontFamily("Tajawal")
                    .FontSize(bodySize)
                    .FontColor(PdfTheme.Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(c => Header(c, r));
                page.Content().Element(c => Body(c, r, bodySize));
                page.Footer().Element(c => PdfTheme.Footer(c, r.GeneratedAt));
            });
        }).GeneratePdf(filePath);
    }

    private static void Header(IContainer container, MarksReportResult r)
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"الصف: {r.GradeNameAr}",
            $"الشعبة: {r.SectionNameAr}",
            $"الامتحان: {r.ExamNameAr}"
        };
        PdfTheme.ReportHeader(container, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone,
            "كشف الدرجات", string.Join("  •  ", parts), r.AcademicYearAr);
    }

    private static void Body(IContainer container, MarksReportResult r, float bodySize)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4);     // #
                    c.RelativeColumn(7);     // رقم
                    c.RelativeColumn(14);    // الاسم

                    for (int i = 0; i < r.Columns.Count; i++)
                        c.RelativeColumn(6);  // subject (fluid)

                    c.RelativeColumn(7);     // المجموع
                    c.RelativeColumn(6);     // النسبة
                    c.RelativeColumn(8);     // التقدير
                    c.RelativeColumn(8);     // الحالة
                });

                table.Header(h =>
                {
                    PdfTheme.HeaderCell(h.Cell(), "#");
                    PdfTheme.HeaderCell(h.Cell(), "رقم");
                    PdfTheme.HeaderCell(h.Cell(), "الاسم");

                    foreach (var s in r.Columns)
                        PdfTheme.HeaderCell(h.Cell(), $"{s.SubjectNameAr}\n/{s.MaxMark.ToString(ArSa)}");

                    PdfTheme.HeaderCell(h.Cell(), "المجموع");
                    PdfTheme.HeaderCell(h.Cell(), "النسبة%");
                    PdfTheme.HeaderCell(h.Cell(), "التقدير");
                    PdfTheme.HeaderCell(h.Cell(), "الحالة");
                });

                int i = 0;
                foreach (var row in r.Rows)
                {
                    var bg = (i % 2 == 0) ? PdfTheme.RowAlt : PdfTheme.White;
                    PdfTheme.BodyCell(table.Cell(), (i + 1).ToString(ArSa), bg, center: true, fontSize: bodySize);
                    PdfTheme.BodyCell(table.Cell(), row.StudentNumber ?? "—", bg, center: true, fontSize: bodySize);
                    PdfTheme.BodyCell(table.Cell(), row.FullName ?? "—", bg, fontSize: bodySize);

                    // Build a lookup so cells line up with columns even if the row's cell order differs.
                    var cellsBySubject = row.Cells?.ToDictionary(x => x.SubjectId) ?? new System.Collections.Generic.Dictionary<int, MarksReportCell>();
                    foreach (var subj in r.Columns)
                    {
                        cellsBySubject.TryGetValue(subj.SubjectId, out var cell);
                        var text = FormatMarkCell(cell);
                        var color = cell switch
                        {
                            null => PdfTheme.Muted,
                            { IsAbsent: true } => PdfTheme.DangerText,
                            { Mark: null } => PdfTheme.Muted,
                            { IsPass: false } => PdfTheme.DangerText,
                            _ => PdfTheme.Navy
                        };
                        PdfTheme.BodyCell(table.Cell(), text, bg, center: true, textColor: color, fontSize: bodySize);
                    }

                    PdfTheme.BodyCell(table.Cell(),
                        $"{row.Total.ToString("0.##", ArSa)}/{row.MaxTotal.ToString("0.##", ArSa)}",
                        bg, center: true, fontSize: bodySize);
                    PdfTheme.BodyCell(table.Cell(), FormatPct(row.Percentage), bg, center: true, fontSize: bodySize);
                    PdfTheme.BodyCell(table.Cell(), row.GradeLabelAr ?? "—", bg, center: true, fontSize: bodySize);

                    string statusBg;
                    string statusFg;
                    if (row.IsPending) { statusBg = PdfTheme.WarningSoft; statusFg = PdfTheme.WarningText; }
                    else if (row.IsPassed) { statusBg = PdfTheme.SuccessSoft; statusFg = PdfTheme.SuccessText; }
                    else { statusBg = PdfTheme.DangerSoft; statusFg = PdfTheme.DangerText; }

                    PdfTheme.StatusPillCell(table.Cell(), row.StatusAr ?? "—", statusBg, statusFg, bg);
                    i++;
                }
            });

            var t = r.Totals;
            var totalsText =
                $"ناجح: {t.PassedCount.ToString(ArSa)}  •  راسب: {t.FailedCount.ToString(ArSa)}  •  " +
                $"غير مكتمل: {t.PendingCount.ToString(ArSa)}  •  متوسط: {FormatPct(t.AveragePercentage)}  •  " +
                $"أعلى: {FormatPct(t.HighestPercentage)}  •  أدنى: {FormatPct(t.LowestPercentage)}";
            col.Item().PaddingTop(6).Element(c => PdfTheme.TotalsStrip(c, totalsText));
        });
    }

    private static string FormatMarkCell(MarksReportCell? cell)
    {
        if (cell is null) return "—";
        if (cell.IsAbsent) return "غ";
        if (cell.Mark is null) return "—";
        return cell.Mark.Value.ToString("0.##", ArSa);
    }

    private static string FormatPct(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) v = 0;
        return v.ToString("0.0", ArSa) + "%";
    }
}
