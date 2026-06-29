# MSOfficeAuthors

A professional, cross-platform desktop application for batch metadata and author management in Microsoft Office documents.

Built on the **Avalonia UI** framework utilizing the **MVVM** pattern, modern **C# 12** standards, and **.NET 8**.

---

## 🌟 Key Features

* **Batch Processing:** Load and process an unlimited number of Office documents simultaneously.
* **Supported Formats:** Edit Word (`.docx`), Excel (`.xlsx`), and PowerPoint (`.pptx`) files.
* **Mass Actions:**
  * Find a specific author across all loaded documents and replace their name in one click.
  * Completely clear/delete author metadata from all loaded files instantly.
* **Standalone Execution:** Metadata editing is performed directly via the **OpenXML SDK**. Microsoft Office installation on the host machine **is not required**.
* **Premium User Interface:**
  * Modern, minimalist, monochrome design featuring a clean glassmorphism card layout.
  * Support for both Light (warm pastel "Quiet Light" palette) and Dark themes.
  * Smooth micro-animations and highly responsive UI controls.
  * Fully asynchronous I/O operations (the UI thread never freezes).

---

## 🏗️ Architecture & Technologies

The project adheres to Clean Architecture and modular MVVM principles:

* **Avalonia UI & SukiUI:** Modern cross-platform UI framework and theme library.
* **CommunityToolkit.Mvvm:** Code generation for commands and properties (Source Generators) and reactive UI updates.
* **Microsoft.Extensions.DependencyInjection:** Fully-featured dependency injection (DI) container for services and ViewModels.
* **DocumentFormat.OpenXml:** High-performance, safe editing of Office document structures without running heavy Office processes.

### Project Structure:
* [Models/](file:///D:/AI%20Agent/MSOfficeAuthors/Models) — Data models (metadata entries: [AuthorEntry.cs](file:///D:/AI%20Agent/MSOfficeAuthors/Models/AuthorEntry.cs)).
* [Services/](file:///D:/AI%20Agent/MSOfficeAuthors/Services) — Service layer for file operations and OpenXML interactions ([OfficeService.cs](file:///D:/AI%20Agent/MSOfficeAuthors/Services/OfficeService.cs), [AuthorService.cs](file:///D:/AI%20Agent/MSOfficeAuthors/Services/AuthorService.cs)).
* [ViewModels/](file:///D:/AI%20Agent/MSOfficeAuthors/ViewModels) — Presentation state logic ([MainViewModel.cs](file:///D:/AI%20Agent/MSOfficeAuthors/ViewModels/MainViewModel.cs)).
* [MainWindow.axaml](file:///D:/AI%20Agent/MSOfficeAuthors/MainWindow.axaml) — Declarative XAML UI layout.

---

## 🚀 Getting Started

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed on your machine.

### Running the Application
1. Clone the repository:
   ```bash
   git clone https://github.com/ButcchPro/MSOfficeAuthors.git
   cd MSOfficeAuthors
   ```
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
