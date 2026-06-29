using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MSOfficeAuthors.Models;

namespace MSOfficeAuthors.Services
{
    public class AuthorService
    {
        public async Task ReplaceAuthorsAsync(ObservableCollection<AuthorEntry> entries, string from, string to)
        {
            await Task.Run(() =>
            {
                var targets = entries
                    .Where(e => string.Equals(e.OriginalAuthorName, from, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var entry in targets)
                {
                    entry.NewAuthorName = to ?? string.Empty;
                }
            });
        }
    }
}
