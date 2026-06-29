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
        private ObservableCollection<AuthorEntry> _entries = new();

        [ObservableProperty]
        private bool _isDarkTheme = true;

        [ObservableProperty]
        private string _themeToggleText = "☀️";

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

        public MainViewModel(MainViewModelServices services)
        {
            _services = services;

            Entries.CollectionChanged += OnEntriesCollectionChanged;
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

                    UpdateStatus($"Загружено файлов: {LoadedFilesCount}. Всего записей: {Entries.Count}");
                    
                    if (errors.Any() && MessageBoxAsync != null)
                    {
                        await MessageBoxAsync("Предупреждение", string.Join("\n", errors.Take(5)) + (errors.Count > 5 ? "\n..." : ""));
                    }
                }
            }
            catch (Exception ex)
            {
                await ReportErrorAsync(ex, "Ошибка при загрузке файлов");
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
                    errors.Add($"В файле '{System.IO.Path.GetFileName(file)}' не найдено авторов.");
                }
                else
                {
                    entries.AddRange(fileAuthors);
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                _services.Logger.LogWarning(ex, "File not found: {FilePath}", file);
                errors.Add($"Файл не найден '{System.IO.Path.GetFileName(file)}'.");
            }
            catch (System.IO.InvalidDataException ex)
            {
                _services.Logger.LogError(ex, "Invalid or corrupted office file: {FilePath}", file);
                errors.Add($"Файл '{System.IO.Path.GetFileName(file)}' поврежден или не является корректным документом Office.");
            }
            catch (System.IO.IOException ex)
            {
                _services.Logger.LogError(ex, "IO Error processing file {FilePath}", file);
                errors.Add($"Ошибка ввода-вывода при работе с файлом '{System.IO.Path.GetFileName(file)}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                _services.Logger.LogError(ex, "Access Denied processing file {FilePath}", file);
                errors.Add($"Ошибка доступа к файлу '{System.IO.Path.GetFileName(file)}': {ex.Message}");
            }
            catch (Exception ex)
            {
                _services.Logger.LogError(ex, "Error processing file {FilePath}", file);
                errors.Add($"Ошибка обработки файла '{System.IO.Path.GetFileName(file)}': {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanClear))]
        private void Clear()
        {
            Entries.Clear();
            UpdateStatus("Список очищен");
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
                    await MessageBoxAsync("Предупреждение", "Изменения сохранены, но при перезагрузке возникли ошибки:\n" + string.Join("\n", errors.Take(5)));
                }
                else if (MessageBoxAsync != null)
                {
                    await MessageBoxAsync("Успех", "Изменения успешно сохранены и файлы перезагружены!");
                }
                
                UpdateStatus("Изменения сохранены");
            }
            catch (Exception ex)
            {
                await ReportErrorAsync(ex, "Ошибка при сохранении");
            }
        }

        private bool CanSave() => Entries.Any();

        [RelayCommand(CanExecute = nameof(CanMassReplace))]
        private void MassReplace()
        {
            if (string.IsNullOrEmpty(MassReplaceFrom)) return;
            
            ReplaceAuthors(MassReplaceFrom, MassReplaceTo);
            
            UpdateStatus($"Заменено для '{MassReplaceFrom}'");
        }

        [RelayCommand(CanExecute = nameof(CanDeleteAll))]
        private void DeleteAll()
        {
            foreach (var entry in Entries)
            {
                entry.NewAuthorName = string.Empty;
            }
            
            UpdateStatus("Все авторы помечены на удаление");
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
                await MessageBoxAsync("Ошибка", message);
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
