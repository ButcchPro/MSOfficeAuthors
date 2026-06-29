using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSOfficeAuthors.ViewModels;

namespace MSOfficeAuthors.Services
{
    /// <summary>
    /// Методы расширения для регистрации зависимостей в DI-контейнере.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует инфраструктурные сервисы (логирование, работу с файлами и авторами).
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            services.AddSingleton<IOfficeService, OfficeService>();
            services.AddSingleton<AuthorService>();

            return services;
        }

        /// <summary>
        /// Регистрирует компоненты слоя представления (ViewModels, Views и вспомогательные контейнеры).
        /// </summary>
        public static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            services.AddTransient<MainViewModelServices>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();

            return services;
        }
    }
}
