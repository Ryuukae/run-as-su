using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Extensions.Logging;

namespace RunAsAdminPolMan;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);

    private const uint WM_DROPFILES = 0x0233;
    private const uint WM_COPYDATA = 0x004A;
    private const uint WM_COPYGLOBALDATA = 0x0049;
    private const uint MSGFLT_ADD = 1;

    private readonly Microsoft.Extensions.Logging.ILogger<MainWindow> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow(RunAsAdminPolMan.ViewModels.MainViewModel viewModel, Microsoft.Extensions.Logging.ILogger<MainWindow> logger)
    {
        InitializeComponent();
        DataContext = viewModel;
        _logger = logger;

        // Auto-load policies on startup securely through the IAsyncRelayCommand
        Loaded += async (s, e) =>
        {
            _logger.LogInformation("MainWindow loaded. Bypassing UIPI for Drag-and-Drop and loading policies.");
            // CRITICAL WIN32 FIX: Bypass User Interface Privilege Isolation (UIPI) firewall.
            // Since this app runs Elevated, Windows physically blocks Drag-and-Drop from Explorer.
            ChangeWindowMessageFilter(WM_DROPFILES, MSGFLT_ADD);
            ChangeWindowMessageFilter(WM_COPYDATA, MSGFLT_ADD);
            ChangeWindowMessageFilter(WM_COPYGLOBALDATA, MSGFLT_ADD);

            await viewModel.LoadPoliciesCommand.ExecuteAsync(null);
        };
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not RunAsAdminPolMan.ViewModels.MainViewModel vm) return;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var exeFiles = System.Linq.Enumerable.Where(files, f => f.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase)).ToArray();

            if (exeFiles.Length > 0)
            {
                _logger.LogInformation("Drag-and-Drop triggered. Executing AddPoliciesCommand for {Count} executable(s).", exeFiles.Length);
                await vm.AddPoliciesCommand.ExecuteAsync(exeFiles);
            }
            else
            {
                _logger.LogWarning("Drag-and-Drop triggered, but no valid executable files were found in the payload.");
            }
        }
    }

    /// <summary>
    /// Handles the click event for the custom minimize button.
    /// </summary>
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogTrace("User minimized the window via custom WindowChrome.");
        WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Handles the click event for the custom maximize/restore button.
    /// </summary>
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogTrace("User toggled window maximization state via custom WindowChrome.");
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    /// <summary>
    /// Handles the click event for the custom close button.
    /// </summary>
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("User clicked the custom close button. Shutting down application.");
        Close();
    }
}