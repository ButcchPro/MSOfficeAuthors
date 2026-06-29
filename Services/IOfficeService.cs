using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MSOfficeAuthors.Models;

namespace MSOfficeAuthors.Services
{
    /// <summary>
    /// Предоставляет методы для работы с авторами и метаданными документов MS Office.
    /// </summary>
    public interface IOfficeService : IDisposable
    {
        /// <summary>
        /// Асинхронно извлекает список авторов (создателей, редакторов, авторов комментариев и ревизий) из указанного файла Office.
        /// </summary>
        /// <param name="filePath">Абсолютный путь к файлу (.docx, .xlsx, .pptx).</param>
        /// <returns>Список найденных авторов в виде коллекции <see cref="AuthorEntry"/>.</returns>
        Task<List<AuthorEntry>> GetAuthorsAsync(string filePath);

        /// <summary>
        /// Асинхронно сохраняет измененные имена авторов обратно в соответствующие документы Office.
        /// </summary>
        /// <param name="entries">Коллекция записей авторов с новыми именами.</param>
        /// <returns>Задачу, представляющую асинхронную операцию сохранения.</returns>
        Task SaveChangesAsync(IEnumerable<AuthorEntry> entries);
    }
}
