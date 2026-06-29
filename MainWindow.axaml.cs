using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MSOfficeAuthors.ViewModels;
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
        viewModel.FilePickerAsync = ShowOpenFileDialogAsync;
        viewModel.MessageBoxAsync = ShowMessageBoxAsync;
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
            FileTypeFilter =
            [
                new FilePickerFileType("Office Files") { Patterns = ["*.docx", "*.xlsx", "*.pptx"] },
                new FilePickerFileType("All files") { Patterns = ["*.*"] }
            ]
        });

        return files?.Select(f => f.Path.LocalPath) ?? System.Linq.Enumerable.Empty<string>();
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
