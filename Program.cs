using System;
using Avalonia;

namespace MSOfficeAuthors;

class Program
{
    // Initialization code. Don't use any Avalonia, FreeDesktop, or GPU methods here.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
