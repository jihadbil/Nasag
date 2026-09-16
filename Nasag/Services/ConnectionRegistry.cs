using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Nasag.Services;

/// <summary>
/// تنفيذ <see cref="IConnectionRegistry"/> ثابت الحالة (thread-safe) يحفظ سجل
/// الاتصالات إلى <c>%LOCALAPPDATA%\Nasaq\connections.json</c>.
/// يدعم Migration صامتة من ملف Phase 13 القديم (<c>connection.json</c>).
/// </summary>
public sealed class ConnectionRegistry : IConnectionRegistry
{
    private const string FolderName = "Nasaq";
    private const string FileName = "connections.json";
    private const string LegacyFileName = "connection.json";
    private const string LegacyJsonKey = "DefaultConnection";
    private const string AppSettingsKey = "DefaultConnection";

    /// <summary>
    /// قيمة LocalDB افتراضية تُستخدم حين لا يوجد اتصال نشط ولا قيمة في
    /// <c>appsettings.json</c>. تضمن أن <see cref="ActiveConnectionString"/> لا
    /// تكون فارغة أبداً، فتسمح للـ Host بالبناء بأمان.
    /// </summary>
    private const string DefaultLocalDbConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=NasaqSchoolDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    private readonly IConfiguration _configuration;
    private readonly object _gate = new();

    private RegistryFile _state = new();

    // Stable snapshot — refreshed inside the lock on every mutation. Returning the
    // same reference across reads prevents WPF bindings (e.g. SearchableComboBox)
    // from re-binding while the user is interacting.
    private IReadOnlyList<SavedConnection> _allSnapshot = Array.Empty<SavedConnection>();

    public ConnectionRegistry(IConfiguration configuration)
    {
        _configuration = configuration;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, FolderName);
        StoreFilePath = Path.Combine(dir, FileName);

        Load();
    }

    public string StoreFilePath { get; }

    public event EventHandler? Changed;

    public IReadOnlyList<SavedConnection> All
    {
        get
        {
            lock (_gate) { return _allSnapshot; }
        }
    }

    private void RefreshSnapshotNoLock()
    {
        _allSnapshot = _state.Connections
            .OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public SavedConnection? Active
    {
        get
        {
            lock (_gate)
            {
                if (_state.ActiveConnectionId is null) return null;
                return _state.Connections.FirstOrDefault(c => c.Id == _state.ActiveConnectionId.Value);
            }
        }
    }

    public string ActiveConnectionString
    {
        get
        {
            var active = Active;
            if (active is not null && !string.IsNullOrWhiteSpace(active.ConnectionString))
                return active.ConnectionString;

            var fromAppSettings = _configuration.GetConnectionString(AppSettingsKey);
            if (!string.IsNullOrWhiteSpace(fromAppSettings))
                return fromAppSettings!;

            return DefaultLocalDbConnectionString;
        }
    }

    public string Source
    {
        get
        {
            var active = Active;
            if (active is not null && !string.IsNullOrWhiteSpace(active.ConnectionString))
                return "Saved";

            var fromAppSettings = _configuration.GetConnectionString(AppSettingsKey);
            if (!string.IsNullOrWhiteSpace(fromAppSettings))
                return "AppSettings";

            return "Default";
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (_gate)
            {
                return _state.Connections.Count == 0;
            }
        }
    }

    public SavedConnection Add(string displayName, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("الاسم الظاهر مطلوب.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("سلسلة الاتصال لا يمكن أن تكون فارغة.", nameof(connectionString));

        SavedConnection entry;
        lock (_gate)
        {
            entry = new SavedConnection
            {
                Id = Guid.NewGuid(),
                DisplayName = displayName.Trim(),
                ConnectionString = connectionString,
                CreatedAt = DateTime.UtcNow
            };

            _state.Connections.Add(entry);

            // أول اتصال يصبح النشط تلقائياً.
            if (_state.ActiveConnectionId is null)
                _state.ActiveConnectionId = entry.Id;

            Save();
            RefreshSnapshotNoLock();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return entry;
    }

    public void Update(Guid id, string displayName, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("الاسم الظاهر مطلوب.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("سلسلة الاتصال لا يمكن أن تكون فارغة.", nameof(connectionString));

        lock (_gate)
        {
            var existing = _state.Connections.FirstOrDefault(c => c.Id == id)
                           ?? throw new InvalidOperationException("الاتصال المحدد غير موجود.");

            existing.DisplayName = displayName.Trim();
            existing.ConnectionString = connectionString;

            Save();
            RefreshSnapshotNoLock();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Guid id)
    {
        lock (_gate)
        {
            var existing = _state.Connections.FirstOrDefault(c => c.Id == id);
            if (existing is null) return;

            _state.Connections.Remove(existing);

            if (_state.ActiveConnectionId == id)
            {
                _state.ActiveConnectionId = _state.Connections.Count > 0
                    ? _state.Connections[0].Id
                    : null;
            }

            Save();
            RefreshSnapshotNoLock();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetActive(Guid id)
    {
        lock (_gate)
        {
            var existing = _state.Connections.FirstOrDefault(c => c.Id == id)
                           ?? throw new InvalidOperationException("الاتصال المحدد غير موجود.");

            _state.ActiveConnectionId = existing.Id;
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void MarkActiveUsed()
    {
        lock (_gate)
        {
            if (_state.ActiveConnectionId is null) return;
            var active = _state.Connections.FirstOrDefault(c => c.Id == _state.ActiveConnectionId.Value);
            if (active is null) return;

            active.LastUsedAt = DateTime.UtcNow;
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    // ─── Persistence ────────────────────────────────────────────────────────

    private void Load()
    {
        lock (_gate)
        {
            try
            {
                if (File.Exists(StoreFilePath))
                {
                    var json = File.ReadAllText(StoreFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var parsed = JsonSerializer.Deserialize<RegistryFile>(json);
                        if (parsed is not null)
                        {
                            _state = parsed;
                            _state.Connections ??= new List<SavedConnection>();
                            return;
                        }
                    }
                }

                // محاولة Migration صامتة من ملف Phase 13 القديم.
                TryMigrateFromLegacy();
            }
            catch (Exception ex)
            {
                // الملف الجديد تالف — نتراجع إلى سجل فارغ بدلاً من إسقاط بدء التشغيل.
                // سجّل في Debug فقط لتسهيل التشخيص في حال وجوده.
                System.Diagnostics.Debug.WriteLine($"[ConnectionRegistry] Load failed: {ex.Message}");
                _state = new RegistryFile();
            }
            finally
            {
                RefreshSnapshotNoLock();
            }
        }
    }

    private void TryMigrateFromLegacy()
    {
        try
        {
            var dir = Path.GetDirectoryName(StoreFilePath);
            if (string.IsNullOrWhiteSpace(dir)) return;

            var legacyPath = Path.Combine(dir, LegacyFileName);
            if (!File.Exists(legacyPath)) return;

            var legacyJson = File.ReadAllText(legacyPath);
            if (string.IsNullOrWhiteSpace(legacyJson)) return;

            var legacy = JsonSerializer.Deserialize<LegacyFile>(legacyJson);
            var cs = legacy?.DefaultConnection;
            if (string.IsNullOrWhiteSpace(cs)) return;

            var entry = new SavedConnection
            {
                Id = Guid.NewGuid(),
                DisplayName = "قاعدة البيانات الأساسية",
                ConnectionString = cs!,
                CreatedAt = DateTime.UtcNow
            };

            _state = new RegistryFile
            {
                ActiveConnectionId = entry.Id,
                Connections = new List<SavedConnection> { entry }
            };

            SaveNoLock();

            // حذف الملف القديم بصمت — الهجرة نجحت أصلاً، فشل الحذف لا يضر.
            try { File.Delete(legacyPath); } catch { /* قد يكون قيد الاستخدام؛ يبقى مهملاً */ }
        }
        catch (Exception ex)
        {
            // أي خطأ في الـ Migration نتجاهله ونبقى بسجل فارغ — مع تسجيل في Debug.
            System.Diagnostics.Debug.WriteLine($"[ConnectionRegistry] Legacy migration failed: {ex.Message}");
        }
    }

    private void Save()
    {
        // يفترض أن يكون المتصل ضمن lock(_gate).
        SaveNoLock();
    }

    private void SaveNoLock()
    {
        var dir = Path.GetDirectoryName(StoreFilePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(StoreFilePath, json);
    }

    // ─── DTOs ───────────────────────────────────────────────────────────────

    private sealed class RegistryFile
    {
        public Guid? ActiveConnectionId { get; set; }
        public List<SavedConnection> Connections { get; set; } = new();
    }

    private sealed class LegacyFile
    {
        public string? DefaultConnection { get; set; }
    }
}
