using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NasaqPackager.Services;

public sealed class PipelineRunner : IPipelineRunner
{
    public async IAsyncEnumerable<string> RunPipelineAsync(
        PipelineConfig cfg,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cfg.ProjectPath) || !File.Exists(cfg.ProjectPath))
        {
            yield return $"[خطأ] ملف المشروع غير موجود: {cfg.ProjectPath}";
            yield break;
        }

        var solutionRoot = Path.GetDirectoryName(Path.GetDirectoryName(cfg.ProjectPath))
                           ?? Path.GetDirectoryName(cfg.ProjectPath)
                           ?? AppContext.BaseDirectory;

        var publishDir = Path.Combine(solutionRoot, "artifacts", "publish", cfg.PackId, cfg.Rid);
        var obfuscarOut = Path.Combine(solutionRoot, "publish", "nasaq-obf");

        // --- Stage 0: cleanup ---
        yield return "";
        yield return "═══════════════════════════════════════════";
        yield return "[0/3] تنظيف المجلدات السابقة...";
        yield return "═══════════════════════════════════════════";
        foreach (var path in new[] { publishDir, obfuscarOut })
        {
            if (Directory.Exists(path))
            {
                var attempt = TryDeleteDirectory(path);
                yield return attempt is null
                    ? $"  ✓ تم حذف: {path}"
                    : $"  ⚠ تعذّر حذف {path}: {attempt}";
            }
        }
        try { Directory.CreateDirectory(publishDir); } catch { }
        try { Directory.CreateDirectory(cfg.ReleasesPath); } catch { }

        // --- Stage 1: dotnet publish ---
        yield return "";
        yield return "═══════════════════════════════════════════";
        yield return $"[1/3] dotnet publish — {cfg.Rid} (self-contained={cfg.SelfContained})";
        yield return "═══════════════════════════════════════════";

        var publishArgs =
            $"publish \"{cfg.ProjectPath}\" -c Release -r {cfg.Rid} --self-contained {cfg.SelfContained.ToString().ToLowerInvariant()} " +
            $"-p:DebugType=none -p:DebugSymbols=false -o \"{publishDir}\"";

        await foreach (var line in RunProcessAsync("dotnet", publishArgs, solutionRoot, ct))
        {
            yield return line;
        }

        // --- Stage 2: obfuscar (optional) ---
        if (cfg.RunObfuscar)
        {
            yield return "";
            yield return "═══════════════════════════════════════════";
            yield return "[2/3] obfuscar.console";
            yield return "═══════════════════════════════════════════";

            var obfuscarConfig = Path.Combine(solutionRoot, "Obfuscar.xml");
            if (!File.Exists(obfuscarConfig))
            {
                yield return $"[تحذير] لم يتم العثور على ملف Obfuscar.xml — تخطي مرحلة التشويش.";
            }
            else
            {
                await foreach (var line in RunProcessAsync("obfuscar.console", $"\"{obfuscarConfig}\"", solutionRoot, ct))
                {
                    yield return line;
                }
            }
        }
        else
        {
            yield return "";
            yield return "[2/3] التشويش (Obfuscar) معطل — تخطي.";
        }

        // --- Stage 3: vpk pack ---
        yield return "";
        yield return "═══════════════════════════════════════════";
        yield return $"[3/3] vpk pack — {cfg.PackId} v{cfg.Version} → {cfg.ReleasesPath}";
        yield return "═══════════════════════════════════════════";

        var vpkArgs =
            $"pack --packId {cfg.PackId} --packVersion {cfg.Version} --packDir \"{publishDir}\" " +
            $"--mainExe Nasag.exe --packTitle \"{cfg.PackTitle}\" " +
            $"--outputDir \"{cfg.ReleasesPath}\" --channel {cfg.Channel}";

        var resolvedIcon = ResolveIcon(cfg.IconPath);
        if (resolvedIcon is not null)
        {
            if (!string.Equals(resolvedIcon, cfg.IconPath, StringComparison.OrdinalIgnoreCase))
                yield return $"ℹ تم استخدام أيقونة .ico بجوار الملف المُكوَّن: {resolvedIcon}";
            vpkArgs += $" --icon \"{resolvedIcon}\"";
        }
        else if (!string.IsNullOrWhiteSpace(cfg.IconPath))
        {
            yield return $"⚠ تم تخطي الأيقونة: {cfg.IconPath} — vpk يتطلب امتداد .ico فقط، ولا يوجد ملف .ico بجواره";
        }

        await foreach (var line in RunProcessAsync("vpk", vpkArgs, solutionRoot, ct))
        {
            yield return line;
        }

        yield return "";
        yield return "═══════════════════════════════════════════";
        yield return $"[تم] إصدار {cfg.PackId} v{cfg.Version} جاهز في: {cfg.ReleasesPath}";
        yield return "═══════════════════════════════════════════";
    }

    private static string? TryDeleteDirectory(string path)
    {
        try
        {
            // Strip read-only attribute on files first (vpk sometimes leaves locked artifacts).
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { File.SetAttributes(f, FileAttributes.Normal); } catch { /* تجاهل */ }
            }
            Directory.Delete(path, recursive: true);
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string? ResolveIcon(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured)) return null;
        if (File.Exists(configured) &&
            string.Equals(Path.GetExtension(configured), ".ico", StringComparison.OrdinalIgnoreCase))
            return configured;
        // Auto-swap: if configured path is a non-.ico (e.g. Logo.png), look for a sibling .ico.
        var dir = Path.GetDirectoryName(configured);
        var name = Path.GetFileNameWithoutExtension(configured);
        if (!string.IsNullOrWhiteSpace(dir) && !string.IsNullOrWhiteSpace(name))
        {
            var candidate = Path.Combine(dir, name + ".ico");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static async IAsyncEnumerable<string> RunProcessAsync(
        string fileName,
        string arguments,
        string workingDir,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDir,
        };

        Process? process = null;
        Exception? startError = null;

        await channel.Writer.WriteAsync($"> {fileName} {arguments}", ct);

        try
        {
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    channel.Writer.TryWrite(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    channel.Writer.TryWrite(e.Data);
            };

            if (!process.Start())
                throw new InvalidOperationException($"فشل تشغيل: {fileName}");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            startError = ex;
        }

        if (startError is not null)
        {
            await channel.Writer.WriteAsync($"[خطأ] فشل تشغيل '{fileName}': {startError.Message}", ct);
            channel.Writer.TryComplete();
            await foreach (var line in channel.Reader.ReadAllAsync(ct))
                yield return line;
            throw new InvalidOperationException($"فشل تشغيل '{fileName}': {startError.Message}", startError);
        }

        // Pump: wait for process exit on a background task, then complete the channel.
        var waiter = Task.Run(async () =>
        {
            try
            {
                await process!.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { if (!process!.HasExited) process.Kill(true); } catch { }
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, ct);

        await foreach (var line in channel.Reader.ReadAllAsync(ct))
            yield return line;

        await waiter.ConfigureAwait(false);

        var exitCode = process!.ExitCode;
        process.Dispose();

        if (exitCode != 0)
            throw new InvalidOperationException($"'{fileName}' انتهى برمز خروج غير صفري: {exitCode}");
    }
}
