using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Nasag.Services.Printing;

/// <summary>
/// Converts a decimal monetary amount into a readable Arabic phrase suitable
/// for receipts and statements (e.g. "خمسة آلاف وخمسمئة ريالاً وخمس وعشرون هللة فقط لا غير").
/// Supports values up to 999,999,999.99 with a 2-digit fractional part. The goal is
/// human-readable round-trippable output for typical school-fee values rather
/// than strict grammatical perfection across all edge cases.
/// </summary>
public static class ArabicNumberWords
{
    private const decimal MaxSupported = 999_999_999.99m;

    private static readonly string[] Ones =
    {
        "صفر", "واحد", "اثنان", "ثلاثة", "أربعة",
        "خمسة", "ستة", "سبعة", "ثمانية", "تسعة"
    };

    private static readonly string[] Teens =
    {
        "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر",
        "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"
    };

    private static readonly string[] Tens =
    {
        "", "", "عشرون", "ثلاثون", "أربعون",
        "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
    };

    private static readonly string[] Hundreds =
    {
        "", "مئة", "مئتان", "ثلاثمئة", "أربعمئة",
        "خمسمئة", "ستمئة", "سبعمئة", "ثمانمئة", "تسعمئة"
    };

    /// <summary>
    /// Converts an amount to an Arabic phrase. <paramref name="majorAr"/> is the
    /// main unit (e.g. "ريالاً") and <paramref name="minorAr"/> is the fractional
    /// unit (e.g. "هللة"). Edge cases:
    ///  - amount == 0 ⇒ "صفر"
    ///  - integer > 0 &amp;&amp; fraction == 0 ⇒ "{words} {majorAr} فقط لا غير"
    ///  - integer == 0 &amp;&amp; fraction > 0 ⇒ "{fractionWords} {minorAr} فقط لا غير"
    /// </summary>
    public static string Convert(decimal amount, string majorAr = "ريالاً", string minorAr = "هللة")
    {
        try
        {
            if (amount < 0m) amount = Math.Abs(amount);
            if (amount > MaxSupported)
                return amount.ToString("N2", CultureInfo.InvariantCulture) + " " + majorAr;

            // Round to 2 decimal places.
            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

            // Exactly zero → minimal phrase, no currency suffix.
            if (amount == 0m) return "صفر";

            var integerPart = (long)Math.Truncate(amount);
            var fractionPart = (int)Math.Round((amount - integerPart) * 100m, 0, MidpointRounding.AwayFromZero);

            // Major == 0 && Minor > 0 → fraction-only phrase, omit the major unit entirely.
            if (integerPart == 0 && fractionPart > 0)
                return IntegerToWords(fractionPart) + " " + minorAr + " فقط لا غير";

            var sb = new StringBuilder();

            if (integerPart > 0)
            {
                sb.Append(IntegerToWords(integerPart));
                sb.Append(' ');
                sb.Append(majorAr);
            }

            if (fractionPart > 0)
            {
                if (sb.Length > 0) sb.Append(" و");
                sb.Append(IntegerToWords(fractionPart));
                sb.Append(' ');
                sb.Append(minorAr);
            }

            sb.Append(" فقط لا غير");
            return sb.ToString();
        }
        catch
        {
            return amount.ToString("N2", CultureInfo.InvariantCulture) + " " + majorAr;
        }
    }

    private static string IntegerToWords(long n)
    {
        if (n == 0) return Ones[0];

        var parts = new List<string>();

        // Up to 999,999,999 — split into millions, thousands, units.
        var millions = (int)(n / 1_000_000);
        var afterMillions = n % 1_000_000;
        var thousands = (int)(afterMillions / 1000);
        var remainder = (int)(afterMillions % 1000);

        if (millions > 0)
            parts.Add(MillionsToWords(millions));

        if (thousands > 0)
            parts.Add(ThousandsToWords(thousands));

        if (remainder > 0)
            parts.Add(BelowThousandToWords(remainder));

        return string.Join(" و", parts);
    }

    private static string MillionsToWords(int millions)
    {
        // 1 → مليون, 2 → مليونان, 3..10 → "{count} ملايين", 11+ → "{count} مليوناً".
        if (millions == 1) return "مليون";
        if (millions == 2) return "مليونان";
        if (millions >= 3 && millions <= 10)
            return BelowThousandToWords(millions) + " ملايين";
        return BelowThousandToWords(millions) + " مليوناً";
    }

    private static string ThousandsToWords(int thousands)
    {
        // 1 → ألف, 2 → ألفان, 3..10 → "{count} آلاف", 11+ → "{count} ألفاً".
        if (thousands == 1) return "ألف";
        if (thousands == 2) return "ألفان";
        if (thousands >= 3 && thousands <= 10)
            return BelowThousandToWords(thousands) + " آلاف";
        return BelowThousandToWords(thousands) + " ألفاً";
    }

    private static string BelowThousandToWords(int n)
    {
        if (n <= 0) return string.Empty;
        if (n < 10) return Ones[n];
        if (n < 20) return Teens[n - 10];
        if (n < 100)
        {
            var t = n / 10;
            var u = n % 10;
            if (u == 0) return Tens[t];
            // Arabic order for compound 21..99: units و tens (e.g. "خمس وعشرون").
            return Ones[u] + " و" + Tens[t];
        }

        // 100..999
        var h = n / 100;
        var rest = n % 100;
        if (rest == 0) return Hundreds[h];
        return Hundreds[h] + " و" + BelowThousandToWords(rest);
    }
}
