namespace RunAsAdminPolMan.Core.Services;

/// <summary>
/// Defines operations for interacting with the native file system dialogs.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Prompts the user to select one or more executable files.
    /// </summary>
    /// <returns>An array of selected file paths, or null if cancelled.</returns>
    string[]? ShowOpenExeDialog();
    /// <summary>
    /// Prompts the user to save a file.
    /// </summary>
    string? ShowSaveFileDialog(string title, string defaultPath, string defaultFileName, string filter);
}