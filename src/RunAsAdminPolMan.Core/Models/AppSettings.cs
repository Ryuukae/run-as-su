using System;

namespace RunAsAdminPolMan.Core.Models;

/// <summary>
/// Represents the persistent application settings.
/// </summary>
public record AppSettings
{
    /// <summary>
    /// Gets or sets the default directory where UAC bypass shortcuts will be saved.
    /// Defaults to the user's Desktop.
    /// </summary>
    public string DefaultShortcutLocation { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
}