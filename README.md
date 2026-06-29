# MSOfficeAuthors

Профессиональное кроссплатформенное Desktop-приложение для пакетного управления метаданными и авторами документов Microsoft Office.

Разработано на платформе **Avalonia UI** с использованием паттерна **MVVM**, современных стандартов **C# 12** и **.NET 8**.

---

## 🌟 Основные возможности

* **Пакетная обработка:** Одновременная загрузка и обработка неограниченного количества документов Office.
* **Поддержка форматов:** Работа с файлами Word (`.docx`), Excel (`.xlsx`) и PowerPoint (`.pptx`).
* **Массовые действия:**
  * Поиск конкретного автора среди всех документов и пакетная замена на новое имя.
  * Полное удаление (очистка) имен авторов из метаданных всех загруженных файлов в один клик.
* **Автономность:** Редактирование метаданных выполняется напрямую через **OpenXML SDK**. Установка Microsoft Office на компьютере **не требуется**.
* **Премиальный интерфейс:**
  * Современный минималистичный монохромный дизайн с эффектом стеклянных карточек (Glassmorphism).
  * Поддержка двух тем оформления: светлой (в теплых пастельных тонах Quiet Light) и тёмной.
  * Плавные микро-анимации и высокая скорость отклика интерфейса.
  * Полностью асинхронное выполнение операций ввода-вывода (UI-поток никогда не блокируется).

---

## 🏗️ Архитектура и технологии

Проект построен по принципам чистой архитектуры и модульного MVVM:

* **Avalonia UI & SukiUI:** Современный кроссплатформенный UI-фреймворк.
* **CommunityToolkit.Mvvm:** Генерация кода для команд и свойств (Source Generators), реактивное обновление данных.
* **Microsoft.Extensions.DependencyInjection:** Полноценный контейнер внедрения зависимостей (DI) для сервисов и ViewModels.
* **DocumentFormat.OpenXml:** Быстрое и безопасное редактирование структуры документов без запуска тяжелых процессов Office.

### Структура проекта:
* [Models/](file:///D:/AI%20Agent/MSOfficeAuthors/Models) — бизнес-модели данных (записи об авторах [AuthorEntry.cs](file:///D:/AI%20Agent/MSOfficeAuthors/Models/AuthorEntry.cs)).
* [Services/](file:///D:/AI%20Agent/MSOfficeAuthors/Services) — сервисный слой для работы с файловой системой и OpenXML ([OfficeService.cs](file:///D:/AI%20Agent/MSOfficeAuthors/Services/OfficeService.cs), [AuthorService.cs](file:///D:/AI%20Agent/MSOfficeAuthors/Services/AuthorService.cs)).
* [ViewModels/](file:///D:/AI%20Agent/MSOfficeAuthors/ViewModels) — логика управления состоянием представления ([MainViewModel.cs](file:///D:/AI%20Agent/MSOfficeAuthors/ViewModels/MainViewModel.cs)).
* [MainWindow.axaml](file:///D:/AI%20Agent/MSOfficeAuthors/MainWindow.axaml) — декларативная XAML-разметка интерфейса.

---

## 🚀 Как запустить

### Требования
* Установленный [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### Запуск приложения
1. Склонируйте репозиторий:
   ```bash
   git clone https://github.com/ButcchPro/MSOfficeAuthors.git
   cd MSOfficeAuthors
   ```
2. Выполните сборку проекта:
   ```bash
   dotnet build
   ```
3. Запустите приложение:
   ```bash
   dotnet run
   ```
