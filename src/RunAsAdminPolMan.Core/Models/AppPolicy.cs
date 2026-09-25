namespace RunAsAdminPolMan.Core.Models;

/// <summary>
/// Represents a RunAsAdmin registry policy for a specific application.
/// </summary>
public record AppPolicy
{
    /// <summary>
    /// Gets the absolute file path to the executable. This serves as the unique identifier.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the extracted product name of the application.
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the extracted publisher name of the application.
    /// </summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>
    /// Gets the raw bytes of the application's icon, for rendering in the UI.
    /// </summary>
    public byte[]? IconBytes { get; init; }

    /// <summary>
    /// Gets a value indicating whether the RunAsAdmin flag is currently enabled (~ RUNASADMIN) or disabled (-RUNASADMIN).
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Gets the registry scope where this policy resides.
    /// </summary>
    public PolicyScope Scope { get; init; }
    /// <summary>
    /// Gets the arguments to pass to the executable.
    /// </summary>
    public string Arguments { get; init; } = string.Empty;
}