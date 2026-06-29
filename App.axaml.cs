using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MSOfficeAuthors.Services;
using MSOfficeAuthors.ViewModels;
using System;
using System.IO;

namespace MSOfficeAuthors;

public partial class App : Application
{
    private const string InitializationErrorKey = "InitializationError";

    public new static App? Current => Application.Current as App;
    
    // Use ServiceProvider for faster startup instead of full IHost
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Global exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var logger = _serviceProvider?.GetService<ILogger<App>>();
            if (args.ExceptionObject is Exception ex)
            {
                logger?.LogCritical(ex, "Unhandled exception occurred.");
            }
            else 
            {
                logger?.LogCritical("Unhandled exception object: {ExceptionObject}", args.ExceptionObject);
            }
        };

        // Build the services directly for faster startup
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        ConfigureServices(services);
        
        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        var config = _serviceProvider.GetRequiredService<IConfiguration>();
        var initializationError = config[InitializationErrorKey] ?? "Error during initialization";

        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                ConfigureMainWindow(desktop);
                
                desktop.Exit += async (s, e) =>
                {
                    try
                    {
                        if (_serviceProvider is IAsyncDisposable asyncDisposable)
                        {
                            await asyncDisposable.DisposeAsync();
                        }
                        else if (_serviceProvider is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Try to log the error using the logger if available
                        var exitLogger = _serviceProvider?.GetService<ILogger<App>>();
                        if (exitLogger != null)
                        {
                            exitLogger.LogError(ex, "Error during application shutdown.");
                        }
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, initializationError);
            throw;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        services.AddSingleton<IOfficeService, OfficeService>();
        services.AddSingleton<AuthorService>();
        services.AddTransient<MainViewModelServices>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }

    private void ConfigureMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_serviceProvider == null) return;

        desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
    }
}
