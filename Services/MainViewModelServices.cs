using Microsoft.Extensions.Logging;
using MSOfficeAuthors.ViewModels;

namespace MSOfficeAuthors.Services
{
    public class MainViewModelServices(IOfficeService officeService, AuthorService authorService, ILogger<MainViewModel> logger)
    {
        public IOfficeService OfficeService { get; } = officeService ?? throw new System.ArgumentNullException(nameof(officeService));
        public AuthorService AuthorService { get; } = authorService ?? throw new System.ArgumentNullException(nameof(authorService));
        public ILogger<MainViewModel> Logger { get; } = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }
}
