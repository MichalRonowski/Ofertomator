# Architektura Ofertomatora 2.0

## Przegląd

Ofertomator 2.0 jest zbudowany w oparciu o solidne fundamenty architektoniczne, które zapewniają:
- ✅ **Zero UI Freezing** - wszystkie operacje asynchroniczne
- ✅ **Graceful Degradation** - odporna obsługa błędów
- ✅ **Precision** - decimal dla wszystkich operacji finansowych
- ✅ **Scalability** - optymalizacje dla dużych zbiorów danych
- ✅ **Maintainability** - czysty kod MVVM + DI

## Warstwy Aplikacji

```
┌─────────────────────────────────────┐
│         PRESENTATION LAYER          │
│     (Views + ViewModels)            │
├─────────────────────────────────────┤
│         BUSINESS LOGIC LAYER        │
│           (Services)                │
├─────────────────────────────────────┤
│          DATA ACCESS LAYER          │
│       (DatabaseService)             │
├─────────────────────────────────────┤
│            DATA LAYER               │
│      (SQLite Database)              │
└─────────────────────────────────────┘
```

## Komponenty Systemu

### 1. Models (Warstwa Danych)
**Odpowiedzialność**: Definicja struktur danych

**Klasy**:
- `Category` - kategorie produktów z domyślną marżą
- `Product` - produkty z cenami i atrybutami
- `BusinessCard` - dane kontaktowe użytkownika
- `SavedOffer` - zapisane oferty (szablony)
- `SavedOfferItem` - pozycje w ofercie z kalkulacjami

**Kluczowe decyzje**:
- Wszystkie ceny jako `decimal` (precyzja finansowa)
- Calculated properties w `SavedOfferItem` (DRY principle)
- Nullable `ProductId` w `SavedOfferItem` (odporność na usunięcie produktu)

### 2. Services (Warstwa Logiki Biznesowej)

#### DatabaseService
**Odpowiedzialność**: Zarządzanie dostępem do bazy danych

**Kluczowe funkcje**:
- Asynchroniczne operacje CRUD dla wszystkich encji
- Paginacja dla wydajności (max 100 rekordów na stronę)
- Batch processing dla importu danych
- Funkcja `POLISH_LOWER()` dla wsparcia polskich znaków
- Indeksy dla optymalizacji wyszukiwania

**Optymalizacje**:
```csharp
// Paginacja
public async Task<(IEnumerable<Product>, int)> GetProductsPagedAsync(
    int pageNumber, int pageSize, string? searchQuery)

// Batch import z transakcjami
public async Task<(int Added, int Updated)> ImportProductsBatchAsync(
    IEnumerable<Product> products, bool updateExisting)

// Case-insensitive search z polskimi znakami
WHERE POLISH_LOWER(p.Name) LIKE @Search
```

**Obsługa błędów**:
```csharp
try
{
    // Operacja na bazie danych
}
catch (Exception ex)
{
    Console.WriteLine($"Błąd: {ex.Message}");
    return defaultValue; // Graceful degradation
}
```

### 3. ViewModels (Warstwa Prezentacji - Logika)

#### ViewModelBase
**Odpowiedzialność**: Bazowa klasa dla wszystkich ViewModeli

**Cechy**:
- Dziedziczy po `ObservableObject` (CommunityToolkit.Mvvm)
- Automatyczna implementacja `INotifyPropertyChanged`
- Podstawa dla wszystkich ViewModeli

#### MainViewModel
**Odpowiedzialność**: Główny ViewModel aplikacji

**Kluczowe funkcje**:
- Zarządzanie stanem aplikacji (loading, status)
- Inicjalizacja asynchroniczna bazy danych
- Nawigacja między widokami (future)

**Użycie Source Generators**:
```csharp
[ObservableProperty]
private string _statusMessage = "Gotowy";
// Generuje: StatusMessage property + OnStatusMessageChanged

[RelayCommand]
private async Task DoSomethingAsync()
// Generuje: DoSomethingCommand : IAsyncRelayCommand
```

### 4. Views (Warstwa Prezentacji - UI)

#### MainWindow
**Odpowiedzialność**: Główne okno aplikacji

**Struktura**:
- Menu Bar (nawigacja)
- Content Area (dynamiczny content)
- Status Bar (komunikaty dla użytkownika)

**Binding**:
```xaml
<TextBlock Text="{Binding StatusMessage}" />
<ProgressBar IsIndeterminate="{Binding IsLoading}" />
```

### 5. Helpers (Narzędzia)

#### DataParser
**Odpowiedzialność**: Parsowanie danych z różnych formatów

**Funkcje**:
- `ParsePrice()` - konwersja "12,50" lub "12.50" → 12.50m
- `ParseVatRate()` - konwersja "23%", "23", "0.23" → 23m
- `FormatPrice()` - formatowanie do wyświetlania
- `FormatPercent()` - formatowanie procentów

## Dependency Injection

### Konfiguracja (App.axaml.cs)

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Singleton - jedna instancja dla całej aplikacji
    services.AddSingleton<DatabaseService>();
    
    // Transient - nowa instancja przy każdym resolve
    services.AddTransient<MainViewModel>();
}
```

### Zalety DI
1. **Testability** - łatwa podmiana implementacji
2. **Loose Coupling** - komponenty nie znają szczegółów implementacji
3. **Lifetime Management** - automatyczne zarządzanie cyklem życia
4. **Dependency Resolution** - automatyczne wstrzykiwanie zależności

### Użycie
```csharp
public MainViewModel(DatabaseService databaseService)
{
    _databaseService = databaseService; // Automatycznie wstrzyknięte
}
```

## Baza Danych

### Struktura

#### Tabele
- **Categories** - kategorie produktów
- **Products** - produkty z cenami
- **BusinessCard** - wizytówka (singleton)
- **SavedOffers** - zapisane oferty
- **SavedOfferItems** - pozycje ofert

#### Indeksy (Optymalizacja)
```sql
CREATE INDEX idx_products_category ON Products(CategoryId);
CREATE INDEX idx_products_code ON Products(Code);
CREATE INDEX idx_products_name ON Products(Name COLLATE NOCASE);
CREATE INDEX idx_offer_items_offer ON SavedOfferItems(OfferId);
```

#### Funkcje Custom
```sql
-- Case-insensitive dla polskich znaków
POLISH_LOWER(text) → ToLowerInvariant()
```

#### Foreign Keys + Cascades
```sql
-- Usunięcie oferty usuwa wszystkie pozycje
FOREIGN KEY (OfferId) REFERENCES SavedOffers(Id) ON DELETE CASCADE

-- Usunięcie produktu ustawia NULL w pozycjach (zachowanie snapshota)
FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE SET NULL
```

### Optymalizacje Wydajności

#### 1. Paginacja
**Problem**: Wyświetlanie 1000+ produktów zamraża UI

**Rozwiązanie**:
```csharp
SELECT * FROM Products 
ORDER BY Name 
LIMIT 100 OFFSET 0  -- Strona 1

// Następna strona
LIMIT 100 OFFSET 100  -- Strona 2
```

**Efekt**: Stały czas ładowania niezależnie od rozmiaru bazy

#### 2. Indeksy
**Problem**: Wolne wyszukiwanie w dużych tabelach

**Rozwiązanie**:
```sql
CREATE INDEX idx_products_name ON Products(Name COLLATE NOCASE);
```

**Efekt**: Wyszukiwanie O(log n) zamiast O(n)

#### 3. Batch Operations
**Problem**: Import 1000 produktów = 1000 INSERT'ów = powolne

**Rozwiązanie**:
```csharp
using var transaction = connection.BeginTransaction();
foreach (var product in products)
{
    await connection.ExecuteAsync(insertSql, product, transaction);
}
transaction.Commit();
```

**Efekt**: Wszystkie operacje w jednej transakcji = 10x szybciej

#### 4. Eager Loading (JOIN)
**Problem**: N+1 queries (pobierz produkty, potem dla każdego kategorię)

**Rozwiązanie**:
```csharp
SELECT p.*, c.* 
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.Id
```

**Efekt**: Jedna kwerenda zamiast N+1

## Wzorce Projektowe

### 1. MVVM (Model-View-ViewModel)
**Cel**: Separacja logiki biznesowej od UI

**Implementacja**:
- **Model**: Czyste dane (Product, Category)
- **View**: XAML + minimal code-behind
- **ViewModel**: Logika + binding properties

### 2. Repository Pattern
**Cel**: Abstrakcja dostępu do danych

**Implementacja**: `DatabaseService` jako repository

### 3. Dependency Injection
**Cel**: Loose coupling, testability

**Implementacja**: Microsoft.Extensions.DependencyInjection

### 4. Async/Await
**Cel**: Responsywność UI

**Implementacja**: Wszystkie operacje I/O są async

### 5. Source Generators
**Cel**: Redukcja boilerplate code

**Implementacja**: `[ObservableProperty]`, `[RelayCommand]`

## Asynchroniczność

### Dlaczego async/await?

**Problem**:
```csharp
// ❌ Synchroniczne - zamraża UI
var products = _db.GetProducts(); // UI czeka...
```

**Rozwiązanie**:
```csharp
// ✅ Asynchroniczne - UI responsywne
var products = await _db.GetProductsAsync(); // UI działa dalej
```

### Zasady

1. **Wszystkie operacje I/O jako async**
   - Baza danych
   - Pliki
   - Sieć

2. **Propagacja async przez cały stack**
   ```csharp
   Service.MethodAsync() 
   → ViewModel.CommandAsync() 
   → View (binding)
   ```

3. **Nie blokuj async**
   ```csharp
   // ❌ NIE
   var result = Task.Result; // Deadlock risk!
   
   // ✅ TAK
   var result = await Task;
   ```

4. **ConfigureAwait w bibliotekach** (nie w UI)
   ```csharp
   // W serwisach (optional)
   await task.ConfigureAwait(false);
   ```

## Obsługa Błędów

### Strategia: Graceful Degradation

**Cel**: Aplikacja NIE crashuje, nawet przy błędach

**Implementacja**:
```csharp
try
{
    await _db.DeleteProductAsync(id);
    StatusMessage = "Produkt usunięty";
}
catch (Exception ex)
{
    Console.WriteLine($"Błąd: {ex.Message}"); // Logging
    StatusMessage = "Nie można usunąć produktu"; // User-friendly
    // Aplikacja działa dalej!
}
```

### Poziomy Obsługi

1. **Service Layer**
   - Try-catch wokół operacji DB
   - Return default values on error
   - Log to console

2. **ViewModel Layer**
   - Try-catch w commands
   - Update status message
   - Show user-friendly errors

3. **View Layer**
   - Binding do error messages
   - Visual feedback (colors, icons)

## Wydajność - Thresholds

System automatycznie dostosowuje zachowanie:

| Operacja | Próg | Optymalizacja |
|----------|------|---------------|
| Lista produktów | Zawsze | Paginacja 100/strona |
| Wyszukiwanie | > 200 ms | Debouncing 300ms |
| Widok kategorii | > 200 produktów | Limit wyświetlania |
| Import batch | > 50 produktów | Transakcje + progress |
| Generowanie PDF | > 200 produktów | Threading + progress |

## Rozwój - Następne Kroki

### Priorytet 1: Zarządzanie Produktami
- [ ] ProductsViewModel z CRUD
- [ ] Widok listy z paginacją
- [ ] Wyszukiwanie z debouncing

### Priorytet 2: Import Danych
- [ ] ImportService (CSV/Excel)
- [ ] Mapowanie kolumn
- [ ] Progress bar

### Priorytet 3: Kategorie
- [ ] CategoriesViewModel
- [ ] Zarządzanie marżami
- [ ] Przypisywanie produktów

### Priorytet 4: Generator Ofert
- [ ] OfferGeneratorViewModel
- [ ] Trójkolumnowy layout
- [ ] Kalkulator marż

### Priorytet 5: PDF Generation
- [ ] PdfService (QuestPDF)
- [ ] Template design
- [ ] Logo + watermark

## Kluczowe Decyzje Architektoniczne

### 1. Decimal zamiast Double
**Dlaczego**: Precyzja finansowa
```csharp
double price = 0.1 + 0.2; // = 0.30000000000000004 ❌
decimal price = 0.1m + 0.2m; // = 0.3 ✅
```

### 2. Snapshot w SavedOfferItem
**Dlaczego**: Oferta nie zmienia się po usunięciu produktu z bazy
```csharp
public int? ProductId { get; set; } // Nullable!
public string Name { get; set; } // Snapshot nazwy
public decimal PurchasePriceNet { get; set; } // Snapshot ceny
```

### 3. Paginacja zamiast Virtualization
**Dlaczego**: Prostsze + równie wydajne dla naszego use case
- Avalonia TreeDataGrid ma virtualization, ale paginacja jest wystarczająca
- 100 produktów na stronę = instant loading

### 4. Dapper zamiast EF Core
**Dlaczego**: Kontrola + wydajność
- Pełna kontrola nad SQL
- Brak overhead EF
- Idealne dla prostego schematu

### 5. Avalonia zamiast WPF/MAUI
**Dlaczego**: Cross-platform + nowoczesność
- Windows + macOS + Linux
- Aktywny rozwój
- Lepsze performance niż WPF

## Podsumowanie

Architektura Ofertomatora 2.0 została zaprojektowana z myślą o:

✅ **Wydajności** - optymalizacje dla dużych zbiorów danych  
✅ **Niezawodności** - graceful error handling  
✅ **Precyzji** - decimal dla finansów  
✅ **Utrzymywalności** - czysty kod, wzorce projektowe  
✅ **Rozwoju** - łatwe dodawanie nowych funkcji  

**Status**: Fundamenty gotowe ✅  
**Następny krok**: Implementacja zarządzania produktami
