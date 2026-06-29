using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MSOfficeAuthors.ViewModels;
using SukiUI;
using SukiUI.Controls;

namespace MSOfficeAuthors;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainViewModel vm)
        {
            vm.FilePickerAsync = ShowOpenFileDialogAsync;
            vm.MessageBoxAsync = ShowMessageBoxAsync;
        }
    }

    private async Task<IEnumerable<string>> ShowOpenFileDialogAsync()
    {
        var storage = this.StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите файлы Office",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Office Files") { Patterns = new[] { "*.docx", "*.xlsx", "*.pptx" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
            }
        });

        return files.Select(f => f.Path.LocalPath);
    }

    private Task ShowMessageBoxAsync(string title, string message)
    {
        // For simplicity without external MsBox package, we just use status text or a simple window later
        if (DataContext is MainViewModel vm)
        {
            vm.StatusText = $"{title}: {message}";
        }
        return Task.CompletedTask;
    }
}
