using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MSOfficeAuthors.Models;

namespace MSOfficeAuthors.Services
{
    /// <summary>
    /// Сервис для обработки и изменения данных авторов в оперативной памяти.
    /// </summary>
    public class AuthorService
    {
        /// <summary>
        /// Производит массовую замену имени автора в коллекции записей.
        /// </summary>
        /// <param name="entries">Коллекция записей авторов для обработки.</param>
        /// <param name="from">Оригинальное имя автора, которое требуется заменить (без учета регистра).</param>
        /// <param name="to">Новое имя автора для замены.</param>
        public void ReplaceAuthors(ObservableCollection<AuthorEntry> entries, string from, string to)
        {
            var targets = entries
                .Where(e => string.Equals(e.OriginalAuthorName, from, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var entry in targets)
            {
                entry.NewAuthorName = to ?? string.Empty;
            }
        }
    }
}
