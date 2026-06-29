using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MSOfficeAuthors.Models;

namespace MSOfficeAuthors.Services
{
    public interface IOfficeService : IDisposable
    {
        Task<List<AuthorEntry>> GetAuthorsAsync(string filePath);
        Task SaveChangesAsync(IEnumerable<AuthorEntry> entries);
    }
}
