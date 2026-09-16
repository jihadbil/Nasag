using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Nasag.Services.Printing.Reports;

/// <summary>
/// Shared style helpers for the Phase-11 Reports FlowDocuments
/// (Students/Attendance/Marks/Fees). Centralizes resource lookup,
/// table-cell construction and the Navy/Teal/muted palette so the
/// four reports look identical.
/// </summary>
internal static class ReportDocumentStyle
{
    public static readonly CultureInfo ArSa = TryGetArSa();

    public sealed class Palette
    {
        public FontFamily Tajawal { get; init; } = new("Segoe UI");
        public Brush Teal { get; init; } = Brushes.Teal;
        public Brush Navy { get; init; } = Brushes.Black;
        public Brush Muted { get; init; } = Brushes.Gray;
        public Brush Border { get; init; } = Brushes.LightGray;
        public Brush TealSoft { get; init; } = new SolidColorBrush(Color.FromArgb(0x22, 0x1F, 0xB5, 0xA8));
        public Brush HeaderBg { get; init; } = new SolidColorBrush(Color.FromRgb(0x1F, 0xB5, 0xA8));
        public Brush AltRowBg { get; init; } = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
        public Brush PassSoft { get; init; } = new SolidColorBrush(Color.FromArgb(0x1F, 0x22, 0xC5, 0x5E));
        public Brush FailSoft { get; init; } = new SolidColorBrush(Color.FromArgb(0x1F, 0xEF, 0x44, 0x44));
        public Brush WarnSoft { get; init; } = new SolidColorBrush(Color.FromArgb(0x1F, 0xF5, 0x9E, 0x0B));
    }

    public static Palette LoadPalette()
    {
        var tajawal = (Application.Current?.Resources["TajawalFont"] as FontFamily) ?? new FontFamily("Segoe UI");
        var teal = (Application.Current?.Resources["TealPressedBrush"] as Brush) ?? Brushes.Teal;
        var navy = (Application.Current?.Resources["TextPrimaryBrush"] as Brush) ?? Brushes.Black;
        var muted = (Application.Current?.Resources["TextSecondaryBrush"] as Brush) ?? Brushes.Gray;
        var border = (Application.Current?.Resources["BorderBrush"] as Brush) ?? Brushes.LightGray;

        return new Palette
        {
            Tajawal = tajawal,
            Teal = teal,
            Navy = navy,
            Muted = muted,
            Border = border
        };
    }

    public static FlowDocument CreateA4Document(Palette p, bool landscape = true)
    {
        // A4 at 96 DPI: 793.7 x 1122.5. Landscape (default) swaps.
        const double a4Portrait = 793.7;
        const double a4Landscape = 1122.5;
        var width = landscape ? a4Landscape : a4Portrait;
        var height = landscape ? a4Portrait : a4Landscape;

        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = p.Tajawal,
            FontSize = 10,
            TextAlignment = TextAlignment.Right,
            Foreground = p.Navy,
            PagePadding = new Thickness(32),
            PageWidth = width,
            PageHeight = height,
            ColumnWidth = width,
            ColumnGap = 0,
            IsOptimalParagraphEnabled = true,
            IsHyphenationEnabled = false
        };
        return doc;
    }

    public static void AddSchoolHeader(FlowDocument doc, Palette p, string schoolNameAr, string? address, string? phone)
    {
        var schoolHeader = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        schoolHeader.Inlines.Add(new Run(schoolNameAr)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 18,
            Foreground = p.Navy
        });
        if (!string.IsNullOrWhiteSpace(address))
        {
            schoolHeader.Inlines.Add(new LineBreak());
            schoolHeader.Inlines.Add(new Run(address) { FontSize = 9, Foreground = p.Muted });
        }
        if (!string.IsNullOrWhiteSpace(phone))
        {
            schoolHeader.Inlines.Add(new LineBreak());
            schoolHeader.Inlines.Add(new Run("هاتف: " + phone) { FontSize = 9, Foreground = p.Muted });
        }
        doc.Blocks.Add(schoolHeader);

        // Thin teal divider
        var divider = new BlockUIContainer(new System.Windows.Controls.Border
        {
            Height = 1,
            Background = p.Teal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 2, 0, 6)
        });
        doc.Blocks.Add(divider);
    }

    public static void AddTitleAndSubtitle(FlowDocument doc, Palette p, string title, string? subtitle)
    {
        var t = new Paragraph(new Run(title)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            Foreground = p.Teal
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        };
        doc.Blocks.Add(t);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var s = new Paragraph(new Run(subtitle) { FontSize = 10, Foreground = p.Muted })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(s);
        }
        else
        {
            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 0, 0, 6) });
        }
    }

    public static void AddFooter(FlowDocument doc, Palette p, DateTime generatedAt, string schoolNameAr)
    {
        var footer = new Paragraph(new Run(
            $"تم الإنشاء: {generatedAt:yyyy/MM/dd HH:mm} — {schoolNameAr}")
        {
            FontSize = 9,
            Foreground = p.Muted
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 14, 0, 0)
        };
        doc.Blocks.Add(footer);
    }

    public static void AddEmptyPlaceholder(FlowDocument doc, Palette p)
    {
        var empty = new Paragraph(new Run("لا توجد بيانات لعرضها") { Foreground = p.Muted, FontSize = 13 })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 40, 0, 40)
        };
        doc.Blocks.Add(empty);
    }

    public static Table NewTable(Palette p, params double[] columnWidths)
    {
        var t = new Table
        {
            CellSpacing = 0,
            BorderBrush = p.Border,
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 4, 0, 6)
        };
        foreach (var w in columnWidths)
        {
            if (w <= 0)
                t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            else
                t.Columns.Add(new TableColumn { Width = new GridLength(w) });
        }
        t.RowGroups.Add(new TableRowGroup());
        return t;
    }

    /// <summary>
    /// Creates a table where every column is proportional (star) using the
    /// supplied weights. Pass integer weights like 4, 8, 18 — they'll be
    /// auto-balanced by WPF to fill the available page width.
    /// </summary>
    public static Table NewStarTable(Palette p, params double[] starWeights)
    {
        var t = new Table
        {
            CellSpacing = 0,
            BorderBrush = p.Border,
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 4, 0, 6)
        };
        foreach (var w in starWeights)
        {
            var weight = w <= 0 ? 1 : w;
            t.Columns.Add(new TableColumn { Width = new GridLength(weight, GridUnitType.Star) });
        }
        t.RowGroups.Add(new TableRowGroup());
        return t;
    }

    public static TableRow AddHeaderRow(Table t, Palette p, params string[] headers)
    {
        var row = new TableRow { Background = p.HeaderBg };
        foreach (var h in headers)
            row.Cells.Add(HeaderCell(h, p));
        t.RowGroups[0].Rows.Add(row);
        return row;
    }

    public static TableCell HeaderCell(string text, Palette p)
    {
        var pg = new Paragraph(new Run(text ?? string.Empty)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 11,
            Foreground = Brushes.White
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0)
        };
        return new TableCell(pg)
        {
            Padding = new Thickness(8, 6, 8, 6),
            BorderBrush = p.Border,
            BorderThickness = new Thickness(0.5)
        };
    }

    public static TableCell BodyCell(string text, Palette p, TextAlignment align = TextAlignment.Center, Brush? background = null, bool bold = false)
    {
        var pg = new Paragraph(new Run(text ?? string.Empty) { FontWeight = bold ? FontWeights.Bold : FontWeights.Normal })
        {
            TextAlignment = align,
            Margin = new Thickness(0)
        };
        var cell = new TableCell(pg)
        {
            Padding = new Thickness(6, 4, 6, 4),
            BorderBrush = p.Border,
            BorderThickness = new Thickness(0.5)
        };
        if (background is not null) cell.Background = background;
        return cell;
    }

    public static TableCell MultilineHeaderCell(string topLine, string bottomLine, Palette p)
    {
        var pg = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0)
        };
        pg.Inlines.Add(new Run(topLine) { FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Brushes.White });
        pg.Inlines.Add(new LineBreak());
        pg.Inlines.Add(new Run(bottomLine) { FontWeight = FontWeights.Normal, Foreground = Brushes.White, FontSize = 9 });
        return new TableCell(pg)
        {
            Padding = new Thickness(6, 6, 6, 6),
            BorderBrush = p.Border,
            BorderThickness = new Thickness(0.5)
        };
    }

    public static TableRow AddBodyRow(Table t, Palette p, int rowIndex, params TableCell[] cells)
    {
        var row = new TableRow();
        if (rowIndex % 2 == 1) row.Background = p.AltRowBg;
        foreach (var c in cells) row.Cells.Add(c);
        t.RowGroups[0].Rows.Add(row);
        return row;
    }

    public static TableRow AddTotalsRow(Table t, Palette p, params string[] cells)
    {
        var row = new TableRow { Background = p.TealSoft };
        foreach (var s in cells)
            row.Cells.Add(BodyCell(s, p, TextAlignment.Center, background: null, bold: true));
        t.RowGroups[0].Rows.Add(row);
        return row;
    }

    public static string FormatPercent(double value)
        => value.ToString("0.0", ArSa) + "%";

    private static CultureInfo TryGetArSa()
    {
        try { return CultureInfo.GetCultureInfo("ar-SA"); }
        catch { return CultureInfo.InvariantCulture; }
    }
}
