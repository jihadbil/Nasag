using System;
using System.Security.Cryptography;

namespace Nasag.Licensing.Cryptography;

/// <summary>
/// توقيع/تحقّق ECDSA على منحنى P-256 مع SHA-256.
/// </summary>
public static class EcdsaSigner
{
    public static (byte[] PrivateKey, byte[] PublicKey) GenerateKeyPair()
    {
        try
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var priv = ecdsa.ExportECPrivateKey();
            var pub = ecdsa.ExportSubjectPublicKeyInfo();
            return (priv, pub);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("فشل توليد زوج مفاتيح ECDSA على المنحنى P-256.", ex);
        }
    }

    public static byte[] Sign(byte[] data, byte[] privateKeyBlob)
    {
        if (data is null) throw new ArgumentNullException(nameof(data), "البيانات المراد توقيعها مطلوبة.");
        if (privateKeyBlob is null || privateKeyBlob.Length == 0)
            throw new ArgumentException("المفتاح الخاص مطلوب.", nameof(privateKeyBlob));

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(privateKeyBlob, out _);
            return ecdsa.SignData(data, HashAlgorithmName.SHA256);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("فشل توقيع البيانات: المفتاح الخاص غير صالح.", ex);
        }
    }

    public static bool Verify(byte[] data, byte[] signature, byte[] publicKeyBlob)
    {
        if (data is null || signature is null || publicKeyBlob is null) return false;
        if (signature.Length == 0 || publicKeyBlob.Length == 0) return false;

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBlob, out _);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
        }
        catch
        {
            return false;
        }
    }
}
