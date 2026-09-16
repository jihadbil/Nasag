using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nasag.Licensing.License;

/// <summary>
/// تسلسل/فك تسلسل ملفات الترخيص بصيغة JSON متعارف عليها (Canonical) للتوقيع.
/// </summary>
public static class LicenseSerializer
{
    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    private static readonly JsonSerializerOptions PlainOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    /// تسلسل قانوني (مرتّب الخصائص، بدون مسافات، بدون حقل Signature) — يُمرَّر للتوقيع/التحقق.
    /// </summary>
    public static byte[] SerializeForSigning(LicenseFile license)
    {
        if (license is null) throw new ArgumentNullException(nameof(license), "بيانات الترخيص مطلوبة.");

        var clone = new LicenseFile
        {
            V = license.V,
            CustomerId = license.CustomerId ?? "",
            CustomerName = license.CustomerName ?? "",
            MachineHashes = license.MachineHashes ?? Array.Empty<string>(),
            IssuedAtUtc = DateTime.SpecifyKind(license.IssuedAtUtc, DateTimeKind.Utc),
            ExpiresAtUtc = license.ExpiresAtUtc.HasValue
                ? DateTime.SpecifyKind(license.ExpiresAtUtc.Value, DateTimeKind.Utc)
                : null,
            Edition = license.Edition,
            Features = license.Features ?? Array.Empty<string>(),
            Signature = "", // مستثنى من التوقيع
        };

        var raw = JsonSerializer.SerializeToNode(clone, PlainOptions) as JsonObject
                  ?? throw new InvalidOperationException("فشل تسلسل بيانات الترخيص.");

        // نُسقط حقل Signature صراحةً قبل الترتيب لضمان الحتمية.
        raw.Remove("Signature");

        var sorted = SortRecursive(raw)
            ?? throw new InvalidOperationException("فشل ترتيب بيانات الترخيص لاحتساب التوقيع.");
        var canonical = sorted.ToJsonString(PlainOptions);
        return Encoding.UTF8.GetBytes(canonical);
    }

    /// <summary>
    /// تسلسل مقروء (Pretty) كاملاً مع التوقيع لاستخدامه في كتابة ملف .naslic.
    /// </summary>
    public static string Serialize(LicenseFile license)
    {
        if (license is null) throw new ArgumentNullException(nameof(license), "بيانات الترخيص مطلوبة.");
        return JsonSerializer.Serialize(license, PrettyOptions);
    }

    /// <summary>
    /// قراءة ملف ترخيص من نص JSON.
    /// </summary>
    public static LicenseFile Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("نص JSON للترخيص مطلوب.", nameof(json));
        try
        {
            var lic = JsonSerializer.Deserialize<LicenseFile>(json);
            return lic ?? throw new InvalidOperationException("ملف الترخيص فارغ أو غير صالح.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("ملف الترخيص تالف أو بتنسيق JSON غير صالح.", ex);
        }
    }

    /// <summary>
    /// قراءة ملف ترخيص من المسار.
    /// </summary>
    public static LicenseFile DeserializeFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("مسار ملف الترخيص مطلوب.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("ملف الترخيص غير موجود.", path);

        var text = File.ReadAllText(path, Encoding.UTF8);
        return Deserialize(text);
    }

    // ---- داخلي: ترتيب JsonNode تكرارياً ----

    private static JsonNode? SortRecursive(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var sortedObj = new JsonObject();
            foreach (var kvp in obj.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sortedObj[kvp.Key] = SortRecursive(kvp.Value?.DeepClone());
            }
            return sortedObj;
        }
        if (node is JsonArray arr)
        {
            var sortedArr = new JsonArray();
            foreach (var item in arr)
            {
                sortedArr.Add(SortRecursive(item?.DeepClone()));
            }
            return sortedArr;
        }
        return node;
    }
}
