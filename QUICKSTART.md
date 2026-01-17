# Ofertomator 2.0 - Quick Start Guide

## 🚀 Uruchomienie Projektu

### Wymagania
- .NET 8 SDK
- Windows 10/11 (lub macOS/Linux)
- Visual Studio 2022 / VS Code / Rider

### Pierwsze uruchomienie

```powershell
# 1. Przejdź do katalogu projektu
cd Ofertomator

# 2. Przywróć pakiety NuGet
dotnet restore

# 3. Zbuduj projekt
dotnet build

# 4. Uruchom aplikację
dotnet run
```

## 📁 Struktura Projektu

```
Ofertomator/
├── Models/              # 📦 Modele danych (Product, Category, etc.)
├── ViewModels/          # 🎮 Logika biznesowa + binding
├── Views/               # 🎨 Interfejs użytkownika (XAML)
├── Services/            # ⚙️ Serwisy (DatabaseService)
├── Helpers/             # 🔧 Narzędzia pomocnicze
├── Assets/              # 🖼️ Zasoby (ikony, obrazy)
├── App.axaml            # 🏗️ Konfiguracja aplikacji
├── Program.cs           # 🚪 Entry point
└── Ofertomator.csproj   # 📋 Konfiguracja projektu
```

## 🏗️ KROK 1: Fundamenty (✅ UKOŃCZONE)

### Co zostało zrobione?

#### 1. Modele Danych ✅
- ✅ `Category` - kategorie z domyślną marżą
- ✅ `Product` - produkty z cenami (decimal!)
- ✅ `BusinessCard` - wizytówka użytkownika
- ✅ `SavedOffer` - zapisane oferty
- ✅ `SavedOfferItem` - pozycje ofert z kalkulacjami

#### 2. DatabaseService ✅
- ✅ Pełna obsługa CRUD dla wszystkich encji
- ✅ Asynchroniczne operacje (async/await)
- ✅ Paginacja (100 produktów/stronę)
- ✅ Batch import z transakcjami
- ✅ Indeksy dla wydajności
- ✅ Funkcja POLISH_LOWER() dla polskich znaków
- ✅ Graceful error handling

#### 3. ViewModels ✅
- ✅ `ViewModelBase` - bazowa klasa
- ✅ `MainViewModel` - główny ViewModel
- ✅ Wykorzystanie `[ObservableProperty]`
- ✅ Asynchroniczna inicjalizacja

#### 4. Views ✅
- ✅ `MainWindow` - główne okno
- ✅ Menu bar
- ✅ Status bar
- ✅ Loading indicator

#### 5. Dependency Injection ✅
- ✅ Konfiguracja DI w App.axaml.cs
- ✅ DatabaseService jako Singleton
- ✅ ViewModels jako Transient

#### 6. Helpers ✅
- ✅ `DataParser` - parsowanie cen i VAT

## 🧪 Testowanie Fundamentów

### Test 1: Kompilacja
```powershell
dotnet build
# Oczekiwany wynik: "Kompilacja powiodła się"
```

### Test 2: Uruchomienie
```powershell
dotnet run
# Oczekiwany wynik: Okno aplikacji z "Ofertomator 2.0"
```

### Test 3: Inicjalizacja Bazy
Aplikacja automatycznie:
- Tworzy plik `ofertomator.db`
- Inicjalizuje wszystkie tabele
- Dodaje kategorię "Bez kategorii"
- Tworzy pustą wizytówkę

Sprawdź w konsoli komunikat: "Inicjalizacja bazy danych..."

## 📚 Dokumentacja

### Przeczytaj przed rozwojem:
1. **[README.md](README.md)** - Ogólny przegląd projektu
2. **[ARCHITECTURE.md](ARCHITECTURE.md)** - Szczegóły architektury
3. **[opis_ofertomatora.md](../opis_ofertomatora.md)** - Specyfikacja funkcjonalna

## 🔧 Konfiguracja IDE

### Visual Studio Code

Zalecane rozszerzenia:
- C# Dev Kit
- Avalonia for VSCode
- SQLite Viewer

### Rider / Visual Studio
Wszystko działa out-of-the-box ✅

## 🎯 Kolejne Kroki

### KROK 2: Zarządzanie Produktami (TODO)

Następnym krokiem jest implementacja:

1. **ProductsViewModel**
   - Lista produktów z paginacją
   - Wyszukiwanie z debouncing
   - CRUD operations

2. **ProductsView**
   - DataGrid z produktami
   - Formularz dodawania/edycji
   - Przyciski akcji

3. **Testy**
   - Dodanie produktu
   - Wyszukiwanie
   - Paginacja

### Plan rozwoju:
- **Krok 2**: Produkty (1-2 dni)
- **Krok 3**: Import CSV/Excel (1 dzień)
- **Krok 4**: Kategorie (0.5 dnia)
- **Krok 5**: Generator ofert (2-3 dni)
- **Krok 6**: PDF Generation (1-2 dni)
- **Krok 7**: Zapisane oferty (1 dzień)

**Łączny czas**: ~8-10 dni rozwoju

## 🐛 Troubleshooting

### Błąd: "Cannot find .NET SDK"
```powershell
# Sprawdź wersję .NET
dotnet --version
# Powinno zwrócić: 8.0.x lub nowszy
```

### Błąd: "Database locked"
- Zamknij inne instancje aplikacji
- Usuń pliki `ofertomator.db-shm` i `ofertomator.db-wal`

### Błąd kompilacji
```powershell
# Wyczyść cache i przebuduj
dotnet clean
dotnet restore
dotnet build
```

## 💡 Wskazówki dla Developerów

### 1. Dodawanie nowego ViewModelu
```csharp
// 1. Utwórz klasę
public partial class MyViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    
    [ObservableProperty]
    private string _myProperty = "";
    
    [RelayCommand]
    private async Task MyCommandAsync()
    {
        // Implementacja
    }
}

// 2. Zarejestruj w App.axaml.cs
services.AddTransient<MyViewModel>();
```

### 2. Dodawanie nowego Widoku
```xaml
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="Ofertomator.Views.MyView"
             x:DataType="vm:MyViewModel">
    <!-- Zawartość -->
</UserControl>
```

### 3. Async/Await Best Practices
```csharp
// ✅ TAK
private async Task LoadDataAsync()
{
    IsLoading = true;
    try
    {
        var data = await _db.GetDataAsync();
        // Process data
    }
    finally
    {
        IsLoading = false;
    }
}

// ❌ NIE
private void LoadData()
{
    var data = _db.GetDataAsync().Result; // Deadlock risk!
}
```

### 4. Graceful Error Handling
```csharp
try
{
    await _db.SaveAsync(item);
    StatusMessage = "Zapisano pomyślnie";
}
catch (Exception ex)
{
    Console.WriteLine($"Błąd: {ex.Message}");
    StatusMessage = "Nie można zapisać";
    // Aplikacja NIE crashuje!
}
```

## 📊 Metryki Projektu (KROK 1)

- **Pliki kodu**: 15
- **Linie kodu**: ~1500
- **Modele**: 5
- **Serwisy**: 1
- **ViewModele**: 2
- **Widoki**: 1
- **Testy**: 0 (TODO)

## ✅ Checklist - KROK 1

- [x] Struktura projektu utworzona
- [x] Wszystkie modele zaimplementowane
- [x] DatabaseService z pełnym CRUD
- [x] Inicjalizacja bazy danych z indeksami
- [x] ViewModelBase + MainViewModel
- [x] MainWindow z bindingami
- [x] Dependency Injection skonfigurowane
- [x] DataParser helpers
- [x] Projekt kompiluje się bez błędów
- [x] Aplikacja uruchamia się poprawnie
- [x] Dokumentacja napisana (README, ARCHITECTURE)
- [x] .gitignore utworzony

**Status**: KROK 1 UKOŃCZONY ✅

## 🎉 Gratulacje!

Fundamenty aplikacji są gotowe. Kod jest:
- ✅ Czysty i dobrze zorganizowany
- ✅ Asynchroniczny (zero UI freezing)
- ✅ Wydajny (optymalizacje dla dużych danych)
- ✅ Odporny na błędy (graceful degradation)
- ✅ Skalowalny (łatwo dodać nowe funkcje)

**Jesteś gotowy do implementacji KROKU 2!** 🚀

---

**Pytania?** Sprawdź [ARCHITECTURE.md](ARCHITECTURE.md) lub specyfikację funkcjonalną.
