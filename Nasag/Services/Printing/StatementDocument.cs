using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Nasag.Helpers;

namespace Nasag.Services.Printing;

public sealed record StatementInstallmentRow(
    int Number,
    decimal Amount,
    decimal Paid,
    decimal Remaining,
    DateTime DueDate,
    string StatusLabelAr);

public sealed record StatementPaymentRow(
    string ReceiptNumber,
    DateTime PaymentDate,
    decimal Amount,
    string MethodLabelAr,
    int? InstallmentNumber,
    string? Notes);

public sealed record StatementModel(
    string SchoolNameAr,
    string? SchoolAddress,
    string? SchoolPhone,
    DateTime IssuedAt,
    string StudentFullName,
    string StudentNumber,
    string GradeName,
    string SectionName,
    string FeePlanName,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<StatementInstallmentRow> Installments,
    IReadOnlyList<StatementPaymentRow> Payments);

/// <summary>
/// Builds a multi-page A4 RTL FlowDocument representing a student fee account
/// statement ("كشف حساب الطالب").
/// </summary>
public static class StatementDocument
{
    public static FlowDocument Build(StatementModel m)
    {
        var tajawal = Application.Current?.Resources["TajawalFont"] as FontFamily ?? new FontFamily("Segoe UI");
        var teal = (Application.Current?.Resources["TealPressedBrush"] as Brush) ?? Brushes.Teal;
        var navy = (Application.Current?.Resources["TextPrimaryBrush"] as Brush) ?? Brushes.Black;
        var muted = (Application.Current?.Resources["TextSecondaryBrush"] as Brush) ?? Brushes.Gray;
        var border = (Application.Current?.Resources["BorderBrush"] as Brush) ?? Brushes.LightGray;
        var headerBg = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));

        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = tajawal,
            FontSize = 12,
            TextAlignment = TextAlignment.Right,
            Foreground = navy
        };

        // 1) School header
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

        // 2) Title
        var title = new Paragraph(new Run("كشف حساب الطالب")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 18,
            Foreground = teal
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 2)
        };
        doc.Blocks.Add(title);

        var issued = new Paragraph(new Run("صدر بتاريخ: " + m.IssuedAt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture))
        {
            FontSize = 10,
            Foreground = muted
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        doc.Blocks.Add(issued);

        // 3) Student info table (2 col, label/value pairs, 2 rows of 2)
        var info = new Table
        {
            CellSpacing = 0,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 4, 0, 10)
        };
        for (int i = 0; i < 4; i++)
            info.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var infoGroup = new TableRowGroup();

        var row1 = new TableRow();
        row1.Cells.Add(MakeLabelCell("اسم الطالب", muted, border));
        row1.Cells.Add(MakeValueCell(m.StudentFullName, navy, border));
        row1.Cells.Add(MakeLabelCell("رقم الطالب", muted, border));
        row1.Cells.Add(MakeValueCell(m.StudentNumber, navy, border));
        infoGroup.Rows.Add(row1);

        var row2 = new TableRow();
        row2.Cells.Add(MakeLabelCell("الصف", muted, border));
        row2.Cells.Add(MakeValueCell(m.GradeName, navy, border));
        row2.Cells.Add(MakeLabelCell("الشعبة", muted, border));
        row2.Cells.Add(MakeValueCell(m.SectionName, navy, border));
        infoGroup.Rows.Add(row2);

        info.RowGroups.Add(infoGroup);
        doc.Blocks.Add(info);

        // 4) Plan summary
        var pct = m.TotalAmount > 0m ? ((double)(m.PaidAmount / m.TotalAmount) * 100.0) : 0.0;
        var summary = new Paragraph
        {
            Margin = new Thickness(0, 4, 0, 10),
            TextAlignment = TextAlignment.Right
        };
        summary.Inlines.Add(new Run("خطة الرسوم: ") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run(m.FeePlanName) { Foreground = teal, FontWeight = FontWeights.SemiBold });
        summary.Inlines.Add(new Run("    •    ") { Foreground = muted });
        summary.Inlines.Add(new Run("إجمالي: ") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run(FormatMoney(m.TotalAmount)));
        summary.Inlines.Add(new Run("    •    ") { Foreground = muted });
        summary.Inlines.Add(new Run("مدفوع: ") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run($"{FormatMoney(m.PaidAmount)} ({pct.ToString("0.00", CultureInfo.InvariantCulture)}%)"));
        summary.Inlines.Add(new Run("    •    ") { Foreground = muted });
        summary.Inlines.Add(new Run("متبقي: ") { FontWeight = FontWeights.Bold });
        summary.Inlines.Add(new Run(FormatMoney(m.RemainingAmount)) { Foreground = Brushes.Firebrick, FontWeight = FontWeights.Bold });
        doc.Blocks.Add(summary);

        // 5) Installments table
        var installTitle = new Paragraph(new Run("الأقساط")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = navy
        })
        { Margin = new Thickness(0, 8, 0, 4) };
        doc.Blocks.Add(installTitle);

        if (m.Installments.Count == 0)
        {
            doc.Blocks.Add(new Paragraph(new Run("لا توجد أقساط مسجلة.") { Foreground = muted })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 8)
            });
        }
        else
        {
            var iTable = new Table
            {
                CellSpacing = 0,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 2, 0, 10)
            };
            foreach (var w in new[] { 40, 110, 100, 100, 100, 120 })
                iTable.Columns.Add(new TableColumn { Width = new GridLength(w) });

            var iGroup = new TableRowGroup();
            var hdr = new TableRow { Background = headerBg };
            hdr.Cells.Add(MakeHeaderCell("#", border));
            hdr.Cells.Add(MakeHeaderCell("تاريخ الاستحقاق", border));
            hdr.Cells.Add(MakeHeaderCell("المبلغ", border));
            hdr.Cells.Add(MakeHeaderCell("المدفوع", border));
            hdr.Cells.Add(MakeHeaderCell("المتبقي", border));
            hdr.Cells.Add(MakeHeaderCell("الحالة", border));
            iGroup.Rows.Add(hdr);

            foreach (var inst in m.Installments)
            {
                var r = new TableRow();
                r.Cells.Add(MakeBodyCell(inst.Number.ToString(CultureInfo.InvariantCulture), border));
                r.Cells.Add(MakeBodyCell(inst.DueDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture), border));
                r.Cells.Add(MakeBodyCell(FormatMoney(inst.Amount), border));
                r.Cells.Add(MakeBodyCell(FormatMoney(inst.Paid), border));
                r.Cells.Add(MakeBodyCell(FormatMoney(inst.Remaining), border));
                r.Cells.Add(MakeBodyCell(inst.StatusLabelAr ?? string.Empty, border));
                iGroup.Rows.Add(r);
            }

            iTable.RowGroups.Add(iGroup);
            doc.Blocks.Add(iTable);
        }

        // 6) Payments table
        var payTitle = new Paragraph(new Run("الدفعات")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = navy
        })
        { Margin = new Thickness(0, 8, 0, 4) };
        doc.Blocks.Add(payTitle);

        if (m.Payments.Count == 0)
        {
            doc.Blocks.Add(new Paragraph(new Run("لا توجد دفعات مسجلة.") { Foreground = muted })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 8)
            });
        }
        else
        {
            var pTable = new Table
            {
                CellSpacing = 0,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 2, 0, 10)
            };
            foreach (var w in new[] { 110, 100, 100, 90, 70, 1 })
            {
                if (w == 1)
                    pTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
                else
                    pTable.Columns.Add(new TableColumn { Width = new GridLength(w) });
            }

            var pGroup = new TableRowGroup();
            var hdr = new TableRow { Background = headerBg };
            hdr.Cells.Add(MakeHeaderCell("رقم السند", border));
            hdr.Cells.Add(MakeHeaderCell("التاريخ", border));
            hdr.Cells.Add(MakeHeaderCell("المبلغ", border));
            hdr.Cells.Add(MakeHeaderCell("الطريقة", border));
            hdr.Cells.Add(MakeHeaderCell("القسط", border));
            hdr.Cells.Add(MakeHeaderCell("ملاحظات", border));
            pGroup.Rows.Add(hdr);

            foreach (var p in m.Payments)
            {
                var r = new TableRow();
                r.Cells.Add(MakeBodyCell(p.ReceiptNumber, border));
                r.Cells.Add(MakeBodyCell(p.PaymentDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture), border));
                r.Cells.Add(MakeBodyCell(FormatMoney(p.Amount), border));
                r.Cells.Add(MakeBodyCell(p.MethodLabelAr ?? string.Empty, border));
                r.Cells.Add(MakeBodyCell(p.InstallmentNumber.HasValue ? $"القسط {p.InstallmentNumber.Value}" : "—", border));
                r.Cells.Add(MakeBodyCell(p.Notes ?? string.Empty, border, align: TextAlignment.Right));
                pGroup.Rows.Add(r);
            }

            pTable.RowGroups.Add(pGroup);
            doc.Blocks.Add(pTable);
        }

        // 7) Footer
        var footer = new Paragraph(new Run($"{m.SchoolNameAr} — تم الطباعة في {DateTime.Now:yyyy/MM/dd HH:mm}")
        {
            FontSize = 9,
            Foreground = muted
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };
        doc.Blocks.Add(footer);

        return doc;
    }

    private static string FormatMoney(decimal value) => MoneyFormatter.Format(value);

    private static TableCell MakeLabelCell(string text, Brush muted, Brush border)
    {
        var p = new Paragraph(new Run(text) { FontWeight = FontWeights.SemiBold, Foreground = muted })
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0)
        };
        return new TableCell(p)
        {
            Padding = new Thickness(8, 6, 8, 6),
            BorderBrush = border,
            BorderThickness = new Thickness(0.5)
        };
    }

    private static TableCell MakeValueCell(string text, Brush primary, Brush border)
    {
        var p = new Paragraph(new Run(text ?? string.Empty) { Foreground = primary })
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0)
        };
        return new TableCell(p)
        {
            Padding = new Thickness(8, 6, 8, 6),
            BorderBrush = border,
            BorderThickness = new Thickness(0.5)
        };
    }

    private static TableCell MakeHeaderCell(string text, Brush border)
    {
        var p = new Paragraph(new Run(text) { FontWeight = FontWeights.Bold })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0)
        };
        return new TableCell(p)
        {
            Padding = new Thickness(6, 6, 6, 6),
            BorderBrush = border,
            BorderThickness = new Thickness(0.5)
        };
    }

    private static TableCell MakeBodyCell(string text, Brush border, TextAlignment align = TextAlignment.Center)
    {
        var p = new Paragraph(new Run(text ?? string.Empty))
        {
            TextAlignment = align,
            Margin = new Thickness(0)
        };
        return new TableCell(p)
        {
            Padding = new Thickness(6, 5, 6, 5),
            BorderBrush = border,
            BorderThickness = new Thickness(0.5)
        };
    }
}
