using System;
using System.IO;
using System.Reflection;

namespace Nasag.Licensing.Cryptography;

/// <summary>
/// قراءة المفتاح العام المضمَّن كمورد داخل التجميع المضيف.
/// </summary>
public static class EmbeddedPublicKey
{
    public static byte[] LoadFromAssembly(Assembly assembly, string resourceName)
    {
        if (assembly is null) throw new ArgumentNullException(nameof(assembly), "التجميع مطلوب.");
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new ArgumentException("اسم المورد المضمَّن مطلوب.", nameof(resourceName));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"تعذّر إيجاد المفتاح العام المضمَّن باسم: {resourceName}");
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        if (bytes.Length == 0)
            throw new InvalidOperationException("المفتاح العام المضمَّن فارغ.");
        return bytes;
    }
}
