using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using RunAsAdminPolMan.Core.Services;

namespace RunAsAdminPolMan.Services;

/// <summary>
/// A WPF implementation of the file dialog service.
/// </summary>
public class WpfFileDialogService : IFileDialogService
{
    private readonly ILogger<WpfFileDialogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfFileDialogService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public WpfFileDialogService(ILogger<WpfFileDialogService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string[]? ShowOpenExeDialog()
    {
        _logger.LogDebug("Initializing OpenFileDialog for executables.");
        var dialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe)|*.exe",
            Title = "Select Application for RunAsAdmin Policy",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            _logger.LogInformation("User selected {Count} files from dialog.", dialog.FileNames.Length);
            return dialog.FileNames;
        }

        _logger.LogDebug("User canceled OpenFileDialog.");
        return null;
    }
    /// <inheritdoc/>
    public string? ShowSaveFileDialog(string title, string defaultPath, string defaultFileName, string filter)
    {
        _logger.LogInformation("Opening SaveFileDialog: {Title}", title);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            InitialDirectory = defaultPath,
            FileName = defaultFileName,
            Filter = filter
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }
}