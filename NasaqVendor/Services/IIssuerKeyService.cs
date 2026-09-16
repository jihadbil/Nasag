using System;
using System.Threading.Tasks;

namespace NasaqVendor.Services;

public interface IIssuerKeyService
{
    /// <summary>True if a private key is loaded in memory and a public key blob is available.</summary>
    bool HasKey { get; }

    /// <summary>Public key (SubjectPublicKeyInfo) Base64 — safe to display/export.</summary>
    string? PublicKeyBase64 { get; }

    /// <summary>Public key SHA-256 fingerprint (Hex with colons every 2 bytes).</summary>
    string? PublicKeyFingerprint { get; }

    /// <summary>Path to the encrypted private key file on disk.</summary>
    string PrivateKeyFilePath { get; }

    event EventHandler? KeyChanged;

    Task EnsureLoadedAsync();
    Task GenerateNewKeyPairAsync();
    Task ImportPrivateKeyAsync(byte[] rawPrivateKey);
    byte[] GetPublicKeyBlob();
    byte[] GetPrivateKeyBlob();
    Task DeleteKeyAsync();
}
