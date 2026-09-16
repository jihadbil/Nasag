using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Nasag.Helpers;

namespace Nasag.Services.Printing;

public sealed record ReceiptModel(
    string SchoolNameAr,
    string? SchoolAddress,
    string? SchoolPhone,
    string ReceiptNumber,
    DateTime PaymentDate,
    string StudentFullName,
    string StudentNumber,
    string GradeName,
    string SectionName,
    decimal Amount,
    string AmountInWordsAr,
    Nasag.Models.PaymentMethod Method,
    string MethodLabelAr,
    int? InstallmentNumber,
    string? Notes,
    string CashierName);

/// <summary>
/// Builds a single-page A4 RTL FlowDocument representing a payment receipt
/// ("سند قبض"). The document is suitable for both preview (DocumentViewer)
/// and direct printing via <see cref="PrintService"/>.
/// </summary>
public static class ReceiptDocument
{
    public static FlowDocument Build(ReceiptModel m)
    {
        var tajawal = Application.Current?.Resources["TajawalFont"] as FontFamily ?? new FontFamily("Segoe UI");
        var teal = (Application.Current?.Resources["TealPressedBrush"] as Brush) ?? Brushes.Teal;
        var navy = (Application.Current?.Resources["TextPrimaryBrush"] as Brush) ?? Brushes.Black;
        var muted = (Application.Current?.Resources["TextSecondaryBrush"] as Brush) ?? Brushes.Gray;
        var border = (Application.Current?.Resources["BorderBrush"] as Brush) ?? Brushes.LightGray;

        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = tajawal,
            FontSize = 13,
            TextAlignment = TextAlignment.Right,
            Foreground = navy
        };

        // 1) Header — school name + address + phone
        var schoolHeader = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        schoolHeader.Inlines.Add(new Run(m.SchoolNameAr)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 22,
            Foreground = navy
        });
        if (!string.IsNullOrWhiteSpace(m.SchoolAddress))
        {
            schoolHeader.Inlines.Add(new LineBreak());
            schoolHeader.Inlines.Add(new Run(m.SchoolAddress) { FontSize = 10, Foreground = muted });
        }
        if (!string.IsNullOrWhiteSpace(m.SchoolPhone))
        {
            schoolHeader.Inlines.Add(new LineBreak());
            schoolHeader.Inlines.Add(new Run("هاتف: " + m.SchoolPhone) { FontSize = 10, Foreground = muted });
        }
        doc.Blocks.Add(schoolHeader);

        // 2) Title — "سند قبض"
        var title = new Paragraph(new Run("سند قبض")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 18,
            Foreground = teal
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        };
        doc.Blocks.Add(title);

        // Thin divider
        var divider = new BlockUIContainer(new System.Windows.Controls.Border
        {
            Height = 2,
            Background = teal,
            Width = 160,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 10)
        });
        doc.Blocks.Add(divider);

        // 2b) Receipt number + date — 2-column table.
        var meta = MakeTwoColTable(border);
        AddCell(meta, "رقم السند: " + m.ReceiptNumber, bold: true);
        AddCell(meta, "التاريخ: " + m.PaymentDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture), bold: true, align: TextAlignment.Left);
        doc.Blocks.Add(meta);

        // 3) Info table
        var info = MakeInfoTable(border);
        AddInfoRow(info, "اسم الطالب", m.StudentFullName, navy, muted);
        AddInfoRow(info, "رقم الطالب", m.StudentNumber, navy, muted);
        AddInfoRow(info, "الصف", m.GradeName, navy, muted);
        AddInfoRow(info, "الشعبة", m.SectionName, navy, muted);
        AddInfoRow(info, "القسط", m.InstallmentNumber.HasValue ? $"القسط {m.InstallmentNumber.Value}" : "—", navy, muted);
        AddInfoRow(info, "طريقة الدفع", m.MethodLabelAr, navy, muted);
        doc.Blocks.Add(info);

        // 4) Amount block
        var amountP = new Paragraph
        {
            Margin = new Thickness(0, 14, 0, 4),
            TextAlignment = TextAlignment.Right
        };
        amountP.Inlines.Add(new Run("المبلغ: ") { FontWeight = FontWeights.Bold, FontSize = 14 });
        amountP.Inlines.Add(new Run(MoneyFormatter.Format(m.Amount))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = teal
        });
        doc.Blocks.Add(amountP);

        var wordsP = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 10),
            TextAlignment = TextAlignment.Right
        };
        wordsP.Inlines.Add(new Run("المبلغ كتابةً: ") { FontWeight = FontWeights.Bold });
        wordsP.Inlines.Add(new Run(m.AmountInWordsAr) { FontStyle = FontStyles.Italic, Foreground = muted });
        doc.Blocks.Add(wordsP);

        // 5) Notes
        if (!string.IsNullOrWhiteSpace(m.Notes))
        {
            var notesP = new Paragraph
            {
                Margin = new Thickness(0, 4, 0, 10),
                TextAlignment = TextAlignment.Right
            };
            notesP.Inlines.Add(new Run("ملاحظات: ") { FontWeight = FontWeights.Bold });
            notesP.Inlines.Add(new Run(m.Notes));
            doc.Blocks.Add(notesP);
        }

        // 6) Signatures (3-col table)
        var sigTable = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 24, 0, 6)
        };
        sigTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        sigTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        sigTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var sigGroup = new TableRowGroup();
        var sigRow = new TableRow();
        sigRow.Cells.Add(MakeCell("أمين الصندوق: " + (string.IsNullOrWhiteSpace(m.CashierName) ? "____________" : m.CashierName), bold: true, align: TextAlignment.Right));
        sigRow.Cells.Add(MakeCell("المستلم: ____________", bold: true, align: TextAlignment.Center));
        sigRow.Cells.Add(MakeCell("التوقيع: ____________", bold: true, align: TextAlignment.Left));
        sigGroup.Rows.Add(sigRow);
        sigTable.RowGroups.Add(sigGroup);
        doc.Blocks.Add(sigTable);

        // 7) Footer
        var footer = new Paragraph(new Run($"{m.SchoolNameAr} — تم الطباعة في {DateTime.Now:yyyy/MM/dd HH:mm}")
        {
            FontSize = 9,
            Foreground = muted
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 32, 0, 0)
        };
        doc.Blocks.Add(footer);

        return doc;
    }

    private static Table MakeTwoColTable(Brush border)
    {
        var t = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 8) };
        t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        t.RowGroups.Add(new TableRowGroup());
        return t;
    }

    private static void AddCell(Table t, string text, bool bold = false, TextAlignment align = TextAlignment.Right)
    {
        var group = t.RowGroups.Count > 0 ? t.RowGroups[0] : null;
        if (group is null) { group = new TableRowGroup(); t.RowGroups.Add(group); }
        TableRow row;
        if (group.Rows.Count == 0 || group.Rows[group.Rows.Count - 1].Cells.Count >= t.Columns.Count)
        {
            row = new TableRow();
            group.Rows.Add(row);
        }
        else
        {
            row = group.Rows[group.Rows.Count - 1];
        }
        row.Cells.Add(MakeCell(text, bold, align));
    }

    private static TableCell MakeCell(string text, bool bold = false, TextAlignment align = TextAlignment.Right)
    {
        var p = new Paragraph(new Run(text) { FontWeight = bold ? FontWeights.Bold : FontWeights.Normal })
        {
            TextAlignment = align,
            Margin = new Thickness(0)
        };
        return new TableCell(p) { Padding = new Thickness(4, 4, 4, 4) };
    }

    private static Table MakeInfoTable(Brush border)
    {
        var t = new Table
        {
            CellSpacing = 0,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 6, 0, 6)
        };
        t.Columns.Add(new TableColumn { Width = new GridLength(150) });
        t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        t.RowGroups.Add(new TableRowGroup());
        return t;
    }

    private static void AddInfoRow(Table t, string label, string value, Brush primary, Brush muted)
    {
        var group = t.RowGroups[0];
        var row = new TableRow();

        var labelCell = new TableCell(new Paragraph(new Run(label) { FontWeight = FontWeights.SemiBold, Foreground = muted })
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0)
        })
        {
            Padding = new Thickness(8, 6, 8, 6),
            BorderBrush = (Application.Current?.Resources["BorderBrush"] as Brush) ?? Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var valueCell = new TableCell(new Paragraph(new Run(value ?? string.Empty) { Foreground = primary })
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0)
        })
        {
            Padding = new Thickness(8, 6, 8, 6),
            BorderBrush = (Application.Current?.Resources["BorderBrush"] as Brush) ?? Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        row.Cells.Add(labelCell);
        row.Cells.Add(valueCell);
        group.Rows.Add(row);
    }
}
