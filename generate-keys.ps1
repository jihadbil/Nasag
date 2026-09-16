$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$code = @"
using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var priv = ecdsa.ExportECPrivateKey();
        var pub = ecdsa.ExportSubjectPublicKeyInfo();
        File.WriteAllBytes(@"$root\Nasag\Resources\issuer.public.key", pub);
        File.WriteAllBytes(@"$root\dev-issuer.private.key", priv);
        Console.WriteLine("Key pair generated successfully.");
    }
}
"@

$tempDir = Join-Path $env:TEMP "KeyGenNet8"
if (Test-Path $tempDir) { Remove-Item -Recurse -Force $tempDir }
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
Set-Content -Path (Join-Path $tempDir "Program.cs") -Value $code -Encoding UTF8
Set-Content -Path (Join-Path $tempDir "KeyGenNet8.csproj") -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
"@ -Encoding UTF8

Write-Host "Generating fresh ECDSA-P256 key pair via .NET 8..."
& dotnet run --project $tempDir
Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue

Write-Host "===== New ECDSA Key Pair Generated Successfully ====="
Write-Host "Public Key saved to: $root\Nasag\Resources\issuer.public.key"
Write-Host "Private Key saved to: $root\dev-issuer.private.key"
