# Ofertomator - Copilot Instructions

## Project Type
Avalonia UI Desktop Application with .NET 8

## Technology Stack
- Framework: .NET 8
- UI Framework: Avalonia UI
- Architecture: MVVM with CommunityToolkit.Mvvm
- Database: SQLite (Microsoft.Data.Sqlite + Dapper)
- PDF Generation: QuestPDF
- DI: Microsoft.Extensions.DependencyInjection

## Project Structure
- Models/: Data models and entities
- ViewModels/: MVVM ViewModels with CommunityToolkit.Mvvm
- Views/: Avalonia XAML views
- Services/: Business logic and database services
- Data/: Database context and migrations

## Development Guidelines
- Use decimal for prices (not double)
- All database operations must be async
- Implement graceful error handling
- Follow MVVM pattern strictly
- Use source generators: [ObservableProperty], [RelayCommand]

## Polish Language Support
- UI and comments in Polish
- Support for Polish characters in database (COLLATE NOCASE)
- UTF-8 encoding for all files
