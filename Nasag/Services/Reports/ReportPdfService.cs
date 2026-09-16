using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Nasag.Repositories;
using Nasag.Services.Reports.Pdf;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Nasag.Services.Reports
{
    /// <summary>
    /// QuestPDF-backed implementation of <see cref="IReportPdfService"/>. Produces
    /// RTL Arabic PDFs in Tajawal, matching the on-screen FlowDocument preview.
    /// </summary>
    public sealed class ReportPdfService : IReportPdfService
    {
        private static int _initialized;

        public ReportPdfService()
        {
            EnsureInitialized();
        }

        internal static void EnsureInitialized()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0) return;

            QuestPDF.Settings.License = LicenseType.Community;

            TryRegisterFont("pack://application:,,,/Assets/Fonts/Tajawal-Regular.ttf");
            TryRegisterFont("pack://application:,,,/Assets/Fonts/Tajawal-Medium.ttf");
            TryRegisterFont("pack://application:,,,/Assets/Fonts/Tajawal-Bold.ttf");
        }

        private static void TryRegisterFont(string packUri)
        {
            try
            {
                // Application may be null in design-time / unit-test scenarios.
                if (Application.ResourceAssembly is null && Application.Current is null) return;
                var info = Application.GetResourceStream(new Uri(packUri, UriKind.Absolute));
                if (info?.Stream is null) return;
                using var ms = new MemoryStream();
                info.Stream.CopyTo(ms);
                ms.Position = 0;
                FontManager.RegisterFont(ms);
            }
            catch
            {
                // Best-effort: if font registration fails, QuestPDF falls back to a system font.
            }
        }

        public Task SaveStudentsAsync(string filePath, StudentsReportResult result, CancellationToken ct = default)
        {
            EnsureInitialized();
            return Task.Run(() => StudentsPdfDocument.Generate(filePath, result), ct);
        }

        public Task SaveAttendanceAsync(string filePath, AttendanceReportResult result, CancellationToken ct = default)
        {
            EnsureInitialized();
            return Task.Run(() => AttendancePdfDocument.Generate(filePath, result), ct);
        }

        public Task SaveMarksAsync(string filePath, MarksReportResult result, CancellationToken ct = default)
        {
            EnsureInitialized();
            return Task.Run(() => MarksPdfDocument.Generate(filePath, result), ct);
        }

        public Task SaveFeesAsync(string filePath, FeesReportResult result, CancellationToken ct = default)
        {
            EnsureInitialized();
            return Task.Run(() => FeesPdfDocument.Generate(filePath, result), ct);
        }
    }
}

namespace Nasag.Services.Reports.Pdf
{
    /// <summary>
    /// Shared visual primitives for the four Phase 11 PDF documents:
    /// palette, header, footer, table cells, totals strip.
    /// </summary>
    internal static class PdfTheme
    {
        // Palette — matches the in-app theme.
        public const string Teal = "#1FB5A8";
        public const string TealSoft = "#E6F7F5";
        public const string Navy = "#1B3A57";
        public const string Muted = "#6B7A8F";
        public const string MutedSoft = "#EEF1F5";
        public const string White = "#FFFFFF";
        public const string RowAlt = "#F8FAFC";
        public const string Border = "#E5E9F0";

        // Status soft fills (~12% alpha approximations against white).
        public const string SuccessSoft = "#DCFCE7";
        public const string SuccessText = "#15803D";
        public const string WarningSoft = "#FEF3C7";
        public const string WarningText = "#B45309";
        public const string DangerSoft = "#FEE2E2";
        public const string DangerText = "#B91C1C";

        public static void ReportHeader(IContainer container, string schoolName, string? address,
            string? phone, string title, string subtitle, string academicYear)
        {
            container.PaddingBottom(8).Column(col =>
            {
                col.Spacing(2);

                col.Item().Text(schoolName ?? "—")
                    .FontFamily("Tajawal").FontSize(16).Bold().FontColor(Navy);

                var meta = string.Join("  •  ",
                    new[] { address, phone, academicYear }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(meta))
                    col.Item().Text(meta).FontFamily("Tajawal").FontSize(10).FontColor(Muted);

                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Teal);

                col.Item().PaddingTop(6).Text(title)
                    .FontFamily("Tajawal").FontSize(14).SemiBold().FontColor(Teal);

                if (!string.IsNullOrWhiteSpace(subtitle))
                    col.Item().Text(subtitle).FontFamily("Tajawal").FontSize(10).FontColor(Muted);
            });
        }

        public static void Footer(IContainer container, DateTime generatedAt)
        {
            container.PaddingTop(6).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text(t =>
                {
                    t.Span($"تم الإنشاء: {generatedAt:yyyy/MM/dd HH:mm}")
                        .FontFamily("Tajawal").FontSize(9).FontColor(Muted);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("صفحة ").FontFamily("Tajawal").FontSize(9).FontColor(Muted);
                    t.CurrentPageNumber().FontFamily("Tajawal").FontSize(9).FontColor(Muted);
                    t.Span(" من ").FontFamily("Tajawal").FontSize(9).FontColor(Muted);
                    t.TotalPages().FontFamily("Tajawal").FontSize(9).FontColor(Muted);
                });
            });
        }

        public static void HeaderCell(IContainer cell, string text)
        {
            // QuestPDF 2024.3+ handles word-vs-character wrap automatically; we keep
            // headers compact with LineHeight 1.1 so 2-line Arabic titles read cleanly.
            cell.Border(0.5f).BorderColor(Border)
                .Background(Teal)
                .PaddingVertical(5).PaddingHorizontal(4)
                .AlignCenter().AlignMiddle()
                .Text(text ?? "—")
                .FontFamily("Tajawal").Bold().FontColor(White).FontSize(10)
                .LineHeight(1.1f);
        }

        public static void BodyCell(IContainer cell, string text, string bg, bool center = false, string? textColor = null, float fontSize = 9f)
        {
            // Body cells default to 9pt so longer Arabic words (e.g. "الابتدائي")
            // fit on one line in the narrower columns of an 11-column landscape table.
            var c = cell.Border(0.5f).BorderColor(Border)
                .Background(bg)
                .PaddingVertical(3).PaddingHorizontal(3)
                .AlignMiddle();
            c = center ? c.AlignCenter() : c.AlignRight();
            c.Text(text ?? "—")
                .FontFamily("Tajawal")
                .FontColor(textColor ?? Navy)
                .FontSize(fontSize)
                .LineHeight(1.15f);
        }

        public static void StatusPillCell(IContainer cell, string text, string pillBg, string pillFg, string rowBg)
        {
            cell.Border(0.5f).BorderColor(Border)
                .Background(rowBg)
                .Padding(3)
                .AlignCenter().AlignMiddle()
                .Background(pillBg)
                .PaddingVertical(3).PaddingHorizontal(6)
                .Text(text ?? "—")
                .FontFamily("Tajawal").SemiBold().FontColor(pillFg).FontSize(9);
        }

        public static void TotalsStrip(IContainer container, string text)
        {
            container.Background(TealSoft).Padding(8)
                .Text(text)
                .FontFamily("Tajawal").SemiBold().FontColor(Navy).FontSize(10);
        }
    }
}
