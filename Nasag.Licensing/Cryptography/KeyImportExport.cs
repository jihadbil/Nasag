using System;
using System.Security.Cryptography;
using System.Text;

namespace Nasag.Licensing.Cryptography;

/// <summary>
/// مساعدات لتصدير/استيراد مفاتيح ECDSA بصيغة Base64 + PEM.
/// </summary>
public static class KeyImportExport
{
    private const string PemPrivateHeader = "-----BEGIN EC PRIVATE KEY-----";
    private const string PemPrivateFooter = "-----END EC PRIVATE KEY-----";
    private const string PemPublicHeader = "-----BEGIN PUBLIC KEY-----";
    private const string PemPublicFooter = "-----END PUBLIC KEY-----";

    public static string PrivateKeyToBase64(byte[] privateKey)
    {
        if (privateKey is null || privateKey.Length == 0)
            throw new ArgumentException("المفتاح الخاص مطلوب.", nameof(privateKey));
        return Convert.ToBase64String(privateKey);
    }

    public static byte[] PrivateKeyFromBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException("نص Base64 للمفتاح الخاص مطلوب.", nameof(base64));
        try { return Convert.FromBase64String(base64.Trim()); }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("صيغة المفتاح الخاص (Base64) غير صالحة.", ex);
        }
    }

    public static string PublicKeyToBase64(byte[] publicKey)
    {
        if (publicKey is null || publicKey.Length == 0)
            throw new ArgumentException("المفتاح العام مطلوب.", nameof(publicKey));
        return Convert.ToBase64String(publicKey);
    }

    public static byte[] PublicKeyFromBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException("نص Base64 للمفتاح العام مطلوب.", nameof(base64));
        try { return Convert.FromBase64String(base64.Trim()); }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("صيغة المفتاح العام (Base64) غير صالحة.", ex);
        }
    }

    public static string PrivateKeyToPem(byte[] privateKey)
    {
        var b64 = PrivateKeyToBase64(privateKey);
        return WrapPem(PemPrivateHeader, PemPrivateFooter, b64);
    }

    public static byte[] PrivateKeyFromPem(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)) throw new ArgumentException("نص PEM للمفتاح الخاص مطلوب.", nameof(pem));
        var b64 = StripPem(pem, PemPrivateHeader, PemPrivateFooter);
        return PrivateKeyFromBase64(b64);
    }

    public static string PublicKeyToPem(byte[] publicKey)
    {
        var b64 = PublicKeyToBase64(publicKey);
        return WrapPem(PemPublicHeader, PemPublicFooter, b64);
    }

    public static byte[] PublicKeyFromPem(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)) throw new ArgumentException("نص PEM للمفتاح العام مطلوب.", nameof(pem));
        var b64 = StripPem(pem, PemPublicHeader, PemPublicFooter);
        return PublicKeyFromBase64(b64);
    }

    private static string WrapPem(string header, string footer, string base64)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        const int lineLen = 64;
        for (int i = 0; i < base64.Length; i += lineLen)
        {
            sb.AppendLine(base64.Substring(i, Math.Min(lineLen, base64.Length - i)));
        }
        sb.AppendLine(footer);
        return sb.ToString();
    }

    private static string StripPem(string pem, string header, string footer)
    {
        var trimmed = pem.Trim();
        var hIdx = trimmed.IndexOf(header, StringComparison.Ordinal);
        var fIdx = trimmed.IndexOf(footer, StringComparison.Ordinal);
        if (hIdx < 0 || fIdx < 0 || fIdx <= hIdx)
            throw new InvalidOperationException("صيغة PEM غير صالحة.");
        var body = trimmed.Substring(hIdx + header.Length, fIdx - (hIdx + header.Length));
        var sb = new StringBuilder();
        foreach (var ch in body)
            if (!char.IsWhiteSpace(ch)) sb.Append(ch);
        return sb.ToString();
    }
}
