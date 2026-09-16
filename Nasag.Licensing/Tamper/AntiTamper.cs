using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Nasag.Licensing.Tamper;

/// <summary>
/// فحوصات بسيطة مضادة للعبث: كشف مُنقّح، فحص توقيت، تجزئة التجميع.
/// </summary>
public static class AntiTamper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isPresent);

    public static bool DebuggerAttached()
    {
        if (Debugger.IsAttached) return true;
        try
        {
            using var proc = Process.GetCurrentProcess();
            if (CheckRemoteDebuggerPresent(proc.Handle, out var isPresent))
                return isPresent;
        }
        catch
        {
            // تجاهل
        }
        return false;
    }

    /// <summary>
    /// فحص توقيت بدائي: حلقة لا تفعل شيئاً — لو استغرقت 50× المتوقع، فالأرجح وجود منقّح.
    /// </summary>
    public static bool TimingAttackDetected()
    {
        try
        {
            const int iterations = 200_000;
            var sw = Stopwatch.StartNew();
            int x = 0;
            for (int i = 0; i < iterations; i++)
            {
                unchecked { x += i; }
            }
            sw.Stop();
            GC.KeepAlive(x);

            // المتوقَّع بضع ميلي ثوانٍ على أجهزة عادية؛ نسمح بسخاء.
            // نعتبر التوقيت مريباً إذا تجاوز 250ms لحلقة 200k عملية جمع.
            return sw.ElapsedMilliseconds > 250;
        }
        catch
        {
            return false;
        }
    }

    public static string ComputeAssemblyHash(Assembly assembly)
    {
        if (assembly is null) throw new ArgumentNullException(nameof(assembly), "التجميع مطلوب.");
        try
        {
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location) || !File.Exists(location))
                return "";
            using var fs = File.OpenRead(location);
            var hash = SHA256.HashData(fs);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
        catch
        {
            return "";
        }
    }
}
