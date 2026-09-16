using System;
using System.Windows.Media;
using Nasag.Models;

namespace Nasag.Services;

public enum NavigationSection
{
    Dashboard,
    Students,
    Classes,
    Attendance,
    Subjects,
    Exams,
    Marks,
    Results,
    Fees,
    Reports,
    Users,
    Settings,
    Backup
}

public sealed class NavigationDescriptor
{
    public NavigationSection Section { get; }
    public string TitleAr { get; }
    public string IconKey { get; }

    /// <summary>
    /// Permission required to see and access this navigation section in the sidebar.
    /// Null means the section is available to every authenticated user.
    /// </summary>
    public Permission? RequiredPermission { get; }

    public NavigationDescriptor(NavigationSection section, string titleAr, string iconKey, Permission? requiredPermission = null)
    {
        Section = section;
        TitleAr = titleAr;
        IconKey = iconKey;
        RequiredPermission = requiredPermission;
    }
}

public interface INavigationService
{
    NavigationSection Current { get; }
    object? CurrentViewModel { get; }
    event EventHandler? CurrentChanged;

    IReadOnlyList<NavigationDescriptor> Descriptors { get; }

    void NavigateTo(NavigationSection section);
}
