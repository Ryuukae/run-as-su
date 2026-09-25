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

namespace RunAsAdminPolMan;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow(RunAsAdminPolMan.ViewModels.MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Auto-load policies on startup securely through the IAsyncRelayCommand
        Loaded += async (s, e) => await viewModel.LoadPoliciesCommand.ExecuteAsync(null);
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
                await vm.AddPoliciesCommand.ExecuteAsync(exeFiles);
            }
        }
    }
}