using System;
using System.Globalization;
using Nasag.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nasag.Services.Reports.Pdf;

internal static class AttendancePdfDocument
{
    private static readonly CultureInfo ArSa = CultureInfo.GetCultureInfo("ar-SA");

    public static void Generate(string filePath, AttendanceReportResult r)
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

    private static void Header(IContainer container, AttendanceReportResult r)
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"من {r.DateFrom:yyyy/MM/dd} إلى {r.DateTo:yyyy/MM/dd}"
        };
        if (!string.IsNullOrWhiteSpace(r.GradeNameAr)) parts.Add($"الصف: {r.GradeNameAr}");
        if (!string.IsNullOrWhiteSpace(r.SectionNameAr)) parts.Add($"الشعبة: {r.SectionNameAr}");

        PdfTheme.ReportHeader(container, r.SchoolNameAr, r.SchoolAddress, r.SchoolPhone,
            "كشف الحضور", string.Join("  •  ", parts), r.AcademicYearAr);
    }

    private static void Body(IContainer container, AttendanceReportResult r)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4);     // #
                    c.RelativeColumn(8);     // رقم
                    c.RelativeColumn(16);    // الاسم
                    c.RelativeColumn(12);    // الصف
                    c.RelativeColumn(8);     // الشعبة
                    c.RelativeColumn(5);     // حاضر
                    c.RelativeColumn(5);     // غائب
                    c.RelativeColumn(5);     // متأخر
                    c.RelativeColumn(5);     // إجازة
                    c.RelativeColumn(5);     // أيام
                    c.RelativeColumn(8);     // النسبة
                });

                table.Header(h =>
                {
                    PdfTheme.HeaderCell(h.Cell(), "#");
                    PdfTheme.HeaderCell(h.Cell(), "رقم");
                    PdfTheme.HeaderCell(h.Cell(), "الاسم");
                    PdfTheme.HeaderCell(h.Cell(), "الصف");
                    PdfTheme.HeaderCell(h.Cell(), "الشعبة");
                    PdfTheme.HeaderCell(h.Cell(), "حاضر");
                    PdfTheme.HeaderCell(h.Cell(), "غائب");
                    PdfTheme.HeaderCell(h.Cell(), "متأخر");
                    PdfTheme.HeaderCell(h.Cell(), "إجازة");
                    PdfTheme.HeaderCell(h.Cell(), "أيام");
                    PdfTheme.HeaderCell(h.Cell(), "النسبة%");
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
                    PdfTheme.BodyCell(table.Cell(), row.Present.ToString(ArSa), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.Absent.ToString(ArSa), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.Late.ToString(ArSa), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.Excused.ToString(ArSa), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), row.RecordedDays.ToString(ArSa), bg, center: true);
                    PdfTheme.BodyCell(table.Cell(), FormatPct(row.AttendancePercentage), bg, center: true);
                    i++;
                }
            });

            // Totals strip
            var t = r.Totals;
            var totalsText =
                $"عدد الطلاب: {t.StudentCount.ToString(ArSa)}  •  أيام مسجَّلة: {t.RecordedDays.ToString(ArSa)}  •  " +
                $"حاضر: {t.TotalPresent.ToString(ArSa)}  •  غائب: {t.TotalAbsent.ToString(ArSa)}  •  " +
                $"متأخر: {t.TotalLate.ToString(ArSa)}  •  إجازة: {t.TotalExcused.ToString(ArSa)}  •  " +
                $"متوسط الحضور: {FormatPct(t.AveragePercentage)}";
            col.Item().PaddingTop(6).Element(c => PdfTheme.TotalsStrip(c, totalsText));
        });
    }

    private static string FormatPct(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) v = 0;
        return v.ToString("0.0", ArSa) + "%";
    }
}
