using System;
using System.Globalization;
using Nasag.Helpers;
using Nasag.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nasag.Services.Reports.Pdf;

internal static class FeesPdfDocument
{
    private static readonly CultureInfo ArSa = CultureInfo.GetCultureInfo("ar-SA");

    public static void Generate(string filePath, FeesReportResult r)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20, Unit.Millimetre);
                page.DefaultTextStyle(t => t
                    .FontFamily("Tajawal")
                    .FontSize(9)
                    .FontColor(PdfTheme.Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(c => Header(c, r));
                page.Content().Element(c => Body(c, r));
                page.Footer().Element(c => PdfTheme.Footer(c, r.GeneratedAt));
            });
        }).GeneratePdf(filePath);
    }

    private static void Header(IContainer container, FeesReportResult r)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(r.GradeNameAr)) parts.Add($"الصف: {r.GradeNameAr}");
        if (!string.IsNullOrWhiteSpace(r.SectionNameAr)) parts.Add($"الشعبة: {r.SectionNameAr}");
        if (!string.IsNullOrWhiteSpace(r.StatusLabelAr)) parts.Add($"الحالة: {r.StatusLabelAr}");

        PdfTheme.ReportHeader(container, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone,
            "كشف الرسوم", string.Join("  •  ", parts), r.AcademicYearAr);
    }

    private static void Body(IContainer container, FeesReportResult r)
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
                    c.RelativeColumn(10);    // الصف
                    c.RelativeColumn(6);     // الشعبة
                    c.RelativeColumn(15);    // الخطة
                    c.RelativeColumn(9);     // الإجمالي
                    c.RelativeColumn(9);     // المدفوع
                    c.RelativeColumn(9);     // المتبقي
                    c.RelativeColumn(5);     // متأخرات
                    c.RelativeColumn(12);    // الحالة
                });

                table.Header(h =>
                {
                    PdfTheme.HeaderCell(h.Cell(), "#");
                    PdfTheme.HeaderCell(h.Cell(), "رقم");
                    PdfTheme.HeaderCell(h.Cell(), "الاسم");
                    PdfTheme.HeaderCell(h.Cell(), "الصف");
                    PdfTheme.HeaderCell(h.Cell(), "الشعبة");
                    PdfTheme.HeaderCell(h.Cell(), "الخطة");
                    PdfTheme.HeaderCell(h.Cell(), "الإجمالي");
                    PdfTheme.HeaderCell(h.Cell(), "المدفوع");
                    PdfTheme.HeaderCell(h.Cell(), "المتبقي");
                    PdfTheme.HeaderCell(h.Cell(), "متأخرات");
                    PdfTheme.HeaderCell(h.Cell(), "الحالة");
                });

                int i = 0;
                foreach (var row in r.Rows)
                {
                    var bg = (i % 2 == 0) ? PdfTheme.RowAlt : PdfTheme.White;
                    PdfTheme.BodyCell(table.Cell(), (i + 1).ToString(ArSa), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.StudentNumber ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.FullName ?? "—", bg);
                    PdfTheme.BodyCell(table.Cell(), row.GradeNameAr ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.SectionNameAr ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.FeePlanNameAr ?? "—", bg);
                    PdfTheme.BodyCell(table.Cell(), MoneyFormatter.Format(row.TotalAmount), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), MoneyFormatter.Format(row.PaidAmount), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), MoneyFormatter.Format(row.RemainingAmount), bg, center: true);

                    var overdueColor = row.OverdueCount > 0 ? PdfTheme.DangerText : PdfTheme.Navy;
                    PdfTheme.BodyCell(table.Cell(), row.OverdueCount.ToString(ArSa), bg, center: true, textColor: overdueColor);

                    var (sBg, sFg) = StatusColor(row);
                    PdfTheme.StatusPillCell(table.Cell(), row.StatusAr ?? "—", sBg, sFg, bg);
                    i++;
                }
            });

            var t = r.Totals;
            var totalsText =
                $"عدد الطلاب: {t.StudentCount.ToString(ArSa)}  •  الإجمالي: {MoneyFormatter.Format(t.TotalAmount)}  •  " +
                $"المدفوع: {MoneyFormatter.Format(t.TotalPaid)}  •  المتبقي: {MoneyFormatter.Format(t.TotalRemaining)}  •  " +
                $"متأخرات: {t.TotalOverdue.ToString(ArSa)}";
            col.Item().PaddingTop(6).Element(c => PdfTheme.TotalsStrip(c, totalsText));
        });
    }

    private static (string bg, string fg) StatusColor(FeesReportRow row)
    {
        if (row.OverdueCount > 0)
            return (PdfTheme.DangerSoft, PdfTheme.DangerText);
        if (row.RemainingAmount <= 0m && row.TotalAmount > 0m)
            return (PdfTheme.SuccessSoft, PdfTheme.SuccessText);
        if (row.PaidAmount > 0m && row.RemainingAmount > 0m)
            return (PdfTheme.WarningSoft, PdfTheme.WarningText);
        return (PdfTheme.MutedSoft, PdfTheme.Muted);
    }
}
