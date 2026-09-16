using System;
using System.IO;
using System.Xml.Linq;

namespace NasaqPackager.Services;

public sealed class ProjectVersionService : IProjectVersionService
{
    private const string DefaultVersion = "1.14.0";

    public string? GetCurrentVersion(string csprojPath)
    {
        if (!File.Exists(csprojPath)) return null;
        var doc = XDocument.Load(csprojPath);
        var versionElement = FindVersionElement(doc);
        return versionElement?.Value?.Trim();
    }

    public string BumpPatch(string csprojPath)
    {
        if (!File.Exists(csprojPath))
            throw new FileNotFoundException($"ملف المشروع غير موجود: {csprojPath}");

        var doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);
        var versionElement = FindVersionElement(doc);

        string newVersion;
        if (versionElement is null)
        {
            // No <Version> yet — add it under the first <PropertyGroup>
            newVersion = DefaultVersion;
            var firstPropertyGroup = doc.Root?.Element("PropertyGroup");
            if (firstPropertyGroup is null)
                throw new InvalidOperationException("لم يتم العثور على PropertyGroup في ملف المشروع.");

            firstPropertyGroup.Add(new XElement("Version", newVersion));
        }
        else
        {
            var current = versionElement.Value.Trim();
            if (!Version.TryParse(current, out var parsed))
                parsed = new Version(1, 14, 0);

            var major = parsed.Major < 0 ? 1 : parsed.Major;
            var minor = parsed.Minor < 0 ? 14 : parsed.Minor;
            var build = parsed.Build < 0 ? 0 : parsed.Build;
            newVersion = $"{major}.{minor}.{build + 1}";
            versionElement.Value = newVersion;
        }

        doc.Save(csprojPath, SaveOptions.DisableFormatting);
        return newVersion;
    }

    public string SetVersion(string csprojPath, string newVersion)
    {
        if (!File.Exists(csprojPath))
            throw new FileNotFoundException($"ملف المشروع غير موجود: {csprojPath}");
        if (!Version.TryParse(newVersion, out _))
            throw new ArgumentException("صيغة رقم الإصدار غير صحيحة.", nameof(newVersion));

        var doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);
        var versionElement = FindVersionElement(doc);
        if (versionElement is null)
        {
            var firstPropertyGroup = doc.Root?.Element("PropertyGroup");
            if (firstPropertyGroup is null)
                throw new InvalidOperationException("لم يتم العثور على PropertyGroup في ملف المشروع.");
            firstPropertyGroup.Add(new XElement("Version", newVersion));
        }
        else
        {
            versionElement.Value = newVersion;
        }
        doc.Save(csprojPath, SaveOptions.DisableFormatting);
        return newVersion;
    }

    private static XElement? FindVersionElement(XDocument doc)
    {
        if (doc.Root is null) return null;
        foreach (var pg in doc.Root.Elements("PropertyGroup"))
        {
            var ver = pg.Element("Version");
            if (ver is not null) return ver;
        }
        return null;
    }
}
