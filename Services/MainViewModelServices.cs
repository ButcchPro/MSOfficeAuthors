using Microsoft.Extensions.Logging;
using MSOfficeAuthors.ViewModels;

namespace MSOfficeAuthors.Services
{
    public class MainViewModelServices
    {
        public IOfficeService OfficeService { get; }
        public AuthorService AuthorService { get; }
        public ILogger<MainViewModel> Logger { get; }

        public MainViewModelServices(IOfficeService officeService, AuthorService authorService, ILogger<MainViewModel> logger)
        {
            OfficeService = officeService ?? throw new System.ArgumentNullException(nameof(officeService));
            AuthorService = authorService ?? throw new System.ArgumentNullException(nameof(authorService));
            Logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            Logger.LogInformation("MainViewModelServices initialized.");
        }
    }
}
