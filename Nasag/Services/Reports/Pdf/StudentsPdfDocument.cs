using System;
using System.Globalization;
using Nasag.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nasag.Services.Reports.Pdf;

internal static class StudentsPdfDocument
{
    public static void Generate(string filePath, StudentsReportResult r)
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

    private static void Header(IContainer container, StudentsReportResult r)
    {
        var subtitle = BuildSubtitle(r);
        PdfTheme.ReportHeader(container, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone,
            "تقرير الطلاب", subtitle, r.AcademicYearAr);
    }

    private static string BuildSubtitle(StudentsReportResult r)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(r.GradeNameAr)) parts.Add($"الصف: {r.GradeNameAr}");
        if (!string.IsNullOrWhiteSpace(r.SectionNameAr)) parts.Add($"الشعبة: {r.SectionNameAr}");
        if (!string.IsNullOrWhiteSpace(r.StatusLabelAr)) parts.Add($"الحالة: {r.StatusLabelAr}");
        return string.Join("  •  ", parts);
    }

    private static void Body(IContainer container, StudentsReportResult r)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(5);     // #
                    c.RelativeColumn(10);    // رقم الطالب
                    c.RelativeColumn(20);    // الاسم
                    c.RelativeColumn(7);     // الجنس
                    c.RelativeColumn(15);    // الصف
                    c.RelativeColumn(7);     // الشعبة
                    c.RelativeColumn(20);    // ولي الأمر
                    c.RelativeColumn(10);    // الهاتف
                    c.RelativeColumn(6);     // الحالة
                });

                table.Header(h =>
                {
                    PdfTheme.HeaderCell(h.Cell(), "#");
                    PdfTheme.HeaderCell(h.Cell(), "رقم الطالب");
                    PdfTheme.HeaderCell(h.Cell(), "الاسم");
                    PdfTheme.HeaderCell(h.Cell(), "الجنس");
                    PdfTheme.HeaderCell(h.Cell(), "الصف");
                    PdfTheme.HeaderCell(h.Cell(), "الشعبة");
                    PdfTheme.HeaderCell(h.Cell(), "ولي الأمر");
                    PdfTheme.HeaderCell(h.Cell(), "الهاتف");
                    PdfTheme.HeaderCell(h.Cell(), "الحالة");
                });

                int i = 0;
                foreach (var row in r.Rows)
                {
                    var bg = (i % 2 == 0) ? PdfTheme.RowAlt : PdfTheme.White;
                    PdfTheme.BodyCell(table.Cell(), (i + 1).ToString(CultureInfo.GetCultureInfo("ar-SA")), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.StudentNumber ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.FullName ?? "—", bg);
                    PdfTheme.BodyCell(table.Cell(), row.GenderAr ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.GradeNameAr ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.SectionNameAr ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.GuardianNameAr ?? "—", bg);
                    PdfTheme.BodyCell(table.Cell(), row.GuardianPhone ?? "—", bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.StatusAr ?? "—", bg, center: true);
                    i++;
                }
            });

            col.Item().PaddingTop(6).Element(c => PdfTheme.TotalsStrip(c, $"إجمالي: {r.Rows.Count} طالب"));
        });
    }
}
