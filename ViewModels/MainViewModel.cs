using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSOfficeAuthors.Models;
using MSOfficeAuthors.Services;
using Microsoft.Extensions.Logging;

namespace MSOfficeAuthors.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly MainViewModelServices _services;
        
        [ObservableProperty]
        private ObservableCollection<AuthorEntry> _entries = [];

        [ObservableProperty]
        private bool _isDarkTheme = true;

        [ObservableProperty]
        private string _themeToggleText = "☀️";

        [ObservableProperty]
        private string _currentLanguage = "RU"; // "RU" or "EN"

        [ObservableProperty]
        private string _langToggleText = "RU";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MassReplaceCommand))]
        private string? _massReplaceFrom = string.Empty;

        [ObservableProperty]
        private string _massReplaceTo = string.Empty;

        [ObservableProperty]
        private string _statusText = "Готов";

        public bool IsEmpty => !Entries.Any();

        public List<string> UniqueOriginalAuthors => Entries.Select(e => e.OriginalAuthorName).Distinct().OrderBy(n => n).ToList();

        public Func<Task<IEnumerable<string>>>? FilePickerAsync { get; set; }
        public Func<string, string, Task>? MessageBoxAsync { get; set; }

        public int LoadedFilesCount => Entries.Select(e => e.FilePath).Distinct().Count();

        public string AppVersion
        {
            get
            {
                var version = typeof(MainViewModel).Assembly.GetName().Version;
                return version != null 
                    ? $"v{version.Major}.{version.Minor}.{version.Build}" 
                    : "v1.0.1";
            }
        }

        private bool IsEnglish => CurrentLanguage == "EN";

        #region Localized Strings

        public string L10nSubtitle => IsEnglish 
            ? "Professional manager for Microsoft Office document metadata and authors" 
            : "Профессиональный менеджер метаданных и авторов документов Office";

        public string L10nAddFiles => IsEnglish ? "➕  Add Files" : "➕  Добавить файлы";
        public string L10nSaveChanges => IsEnglish ? "💾  Save Changes" : "💾  Сохранить изменения";
        public string L10nClearList => IsEnglish ? "🧹  Clear List" : "🧹  Очистить список";
        public string L10nMassActions => IsEnglish ? "⚡ Mass Actions" : "⚡ Массовые действия";
        public string L10nFindOriginal => IsEnglish ? "Find original author:" : "Найти исходного автора:";
        public string L10nReplaceWith => IsEnglish ? "Replace with new name:" : "Заменить на новое имя:";
        public string L10nDeleteAll => IsEnglish ? "🗑  Delete All" : "🗑  Удалить все";
        public string L10nReplaceAll => IsEnglish ? "🔄  Replace All" : "🔄  Заменить всё";
        public string L10nHeaderFile => IsEnglish ? "File" : "Файл";
        public string L10nHeaderType => IsEnglish ? "Type" : "Тип";
        public string L10nHeaderOriginal => IsEnglish ? "Original Author" : "Оригинальное имя";
        public string L10nHeaderNew => IsEnglish ? "New Author" : "Новое имя";
        public string L10nNoFiles => IsEnglish ? "No files loaded" : "Нет загруженных файлов";
        public string L10nNoFilesHint => IsEnglish 
            ? "Click the 'Add Files' button to start working with documents" 
            : "Нажмите кнопку 'Добавить файлы', чтобы начать работу с документами";
        
        public string L10nPlaceholderSelect => IsEnglish ? "Select name to replace..." : "Выберите имя для замены...";
        public string L10nWatermarkNewName => IsEnglish ? "Enter new author name..." : "Введите новое имя автора...";
        public string L10nTooltipTheme => IsEnglish ? "Toggle theme" : "Переключить тему оформления";
        public string L10nTooltipLanguage => IsEnglish ? "Toggle language (RU/EN)" : "Переключить язык (RU/EN)";

        #endregion

        public MainViewModel(MainViewModelServices services)
        {
            _services = services;

            Entries.CollectionChanged += OnEntriesCollectionChanged;
            _statusText = IsEnglish ? "Ready" : "Готов";
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            StatusText = message;
            LogMessage(message, isError);
        }

        private void OnEntriesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(UniqueOriginalAuthors));
            OnPropertyChanged(nameof(LoadedFilesCount));
            SaveCommand.NotifyCanExecuteChanged();
            ClearCommand.NotifyCanExecuteChanged();
            DeleteAllCommand.NotifyCanExecuteChanged();
        }

        private IEnumerable<AuthorEntry> GetUniqueEntries(IEnumerable<AuthorEntry> newEntries)
        {
            var existingKeys = Entries
                .Select(e => (e.FilePath, e.Type, e.OriginalAuthorName))
                .ToHashSet();

            return newEntries.Where(author => !existingKeys.Contains((author.FilePath, author.Type, author.OriginalAuthorName)));
        }

        [RelayCommand]
        private void ToggleLanguage()
        {
            CurrentLanguage = CurrentLanguage == "RU" ? "EN" : "RU";
            LangToggleText = CurrentLanguage;
            
            // Notify all localization properties changed
            OnPropertyChanged(nameof(L10nSubtitle));
            OnPropertyChanged(nameof(L10nAddFiles));
            OnPropertyChanged(nameof(L10nSaveChanges));
            OnPropertyChanged(nameof(L10nClearList));
            OnPropertyChanged(nameof(L10nMassActions));
            OnPropertyChanged(nameof(L10nFindOriginal));
            OnPropertyChanged(nameof(L10nReplaceWith));
            OnPropertyChanged(nameof(L10nDeleteAll));
            OnPropertyChanged(nameof(L10nReplaceAll));
            OnPropertyChanged(nameof(L10nHeaderFile));
            OnPropertyChanged(nameof(L10nHeaderType));
            OnPropertyChanged(nameof(L10nHeaderOriginal));
            OnPropertyChanged(nameof(L10nHeaderNew));
            OnPropertyChanged(nameof(L10nNoFiles));
            OnPropertyChanged(nameof(L10nNoFilesHint));
            OnPropertyChanged(nameof(L10nPlaceholderSelect));
            OnPropertyChanged(nameof(L10nWatermarkNewName));
            OnPropertyChanged(nameof(L10nTooltipTheme));
            OnPropertyChanged(nameof(L10nTooltipLanguage));

            // Update status text
            if (StatusText == "Готов" || StatusText == "Ready")
            {
                UpdateStatus(IsEnglish ? "Ready" : "Готов");
            }
            else if (StatusText == "Список очищен" || StatusText == "List cleared")
            {
                UpdateStatus(IsEnglish ? "List cleared" : "Список очищен");
            }
            else if (StatusText == "Изменения сохранены" || StatusText == "Changes saved")
            {
                UpdateStatus(IsEnglish ? "Changes saved" : "Изменения сохранены");
            }
            else if (StatusText == "Все авторы помечены на удаление" || StatusText == "All authors marked for deletion")
            {
                UpdateStatus(IsEnglish ? "All authors marked for deletion" : "Все авторы помечены на удаление");
            }
            else if (StatusText.StartsWith("Загружено файлов") || StatusText.StartsWith("Loaded files"))
            {
                UpdateStatus(IsEnglish 
                    ? $"Loaded files: {LoadedFilesCount}. Total entries: {Entries.Count}" 
                    : $"Загружено файлов: {LoadedFilesCount}. Всего записей: {Entries.Count}");
            }
            else if (StatusText.StartsWith("Заменено для") || StatusText.StartsWith("Replaced for"))
            {
                UpdateStatus(IsEnglish 
                    ? $"Replaced for '{MassReplaceFrom}'" 
                    : $"Заменено для '{MassReplaceFrom}'");
            }
        }

        [RelayCommand]
        private async Task AddFilesAsync()
        {
            if (FilePickerAsync == null) return;

            try
            {
                var files = await FilePickerAsync();

                if (files != null && files.Any())
                {
                    List<string> errors = [];
                    await LoadFilesInternalAsync(files, clearExisting: false, errors);

                    UpdateStatus(IsEnglish 
                        ? $"Loaded files: {LoadedFilesCount}. Total entries: {Entries.Count}" 
                        : $"Загружено файлов: {LoadedFilesCount}. Всего записей: {Entries.Count}");
                    
                    if (errors.Any() && MessageBoxAsync != null)
                    {
                        await MessageBoxAsync(
                            IsEnglish ? "Warning" : "Предупреждение", 
                            string.Join("\n", errors.Take(5)) + (errors.Count > 5 ? "\n..." : ""));
                    }
                }
            }
            catch (Exception ex)
            {
                await ReportErrorAsync(ex, IsEnglish ? "Error loading files" : "Ошибка при загрузке файлов");
            }
        }

        private async Task LoadFilesInternalAsync(IEnumerable<string> filePaths, bool clearExisting, List<string> errors)
        {
            List<AuthorEntry> newEntries = [];
            var filesList = filePaths.Where(file => !string.IsNullOrEmpty(file)).ToList();
            
            foreach (var file in filesList)
            {
                await ProcessFileAsync(file, newEntries, errors);
            }

            if (clearExisting)
            {
                Entries.Clear();
            }

            if (newEntries.Any())
            {
                var entriesToAdd = clearExisting ? newEntries : GetUniqueEntries(newEntries).ToList();
                foreach (var author in entriesToAdd)
                {
                    Entries.Add(author);
                }
            }
        }

        private async Task ProcessFileAsync(string file, List<AuthorEntry> entries, List<string> errors)
        {
            try
            {
                var fileAuthors = await _services.OfficeService.GetAuthorsAsync(file);
                if (fileAuthors == null || !fileAuthors.Any())
                {
                    errors.Add(IsEnglish 
                        ? $"No authors found in file '{System.IO.Path.GetFileName(file)}'." 
                        : $"В файле '{System.IO.Path.GetFileName(file)}' не найдено авторов.");
                }
                else
                {
                    entries.AddRange(fileAuthors);
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                _services.Logger.LogWarning(ex, "File not found: {FilePath}", file);
                errors.Add(IsEnglish 
                    ? $"File not found '{System.IO.Path.GetFileName(file)}'." 
                    : $"Файл не найден '{System.IO.Path.GetFileName(file)}'.");
            }
            catch (System.IO.InvalidDataException ex)
            {
                _services.Logger.LogError(ex, "Invalid or corrupted office file: {FilePath}", file);
                errors.Add(IsEnglish 
                    ? $"File '{System.IO.Path.GetFileName(file)}' is corrupted or not a valid Office document." 
                    : $"Файл '{System.IO.Path.GetFileName(file)}' поврежден или не является корректным документом Office.");
            }
            catch (System.IO.IOException ex)
            {
                _services.Logger.LogError(ex, "IO Error processing file {FilePath}", file);
                errors.Add(IsEnglish 
                    ? $"IO error processing file '{System.IO.Path.GetFileName(file)}': {ex.Message}" 
                    : $"Ошибка ввода-вывода при работе с файлом '{System.IO.Path.GetFileName(file)}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _services.Logger.LogError(ex, "Access Denied processing file {FilePath}", file);
                errors.Add(IsEnglish 
                    ? $"Access denied for file '{System.IO.Path.GetFileName(file)}': {ex.Message}" 
                    : $"Ошибка доступа к файлу '{System.IO.Path.GetFileName(file)}': {ex.Message}");
            }
            catch (Exception ex)
            {
                _services.Logger.LogError(ex, "Error processing file {FilePath}", file);
                errors.Add(IsEnglish 
                    ? $"Error processing file '{System.IO.Path.GetFileName(file)}': {ex.Message}" 
                    : $"Ошибка обработки файла '{System.IO.Path.GetFileName(file)}': {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanClear))]
        private void Clear()
        {
            Entries.Clear();
            UpdateStatus(IsEnglish ? "List cleared" : "Список очищен");
        }

        private bool CanClear() => Entries.Any();

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            try
            {
                var filePaths = Entries.Select(e => e.FilePath).Distinct().ToList();
                
                await _services.OfficeService.SaveChangesAsync(Entries);
                
                // Reload files after saving
                List<string> errors = [];
                await LoadFilesInternalAsync(filePaths, clearExisting: true, errors);
                
                if (errors.Any() && MessageBoxAsync != null)
                {
                    await MessageBoxAsync(
                        IsEnglish ? "Warning" : "Предупреждение", 
                        IsEnglish 
                            ? "Changes saved, but errors occurred during reload:\n" + string.Join("\n", errors.Take(5))
                            : "Изменения сохранены, но при перезагрузке возникли ошибки:\n" + string.Join("\n", errors.Take(5)));
                }
                else if (MessageBoxAsync != null)
                {
                    await MessageBoxAsync(
                        IsEnglish ? "Success" : "Успех", 
                        IsEnglish 
                            ? "Changes successfully saved and files reloaded!" 
                            : "Изменения успешно сохранены и файлы перезагружены!");
                }
                
                UpdateStatus(IsEnglish ? "Changes saved" : "Изменения сохранены");
            }
            catch (Exception ex)
            {
                await ReportErrorAsync(ex, IsEnglish ? "Error saving" : "Ошибка при сохранении");
            }
        }

        private bool CanSave() => Entries.Any();

        [RelayCommand(CanExecute = nameof(CanMassReplace))]
        private void MassReplace()
        {
            if (string.IsNullOrEmpty(MassReplaceFrom)) return;
            
            ReplaceAuthors(MassReplaceFrom, MassReplaceTo);
            
            UpdateStatus(IsEnglish ? $"Replaced for '{MassReplaceFrom}'" : $"Заменено для '{MassReplaceFrom}'");
        }

        [RelayCommand(CanExecute = nameof(CanDeleteAll))]
        private void DeleteAll()
        {
            foreach (var entry in Entries)
            {
                entry.NewAuthorName = string.Empty;
            }
            
            UpdateStatus(IsEnglish ? "All authors marked for deletion" : "Все авторы помечены на удаление");
        }

        private bool CanDeleteAll() => Entries.Any();

        private void ReplaceAuthors(string from, string to)
        {
            _services.AuthorService.ReplaceAuthors(Entries, from, to);
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            if (Avalonia.Application.Current != null)
            {
                IsDarkTheme = !IsDarkTheme;
                if (IsDarkTheme)
                {
                    ThemeToggleText = "☀️";
                    Avalonia.Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                }
                else
                {
                    ThemeToggleText = "🌙";
                    Avalonia.Application.Current.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                }
            }
        }

        private async Task ReportErrorAsync(Exception ex, string context, List<string>? errors = null, bool showMessageBox = true)
        {
            var message = $"{context}: {ex.Message}";
            _services.Logger.LogError(ex, "{ErrorContext}", context);
            errors?.Add(message);

            if (showMessageBox && MessageBoxAsync != null)
            {
                await MessageBoxAsync(IsEnglish ? "Error" : "Ошибка", message);
            }
            
            LogMessage(context, true);
        }

        private void LogMessage(string message, bool isError)
        {
            if (isError)
                _services.Logger.LogError("{Context}: {Message}", nameof(MainViewModel), message);
            else
                _services.Logger.LogInformation("{Context}: {Message}", nameof(MainViewModel), message);
        }

        private bool CanMassReplace() => !string.IsNullOrEmpty(MassReplaceFrom);

        partial void OnEntriesChanged(ObservableCollection<AuthorEntry> value)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }
}
