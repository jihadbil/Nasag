param([string]$Version)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    if (-not $Version) {
        $csproj = [xml](Get-Content (Join-Path $root 'Nasag\Nasag.csproj'))
        $Version = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    }
    Write-Host "===== Building Nasaq $Version ====="
    Remove-Item -Recurse -Force .\publish\nasaq -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force .\publish\nasaq-obf -ErrorAction SilentlyContinue
    & dotnet publish .\Nasag\Nasag.csproj -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -o .\publish\nasaq
    if ($LASTEXITCODE -ne 0) { throw "publish failed" }

    & obfuscar.console .\Obfuscar.xml
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "obfuscar failed - proceeding with unobfuscated publish"
        $publishDir = ".\publish\nasaq"
    } else {
        $publishDir = ".\publish\nasaq-obf"
    }

    New-Item -ItemType Directory -Force -Path .\Releases\Customer | Out-Null
    & vpk pack --packId Nasaq --packVersion $Version --packDir $publishDir --mainExe Nasag.exe --packTitle "نَسَق لإدارة المدارس" --outputDir .\Releases\Customer --channel win
    if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

    Write-Host "===== Done. Installer at .\Releases\Customer\Setup.exe ====="
}
finally { Pop-Location }
