using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nasag.Models;

namespace Nasag.Services;

/// <summary>
/// Persistence boundary for the "School Settings" page (Phase 12).
/// Returns/saves the singleton <see cref="SchoolSettings"/> row and manages the list
/// of academic years displayed on the same screen.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Returns the singleton settings row, creating a default
    /// (NameAr = "مدرسة النور الأهلية") if none exists.
    /// </summary>
    Task<SchoolSettings> GetOrCreateAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all academic years ordered by start date (newest first).
    /// </summary>
    Task<IReadOnlyList<AcademicYear>> GetAcademicYearsAsync(CancellationToken ct = default);

    /// <summary>
    /// Copies editable fields from <paramref name="settings"/> onto the persisted row
    /// and saves the changes. Requires <see cref="Permission.ManageSettings"/>.
    /// </summary>
    Task SaveAsync(SchoolSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Creates a new academic year. Requires <see cref="Permission.ManageSettings"/>.
    /// </summary>
    Task<AcademicYear> CreateAcademicYearAsync(string nameAr, DateTime start, DateTime end, CancellationToken ct = default);

    /// <summary>
    /// Updates the school's <see cref="SchoolSettings.CurrentAcademicYearId"/>.
    /// Requires <see cref="Permission.ManageSettings"/>.
    /// </summary>
    Task SetCurrentAcademicYearAsync(int yearId, CancellationToken ct = default);

    /// <summary>
    /// Deletes an academic year. Refuses if the year is the current one or if any
    /// Section/Exam/FeePlan still references it (throws <see cref="InvalidOperationException"/>
    /// with an Arabic message in that case). Requires <see cref="Permission.ManageSettings"/>.
    /// </summary>
    Task DeleteAcademicYearAsync(int yearId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing academic year's NameAr / dates / IsActive.
    /// Requires <see cref="Permission.ManageSettings"/>.
    /// </summary>
    Task UpdateAcademicYearAsync(int yearId, string nameAr, DateTime start, DateTime end, CancellationToken ct = default);
}
