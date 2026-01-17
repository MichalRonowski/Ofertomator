# Ofertomator 2.0

## Opis Projektu

Ofertomator 2.0 to profesjonalna aplikacja desktopowa do kompleksowego zarządzania ofertami handlowymi. System umożliwia import produktów z plików zewnętrznych, kalkulację marż, organizację produktów w kategorie oraz generowanie profesjonalnych ofert w formacie PDF.

## Stos Technologiczny

- **Framework**: .NET 8
- **UI Framework**: Avalonia UI 11.1.3
- **Architektura**: MVVM z CommunityToolkit.Mvvm
- **Baza danych**: SQLite (Microsoft.Data.Sqlite + Dapper)
- **Generowanie PDF**: QuestPDF
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## Struktura Projektu

```
Ofertomator/
├── Models/              # Modele danych
│   ├── Category.cs
│   ├── Product.cs
│   ├── BusinessCard.cs
│   ├── SavedOffer.cs
│   └── SavedOfferItem.cs
├── ViewModels/          # ViewModele (MVVM)
│   ├── ViewModelBase.cs
│   └── MainViewModel.cs
├── Views/               # Widoki Avalonia UI
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
├── Services/            # Serwisy biznesowe
│   └── DatabaseService.cs
├── Assets/              # Zasoby (ikony, obrazy)
├── App.axaml           # Konfiguracja aplikacji
└── Program.cs          # Entry point

```

## Kluczowe Cechy Architektury

### 1. Asynchroniczność
Wszystkie operacje na bazie danych i plikach są wykonywane asynchronicznie (`async/await`), co gwarantuje responsywność UI nawet przy dużych zbiorach danych.

### 2. Graceful Error Handling
System implementuje podejście "graceful degradation" - błędy są obsługiwane bez zamykania aplikacji, z przyjaznymi komunikatami dla użytkownika.

### 3. Optymalizacja Wydajności
- **Paginacja**: Wyświetlanie produktów po 100 sztuk
- **Indeksy bazodanowe**: Optymalizacja wyszukiwania i JOIN'ów
- **Batch processing**: Efektywny import wielu rekordów
- **Debouncing**: Redukcja zbędnych zapytań podczas wpisywania

### 4. Precyzja Finansowa
Wszystkie ceny i wartości finansowe używają typu `decimal` (nie `double`), aby uniknąć błędów zaokrągleń.

### 5. Wsparcie dla Polskich Znaków
- Funkcja `POLISH_LOWER()` w SQLite dla case-insensitive wyszukiwania
- Kodowanie UTF-8 dla wszystkich plików
- `COLLATE NOCASE` dla kolumn tekstowych

## Uruchomienie Projektu

### Wymagania
- .NET 8 SDK lub nowszy
- Windows 10/11, macOS, lub Linux

### Kompilacja i uruchomienie

```bash
# Przejdź do katalogu projektu
cd Ofertomator

# Przywróć pakiety NuGet
dotnet restore

# Kompiluj projekt
dotnet build

# Uruchom aplikację
dotnet run
```

### Publikacja

```bash
# Windows (self-contained)
dotnet publish -c Release -r win-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

## Baza Danych

### Struktura Tabel

#### Categories
- `Id` (INTEGER PRIMARY KEY)
- `Name` (TEXT UNIQUE)
- `DefaultMargin` (REAL)

#### Products
- `Id` (INTEGER PRIMARY KEY)
- `Code` (TEXT, nullable)
- `Name` (TEXT, wymagane)
- `Unit` (TEXT, domyślnie "szt.")
- `PurchasePriceNet` (REAL)
- `PriceUpdateDate` (TEXT - ISO 8601)
- `VatRate` (REAL, domyślnie 23.0)
- `CategoryId` (INTEGER, FK)

#### BusinessCard
- `Id` (INTEGER PRIMARY KEY, zawsze = 1)
- `Company` (TEXT)
- `FullName` (TEXT)
- `Phone` (TEXT)
- `Email` (TEXT)

#### SavedOffers
- `Id` (INTEGER PRIMARY KEY)
- `Title` (TEXT)
- `CreatedDate` (TEXT)
- `ModifiedDate` (TEXT)
- `CategoryOrder` (TEXT - JSON)

#### SavedOfferItems
- `Id` (INTEGER PRIMARY KEY)
- `OfferId` (INTEGER, FK)
- `ProductId` (INTEGER, nullable FK)
- `Name` (TEXT)
- `CategoryName` (TEXT)
- `Unit` (TEXT)
- `PurchasePriceNet` (REAL)
- `VatRate` (REAL)
- `Margin` (REAL)
- `Quantity` (REAL)

### Indeksy
- `idx_products_category` - optymalizacja JOIN'ów
- `idx_products_code` - szybkie wyszukiwanie po kodzie
- `idx_products_name` - wyszukiwanie po nazwie
- `idx_offer_items_offer` - pobieranie pozycji oferty

## Główne Funkcjonalności (Plan Implementacji)

### ✅ KROK 1: Fundamenty (UKOŃCZONE)
- [x] Struktura projektu
- [x] Modele danych
- [x] DatabaseService z pełną obsługą CRUD
- [x] Konfiguracja DI
- [x] MainViewModel i MainWindow

### 🔄 KROK 2: Zarządzanie Produktami (TODO)
- [ ] ProductsViewModel
- [ ] Widok listy produktów z paginacją
- [ ] Dodawanie/Edycja/Usuwanie produktów
- [ ] Wyszukiwanie z debouncing

### 🔄 KROK 3: Import Danych (TODO)
- [ ] ImportService
- [ ] Parsowanie CSV/Excel
- [ ] Mapowanie kolumn
- [ ] Batch import z progress bar

### 🔄 KROK 4: Kategorie (TODO)
- [ ] CategoriesViewModel
- [ ] Zarządzanie kategoriami
- [ ] Przypisywanie produktów do kategorii

### 🔄 KROK 5: Generator Ofert (TODO)
- [ ] OfferGeneratorViewModel
- [ ] Trójkolumnowy layout
- [ ] Dodawanie produktów do oferty
- [ ] Edycja marż

### 🔄 KROK 6: Generowanie PDF (TODO)
- [ ] PdfService (QuestPDF)
- [ ] Template oferty
- [ ] Grupowanie po kategoriach
- [ ] Logo i wizytówka

### 🔄 KROK 7: Zapisane Oferty (TODO)
- [ ] SavedOffersViewModel
- [ ] Zarządzanie szablonami
- [ ] Edycja zapisanych ofert

## Dependency Injection

Serwisy są rejestrowane w `App.axaml.cs`:

```csharp
services.AddSingleton<DatabaseService>();
services.AddTransient<MainViewModel>();
// ... kolejne ViewModele
```

## Wzorce Projektowe

### MVVM (Model-View-ViewModel)
- **Models**: Czyste klasy danych (POCO)
- **ViewModels**: Logika biznesowa i binding, dziedziczą po `ViewModelBase`
- **Views**: Tylko XAML + minimal code-behind

### Repository Pattern
`DatabaseService` działa jako repository, enkapsulując dostęp do danych.

### Dependency Injection
Luźne powiązanie między komponentami, łatwe testowanie.

### Source Generators (CommunityToolkit.Mvvm)
- `[ObservableProperty]` - automatyczna implementacja INotifyPropertyChanged
- `[RelayCommand]` - automatyczne tworzenie ICommand

## Najlepsze Praktyki

### 1. Async/Await
```csharp
// ✅ Poprawnie
await _databaseService.GetProductsPagedAsync();

// ❌ Niepoprawnie (blokuje UI)
_databaseService.GetProductsPagedAsync().Wait();
```

### 2. Decimal dla Cen
```csharp
// ✅ Poprawnie
public decimal Price { get; set; } = 0m;

// ❌ Niepoprawnie (błędy zaokrągleń)
public double Price { get; set; } = 0.0;
```

### 3. Graceful Error Handling
```csharp
try
{
    await _databaseService.AddProductAsync(product);
    StatusMessage = "Produkt dodany";
}
catch (Exception ex)
{
    StatusMessage = $"Błąd: {ex.Message}";
    // Aplikacja NIE crashuje
}
```

### 4. UI Feedback
```csharp
ShowLoading("Importowanie produktów...");
try
{
    await ImportProducts();
}
finally
{
    HideLoading();
}
```

## Rozwój

### Dodawanie Nowego ViewModelu

1. Utwórz klasę dziedziczącą po `ViewModelBase`
2. Użyj `[ObservableProperty]` dla właściwości
3. Użyj `[RelayCommand]` dla komend
4. Zarejestruj w DI (`App.axaml.cs`)

```csharp
public partial class MyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _myProperty = "";

    [RelayCommand]
    private async Task MyCommandAsync()
    {
        // Implementacja
    }
}
```

### Dodawanie Nowego Widoku

1. Utwórz `MyView.axaml` i `MyView.axaml.cs`
2. Ustaw `x:DataType` na odpowiedni ViewModel
3. Użyj bindingów `{Binding PropertyName}`

## Testowanie

```bash
# Uruchom testy jednostkowe (gdy zostaną dodane)
dotnet test
```

## Licencja

Projekt wewnętrzny - wszystkie prawa zastrzeżone.

## Kontakt

W razie pytań, skontaktuj się z zespołem deweloperskim.

---

**Wersja**: 2.0  
**Data**: 17.01.2026  
**Status**: Krok 1 - Fundamenty (UKOŃCZONE) ✅

---

## 📖 Dodatkowa Dokumentacja

Szczegółowa dokumentacja projektu:

- **[QUICKSTART.md](QUICKSTART.md)** - Szybki start dla developerów
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Szczegóły architektury systemu
- **[DEPENDENCIES.md](DEPENDENCIES.md)** - Mapa zależności komponentów
- **[KROK1_SUMMARY.md](KROK1_SUMMARY.md)** - Kompletne podsumowanie KROKU 1
