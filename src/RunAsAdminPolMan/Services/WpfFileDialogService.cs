using Microsoft.Win32;

using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Services;

/// <summary>
/// A WPF implementation of the file dialog service.
/// </summary>
public class WpfFileDialogService : IFileDialogService
{
    /// <inheritdoc/>
    public string[]? ShowOpenExeDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe",
            Title = "Select Application for RunAsAdmin Policy",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            return dialog.FileNames;
        }

        return null;
    }
}