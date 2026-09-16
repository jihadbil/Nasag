using System;
using System.Security.Cryptography;
using System.Text;

namespace Nasag.Licensing;

/// <summary>
/// أدوات حساب HMAC-SHA256 وتحقّق ثابت الزمن.
/// </summary>
public static class HmacUtil
{
    public static string ComputeBase64(string key, string payload)
    {
        if (key is null) throw new ArgumentNullException(nameof(key), "مفتاح HMAC مطلوب.");
        if (payload is null) throw new ArgumentNullException(nameof(payload), "الحمل النصي مطلوب.");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToBase64String(hash);
    }

    public static bool Verify(string key, string payload, string expectedBase64)
    {
        if (string.IsNullOrEmpty(expectedBase64)) return false;
        try
        {
            var actual = ComputeBase64(key, payload);
            // مقارنة ثابتة الزمن لتفادي هجمات التوقيت.
            var a = Encoding.ASCII.GetBytes(actual);
            var b = Encoding.ASCII.GetBytes(expectedBase64);
            if (a.Length != b.Length) return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
        catch
        {
            return false;
        }
    }
}
