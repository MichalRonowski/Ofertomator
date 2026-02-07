using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Ofertomator.Models;
using Ofertomator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla generatora ofert - trójkolumnowy widok
/// Lewa: Kategorie | Środkowa: Produkty | Prawa: Oferta/Koszyk
/// </summary>
public partial class OfferGeneratorViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly IPdfService _pdfService;
    private readonly Func<Window?> _getMainWindow;

    // Debouncing dla UpdateOfferSummary
    private Timer? _updateSummaryDebounceTimer;
    private const int SummaryDebounceDelayMs = 300;
    private bool _suppressNotifications = false;

    // Cache dla grupowania kategorii (Etap 3) + zapamiętywanie stanów IsExpanded
    private ObservableCollection<CategoryGroup>? _cachedGroupedItems;
    private int _lastCachedVersion = -1;
    private int _currentCollectionVersion = 0;
    private Dictionary<string, bool> _categoryExpandedStates = new();

    #region Observable Properties

    /// <summary>
    /// Lista wszystkich kategorii (lewa kolumna)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    /// <summary>
    /// Wybrana kategoria (highlight w lewej kolumnie)
    /// </summary>
    [ObservableProperty]
    private Category? _selectedCategory;

    /// <summary>
    /// Produkty do wyświetlenia w środkowej kolumnie
    /// Filtrowane: tylko z wybranej kategorii + bez produktów już w ofercie
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Product> _sourceProducts = new();

    /// <summary>
    /// Wszystkie produkty z wybranej kategorii (przed filtrowaniem)
    /// </summary>
    private List<Product> _allProductsInCategory = new();

    /// <summary>
    /// Query wyszukiwania dla środkowej kolumny (lokalny filtr)
    /// </summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Produkty dodane do oferty (prawa kolumna - koszyk)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SavedOfferItem> _offerItems = new();

    /// <summary>
    /// Wybrany produkt w środkowej kolumnie
    /// </summary>
    [ObservableProperty]
    private Product? _selectedSourceProduct;

    /// <summary>
    /// Wybrany item w ofercie (prawa kolumna)
    /// </summary>
    [ObservableProperty]
    private SavedOfferItem? _selectedOfferItem;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";


    
    /// <summary>
    /// Nowa marża do zastosowania dla zaznaczonych produktów
    /// </summary>
    [ObservableProperty]
    private decimal _newMarginForSelected = 0m;

    /// <summary>
    /// Nazwa oferty (domyślnie "Oferta handlowa")
    /// </summary>
    [ObservableProperty]
    private string _offerName = "Oferta handlowa";

    #endregion

    #region Computed Properties - Podsumowanie Oferty

    /// <summary>
    /// Suma netto całej oferty
    /// </summary>
    public decimal TotalOfferNet => OfferItems.Sum(item => item.TotalNet);

    /// <summary>
    /// Suma VAT całej oferty
    /// </summary>
    public decimal TotalOfferVat => OfferItems.Sum(item => item.VatAmount);

    /// <summary>
    /// Suma brutto całej oferty
    /// </summary>
    public decimal TotalOfferGross => OfferItems.Sum(item => item.TotalGross);

    /// <summary>
    /// Liczba pozycji w ofercie
    /// </summary>
    public int OfferItemsCount => OfferItems.Count;

    /// <summary>
    /// Info: "Pozycji w ofercie: X"
    /// </summary>
    public string OfferItemsInfo => $"Pozycji w ofercie: {OfferItemsCount}";

    /// <summary>
    /// Produkty w ofercie pogrupowane według kategorii (sortowane według własnej kolejności lub DisplayOrder)
    /// OPTYMALIZACJA: Cachowane - przelicza tylko gdy zmienia się struktura kolekcji
    /// ZAPAMIĘTUJE stany IsExpanded dla każdej kategorii
    /// </summary>
    public ObservableCollection<CategoryGroup> OfferItemsGroupedByCategory
    {
        get
        {
            // Użyj cache jeśli wersja się nie zmieniła
            if (_cachedGroupedItems != null && _lastCachedVersion == _currentCollectionVersion)
            {
                return _cachedGroupedItems;
            }

            // Zapisz obecne stany IsExpanded przed przeliczeniem
            if (_cachedGroupedItems != null)
            {
                foreach (var group in _cachedGroupedItems)
                {
                    _categoryExpandedStates[group.CategoryName] = group.IsExpanded;
                }
            }

            // Przelicz i zapisz w cache
            var grouped = OfferItems
                .GroupBy(item => item.CategoryName ?? "Bez kategorii")
                .ToList();

            // Sortuj według DisplayOrder z Categories
            var categoryDict = Categories.ToDictionary(c => c.Name, c => c.DisplayOrder);
            var sortedGroups = grouped.OrderBy(g => categoryDict.TryGetValue(g.Key, out var order) ? order : 9999);

            // Konwertuj na CategoryGroup z zachowanymi stanami IsExpanded
            _cachedGroupedItems = new ObservableCollection<CategoryGroup>();
            foreach (var group in sortedGroups)
            {
                var categoryGroup = new CategoryGroup
                {
                    CategoryName = group.Key,
                    Items = group.ToList(),
                    // Przywróć poprzedni stan lub domyślnie rozwinięty
                    IsExpanded = _categoryExpandedStates.TryGetValue(group.Key, out var isExpanded) ? isExpanded : true
                };
                _cachedGroupedItems.Add(categoryGroup);
            }

            _lastCachedVersion = _currentCollectionVersion;
            return _cachedGroupedItems;
        }
    }

    #endregion

    #region Constructor

    public OfferGeneratorViewModel(DatabaseService databaseService, IPdfService pdfService, Func<Window?> getMainWindow)
    {
        _databaseService = databaseService;
        _pdfService = pdfService;
        _getMainWindow = getMainWindow;

        // Subskrybuj zmiany w kolekcji OfferItems
        OfferItems.CollectionChanged += OnOfferItemsCollectionChanged;

        // Inicjalizacja
        _ = InitializeAsync();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Inicjalizacja ViewModelu - ładowanie kategorii
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Ładowanie kategorii...";

            await LoadCategoriesAsync();

            StatusMessage = "Gotowy";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd inicjalizacji: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Ładuje wszystkie kategorie z bazy
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        var categories = await _databaseService.GetCategoriesAsync();

        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }

        StatusMessage = $"Załadowano {Categories.Count} kategorii";

        // Automatycznie wybierz pierwszą kategorię
        if (Categories.Count > 0 && SelectedCategory == null)
        {
            SelectedCategory = Categories[0];
        }
    }

    #endregion

    #region Property Changed Handlers

    /// <summary>
    /// Handler zmiany wybranej kategorii - ładuje produkty tej kategorii
    /// </summary>
    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (value != null)
        {
            _ = LoadProductsForCategoryAsync(value.Id);
        }
        else
        {
            SourceProducts.Clear();
            _allProductsInCategory.Clear();
        }
    }

    /// <summary>
    /// Handler zmiany query wyszukiwania - filtruje produkty lokalnie
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        FilterSourceProducts();
    }

    /// <summary>
    /// Handler zmiany kolekcji OfferItems - aktualizuje sumy
    /// </summary>
    private void OnOfferItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Nowe itemy - subskrybuj PropertyChanged dla live updates
        if (e.NewItems != null)
        {
            foreach (SavedOfferItem item in e.NewItems)
            {
                item.PropertyChanged += OnOfferItemPropertyChanged;
            }
        }

        // Usunięte itemy - odsubskrybuj
        if (e.OldItems != null)
        {
            foreach (SavedOfferItem item in e.OldItems)
            {
                item.PropertyChanged -= OnOfferItemPropertyChanged;
            }
        }

        // Invalidate cache - zmiana struktury kolekcji
        _currentCollectionVersion++;

        // Aktualizuj podsumowanie
        UpdateOfferSummary();

        // Odśwież dostępne produkty tylko gdy usuwamy z oferty
        // (przy dodawaniu produkt jest już usunięty z SourceProducts w AddProductToOffer)
        if (e.Action == NotifyCollectionChangedAction.Remove || 
            e.Action == NotifyCollectionChangedAction.Reset)
        {
            FilterSourceProducts();
        }
    }

    /// <summary>
    /// Handler zmiany property w SavedOfferItem (Quantity, Margin)
    /// Aktualizuje podsumowanie w czasie rzeczywistym z debouncing
    /// </summary>
    private void OnOfferItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Jeśli zmieniono Quantity lub Margin, zaktualizuj podsumowanie z debouncing
        if (e.PropertyName == nameof(SavedOfferItem.Quantity) ||
            e.PropertyName == nameof(SavedOfferItem.Margin) ||
            e.PropertyName == nameof(SavedOfferItem.TotalGross))
        {
            UpdateOfferSummaryDebounced();
        }
    }

    /// <summary>
    /// Aktualizuje computed properties podsumowania oferty z debouncing
    /// </summary>
    private void UpdateOfferSummaryDebounced()
    {
        if (_suppressNotifications) return;

        // Anuluj poprzedni timer
        _updateSummaryDebounceTimer?.Dispose();
        
        // Ustaw nowy timer
        _updateSummaryDebounceTimer = new Timer(_ =>
        {
            UpdateOfferSummaryImmediate();
            _updateSummaryDebounceTimer?.Dispose();
        }, null, SummaryDebounceDelayMs, Timeout.Infinite);
    }

    /// <summary>
    /// Natychmiastowa aktualizacja podsumowania (bez debouncing)
    /// </summary>
    private void UpdateOfferSummaryImmediate()
    {
        if (_suppressNotifications) return;

        OnPropertyChanged(nameof(TotalOfferNet));
        OnPropertyChanged(nameof(TotalOfferVat));
        OnPropertyChanged(nameof(TotalOfferGross));
        OnPropertyChanged(nameof(OfferItemsCount));
        OnPropertyChanged(nameof(OfferItemsInfo));
        
        // NIE invaliduj cache dla grupowania - tylko dla zmian wartości, nie struktury
        OnPropertyChanged(nameof(OfferItemsGroupedByCategory));
    }

    /// <summary>
    /// Aktualizacja bez debounce (dla operacji strukturalnych)
    /// </summary>
    private void UpdateOfferSummary()
    {
        UpdateOfferSummaryImmediate();
    }

    #endregion

    #region Loading Products

    /// <summary>
    /// Ładuje produkty dla wybranej kategorii
    /// </summary>
    private async Task LoadProductsForCategoryAsync(int categoryId)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Ładowanie produktów...";

            // Pobierz wszystkie produkty w kategorii
            var products = await _databaseService.GetProductsByCategoryAsync(categoryId);
            _allProductsInCategory = products.ToList();

            // Zastosuj filtry
            FilterSourceProducts();

            StatusMessage = $"Załadowano {_allProductsInCategory.Count} produktów";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd ładowania produktów: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Filtruje produkty do wyświetlenia w środkowej kolumnie:
    /// 1. Filtruje po search query (nazwa, kod)
    /// 2. Ukrywa produkty już dodane do oferty
    /// </summary>
    private void FilterSourceProducts()
    {
        // Pobierz ID produktów już w ofercie
        var productsInOfferIds = new HashSet<int>(
            OfferItems.Where(i => i.ProductId.HasValue)
                     .Select(i => i.ProductId!.Value)
        );

        // Filtruj produkty
        var filtered = _allProductsInCategory
            .Where(p =>
            {
                // Ukryj jeśli już w ofercie
                if (productsInOfferIds.Contains(p.Id))
                    return false;

                // Filtruj po search query (ignoruj wielkość liter, obsługuj polskie znaki)
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    return (p.Name?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (p.Code?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false);
                }

                return true;
            })
            .ToList();

        // Aktualizuj kolekcję
        SourceProducts.Clear();
        foreach (var product in filtered)
        {
            SourceProducts.Add(product);
        }
    }

    #endregion

    #region Commands - Zarządzanie Ofertą

    /// <summary>
    /// Komenda: Dodaj produkt do oferty
    /// </summary>
    [RelayCommand]
    private void AddProductToOffer(Product? product)
    {
        if (product == null)
        {
            StatusMessage = "Wybierz produkt do dodania";
            return;
        }

        try
        {
            // Sprawdź czy produkt już w ofercie
            if (OfferItems.Any(i => i.ProductId == product.Id))
            {
                StatusMessage = $"Produkt '{product.Name}' już jest w ofercie";
                return;
            }

            // Pobierz domyślną marżę z kategorii
            var defaultMargin = SelectedCategory?.DefaultMargin ?? 0m;

            // Utwórz nowy SavedOfferItem
            var offerItem = new SavedOfferItem
            {
                ProductId = product.Id,
                Name = product.Name,
                CategoryName = product.Category?.Name,
                Unit = product.Unit ?? "szt.",
                PurchasePriceNet = product.PurchasePriceNet,
                VatRate = product.VatRate,
                Margin = defaultMargin, // Domyślna marża z kategorii
                Quantity = 1m // Domyślna ilość
            };

            // Dodaj do oferty
            OfferItems.Add(offerItem);

            // Usuń produkt z listy źródłowej aby nie przebudowywać całej listy
            // (zapobiega niepożądanemu przewijaniu ListBox)
            SourceProducts.Remove(product);

            StatusMessage = $"Dodano: {product.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd dodawania produktu: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Usuń produkt z oferty
    /// </summary>
    [RelayCommand]
    private void RemoveFromOffer(SavedOfferItem? item)
    {
        if (item == null)
        {
            StatusMessage = "Wybierz pozycję do usunięcia";
            return;
        }

        try
        {
            OfferItems.Remove(item);
            StatusMessage = $"Usunięto: {item.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd usuwania: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Dodaj wszystkie produkty z aktualnie wybranej kategorii
    /// BATCH OPERATION - zawiesza notyfikacje
    /// </summary>
    [RelayCommand]
    private void AddAllProductsFromCategory()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Wybierz kategorię";
            return;
        }

        try
        {
            var productsToAdd = SourceProducts.ToList();
            if (productsToAdd.Count == 0)
            {
                StatusMessage = "Brak produktów do dodania";
                return;
            }

            var defaultMargin = SelectedCategory.DefaultMargin;
            int addedCount = 0;

            // BATCH: Zawieś notyfikacje
            _suppressNotifications = true;

            foreach (var product in productsToAdd)
            {
                // Sprawdź czy produkt już w ofercie
                if (OfferItems.Any(i => i.ProductId == product.Id))
                    continue;

                var offerItem = new SavedOfferItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    CategoryName = product.Category?.Name,
                    Unit = product.Unit ?? "szt.",
                    PurchasePriceNet = product.PurchasePriceNet,
                    VatRate = product.VatRate,
                    Margin = defaultMargin,
                    Quantity = 1m
                };

                OfferItems.Add(offerItem);
                addedCount++;
            }

            // BATCH: Wznów notyfikacje i zaktualizuj raz
            _suppressNotifications = false;
            UpdateOfferSummary();

            // Wyczyść listę źródłową po dodaniu wszystkich produktów
            SourceProducts.Clear();

            StatusMessage = $"Dodano {addedCount} produktów z kategorii '{SelectedCategory.Name}'";
        }
        catch (Exception ex)
        {
            _suppressNotifications = false;
            StatusMessage = $"Błąd dodawania produktów: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Wyczyść całą ofertę
    /// </summary>
    [RelayCommand]
    private void ClearOffer()
    {
        if (OfferItems.Count == 0)
        {
            StatusMessage = "Oferta jest już pusta";
            return;
        }

        try
        {
            var count = OfferItems.Count;
            OfferItems.Clear();
            StatusMessage = $"Usunięto {count} pozycji z oferty";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd czyszczenia oferty: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Zaznacz wszystkie produkty w ofercie
    /// </summary>
    [RelayCommand]
    private void SelectAllOfferItems()
    {
        foreach (var item in OfferItems)
        {
            item.IsSelected = true;
        }
        StatusMessage = $"Zaznaczono {OfferItems.Count} produktów";
    }

    /// <summary>
    /// Komenda: Odznacz wszystkie produkty w ofercie
    /// </summary>
    [RelayCommand]
    private void DeselectAllOfferItems()
    {
        foreach (var item in OfferItems)
        {
            item.IsSelected = false;
        }
        StatusMessage = "Odznaczono wszystkie produkty";
    }

    /// <summary>
    /// Komenda: Przełącz zaznaczenie wszystkich produktów w kategorii
    /// </summary>
    [RelayCommand]
    private void ToggleCategorySelection(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return;

        try
        {
            var categoryItems = OfferItems.Where(x => x.CategoryName == categoryName).ToList();
            
            if (categoryItems.Count == 0)
                return;

            // Sprawdź czy wszystkie są zaznaczone
            bool allSelected = categoryItems.All(x => x.IsSelected);
            
            // Jeśli wszystkie zaznaczone - odznacz, w przeciwnym razie - zaznacz
            bool newState = !allSelected;
            
            foreach (var item in categoryItems)
            {
                item.IsSelected = newState;
            }

            StatusMessage = newState 
                ? $"Zaznaczono {categoryItems.Count} produktów w kategorii '{categoryName}'"
                : $"Odznaczono produkty w kategorii '{categoryName}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd przełączania zaznaczenia: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Zmień marżę dla zaznaczonych produktów
    /// BATCH OPERATION - zawiesza notyfikacje
    /// </summary>
    [RelayCommand]
    private void ApplyMarginToSelected()
    {
        try
        {
            var selectedItems = OfferItems.Where(x => x.IsSelected).ToList();
            
            if (selectedItems.Count == 0)
            {
                StatusMessage = "Nie zaznaczono żadnych produktów";
                return;
            }

            // BATCH: Zawieś notyfikacje
            _suppressNotifications = true;

            foreach (var item in selectedItems)
            {
                item.Margin = NewMarginForSelected;
                item.IsSelected = false; // Wyczyść zaznaczenie po zastosowaniu marży
            }

            // BATCH: Wznów notyfikacje i zaktualizuj raz
            _suppressNotifications = false;
            UpdateOfferSummary();

            StatusMessage = $"Zmieniono marżę na {NewMarginForSelected:F2}% dla {selectedItems.Count} produktów";
        }
        catch (Exception ex)
        {
            _suppressNotifications = false;
            StatusMessage = $"Błąd zmiany marży: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Usuń zaznaczone produkty z oferty
    /// BATCH OPERATION - zawiesza notyfikacje
    /// </summary>
    [RelayCommand]
    private void RemoveSelected()
    {
        try
        {
            var selectedItems = OfferItems.Where(x => x.IsSelected).ToList();
            
            if (selectedItems.Count == 0)
            {
                StatusMessage = "Nie zaznaczono żadnych produktów do usunięcia";
                return;
            }

            int count = selectedItems.Count;
            
            // BATCH: Zawieś notyfikacje
            _suppressNotifications = true;
            
            foreach (var item in selectedItems)
            {
                OfferItems.Remove(item);
            }

            // BATCH: Wznów notyfikacje i zaktualizuj raz
            _suppressNotifications = false;
            UpdateOfferSummary();

            StatusMessage = $"Usunięto {count} produktów z oferty";
        }
        catch (Exception ex)
        {
            _suppressNotifications = false;
            StatusMessage = $"Błąd usuwania produktów: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Odśwież listę kategorii
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadCategoriesAsync();
    }

    /// <summary>
    /// Komenda: Otwórz okno zarządzania kolejnością produktów
    /// </summary>
    [RelayCommand]
    private async Task OpenProductOrderWindowAsync()
    {
        if (OfferItems.Count == 0)
        {
            var emptyBox = MessageBoxManager.GetMessageBoxStandard(
                "Pusta oferta",
                "Dodaj produkty do oferty, aby zmienić ich kolejność.",
                ButtonEnum.Ok,
                Icon.Info);
            await emptyBox.ShowAsync();
            return;
        }

        try
        {
            // Stwórz ViewModel z callbackiem do zastosowania zmian
            var viewModel = new OfferOrderViewModel(
                OfferItems.ToList(),
                onApply: (newOrder) =>
                {
                    // Zastosuj nową kolejność do OfferItems
                    ApplyNewProductOrder(newOrder);
                },
                onCancel: null
            );

            // Stwórz i otwórz okno
            var window = new Views.OfferOrderWindow
            {
                DataContext = viewModel
            };

            var mainWindow = _getMainWindow();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd otwierania okna kolejności: {ex.Message}";
        }
    }

    /// <summary>
    /// Zastosuj nową kolejność produktów
    /// </summary>
    private void ApplyNewProductOrder(List<SavedOfferItem> newOrder)
    {
        try
        {
            // Zawieś notyfikacje podczas masowej operacji
            _suppressNotifications = true;

            // Wyczyść obecną kolekcję
            OfferItems.Clear();

            // Dodaj produkty w nowej kolejności
            foreach (var item in newOrder)
            {
                OfferItems.Add(item);
            }

            // Wznów notyfikacje
            _suppressNotifications = false;

            // Invalidate cache - zmiana kolejności
            _currentCollectionVersion++;

            // Ręcznie wywołaj aktualizację podsumowania i zgrupowanego widoku
            UpdateOfferSummary();
            OnPropertyChanged(nameof(OfferItemsGroupedByCategory));

            StatusMessage = $"Kolejność {newOrder.Count} produktów została zaktualizowana";
        }
        catch (Exception ex)
        {
            _suppressNotifications = false;
            StatusMessage = $"Błąd stosowania nowej kolejności: {ex.Message}";
        }
    }

    /// <summary>
    /// Komenda: Generuj PDF z ofertą
    /// </summary>
    [RelayCommand]
    private async Task GeneratePdfAsync()
    {
        // Walidacja: oferta nie może być pusta
        if (OfferItems.Count == 0)
        {
            var emptyBox = MessageBoxManager.GetMessageBoxStandard(
                "Pusta oferta",
                "Nie można wygenerować PDF - oferta jest pusta. Dodaj produkty do oferty.",
                ButtonEnum.Ok,
                Icon.Warning);
            await emptyBox.ShowAsync();
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Przygotowywanie danych...";

            // Pobierz dane wizytówki
            var businessCard = await _databaseService.GetBusinessCardAsync();
            if (businessCard == null)
            {
                StatusMessage = "Brak danych wizytówki - tworzę domyślne";
                businessCard = new BusinessCard
                {
                    Company = "Moja Firma",
                    FullName = "Jan Kowalski",
                    Phone = "+48 123 456 789",
                    Email = "kontakt@firma.pl"
                };
            }

            // Otwórz dialog zapisu pliku
            var mainWindow = _getMainWindow?.Invoke();
            if (mainWindow == null)
            {
                StatusMessage = "Błąd: Brak dostępu do okna głównego";
                return;
            }

            var storageProvider = mainWindow.StorageProvider;
            var offerDate = DateTime.Now;
            var suggestedFileName = $"{OfferName} z dnia {offerDate:yyyy-MM-dd}.pdf";
            
            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Zapisz ofertę jako PDF",
                DefaultExtension = "pdf",
                SuggestedFileName = suggestedFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF Document")
                    {
                        Patterns = new[] { "*.pdf" }
                    }
                }
            });

            if (file == null)
            {
                StatusMessage = "Anulowano zapis PDF";
                return;
            }

            var filePath = file.Path.LocalPath;

            // Generuj PDF w tle
            StatusMessage = "Generowanie PDF...";
            await Task.Run(async () =>
            {
                await _pdfService.GenerateOfferPdfAsync(OfferItems, businessCard, filePath, OfferName, offerDate, null);
            });

            StatusMessage = "PDF wygenerowany pomyślnie!";

            // Pytanie o otwarcie pliku
            var openBox = MessageBoxManager.GetMessageBoxStandard(
                "PDF wygenerowany",
                $"Plik PDF został zapisany:\n{filePath}\n\nCzy chcesz otworzyć plik?",
                ButtonEnum.YesNo,
                Icon.Success);

            var result = await openBox.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                // Otwórz plik w domyślnej aplikacji
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd generowania PDF: {ex.Message}";
            
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się wygenerować PDF:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Zapisywanie i Wczytywanie Ofert

    /// <summary>
    /// Aktualnie edytowana oferta (null = nowa oferta)
    /// </summary>
    [ObservableProperty]
    private SavedOffer? _currentOffer;

    /// <summary>
    /// Zapisz ofertę (nową lub edytowaną)
    /// </summary>
    [RelayCommand]
    private async Task SaveOfferAsync()
    {
        try
        {
            if (OfferItems.Count == 0)
            {
                var msgBox = MessageBoxManager.GetMessageBoxStandard(
                    "Błąd",
                    "Oferta nie zawiera żadnych pozycji. Dodaj produkty przed zapisaniem.",
                    ButtonEnum.Ok,
                    Icon.Warning);
                await msgBox.ShowWindowDialogAsync(_getMainWindow());
                return;
            }

            // Otwórz dialog z tytułem
            var dialog = new Views.InputDialog(
                "Zapisz ofertę",
                "Wprowadź tytuł oferty:",
                "np. Oferta dla klienta ABC",
                CurrentOffer?.Title ?? "");
            
            var owner = _getMainWindow();
            if (owner == null) return;

            await dialog.ShowDialog(owner);

            if (!dialog.DialogResult || string.IsNullOrWhiteSpace(dialog.InputValue))
                return;

            // Przygotuj nagłówek oferty
            var offer = CurrentOffer ?? new SavedOffer();
            offer.Title = dialog.InputValue.Trim();
            offer.CreatedDate = CurrentOffer?.CreatedDate ?? DateTime.Now;
            offer.ModifiedDate = DateTime.Now;

            // Przygotuj pozycje oferty
            var items = OfferItems.Select(oi => new SavedOfferItem
            {
                OfferId = offer.Id,
                ProductId = oi.ProductId,
                Name = oi.Name,
                CategoryName = oi.CategoryName,
                Unit = oi.Unit,
                PurchasePriceNet = oi.PurchasePriceNet,
                VatRate = oi.VatRate,
                Margin = oi.Margin,
                Quantity = oi.Quantity
            }).ToList();

            // Zapisz w bazie (transakcja)
            var offerId = await _databaseService.SaveOfferAsync(offer, items);
            offer.Id = offerId;
            CurrentOffer = offer;

            var successBox = MessageBoxManager.GetMessageBoxStandard(
                "Sukces",
                $"Oferta '{offer.Title}' została zapisana pomyślnie.",
                ButtonEnum.Ok,
                Icon.Success);
            await successBox.ShowWindowDialogAsync(_getMainWindow());
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się zapisać oferty:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    /// <summary>
    /// Wczytaj zapisaną ofertę do edycji
    /// </summary>
    public async Task LoadOfferAsync(SavedOffer offer, List<SavedOfferItem> items)
    {
        try
        {
            // Ustaw bieżącą ofertę (tryb edycji)
            CurrentOffer = offer;

            // Wyczyść obecną ofertę
            OfferItems.Clear();

            // Wczytaj pozycje
            foreach (var item in items)
            {
                var offerItem = new SavedOfferItem
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    CategoryName = item.CategoryName,
                    Unit = item.Unit,
                    PurchasePriceNet = item.PurchasePriceNet,
                    VatRate = item.VatRate,
                    Margin = item.Margin,
                    Quantity = item.Quantity
                };

                OfferItems.Add(offerItem);
            }

            // Odśwież filtry
            FilterSourceProducts();
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się wczytać oferty:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    /// <summary>
    /// Wczytaj ofertę jako szablon (nowa oferta z tymi samymi produktami i cenami)
    /// </summary>
    public async Task LoadOfferAsTemplateAsync(List<SavedOfferItem> items)
    {
        try
        {
            // NIE ustawiaj CurrentOffer (nowa oferta)
            CurrentOffer = null;

            // Wyczyść obecną ofertę
            OfferItems.Clear();

            // Wczytaj pozycje
            foreach (var item in items)
            {
                var offerItem = new SavedOfferItem
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    CategoryName = item.CategoryName,
                    Unit = item.Unit,
                    PurchasePriceNet = item.PurchasePriceNet,
                    VatRate = item.VatRate,
                    Margin = item.Margin,
                    Quantity = item.Quantity
                };

                OfferItems.Add(offerItem);
            }

            // Odśwież filtry
            FilterSourceProducts();
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się wczytać szablonu:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    /// <summary>
    /// Nowa oferta (wyczyść wszystko)
    /// </summary>
    [RelayCommand]
    private void NewOffer()
    {
        CurrentOffer = null;
        OfferItems.Clear();
        _currentCollectionVersion++; // Invalidate cache
        OnPropertyChanged(nameof(OfferItemsGroupedByCategory));
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup - odsubskrybuj eventy i wyczyść timery
    /// </summary>
    public void Cleanup()
    {
        _updateSummaryDebounceTimer?.Dispose();
        OfferItems.CollectionChanged -= OnOfferItemsCollectionChanged;

        foreach (var item in OfferItems)
        {
            item.PropertyChanged -= OnOfferItemPropertyChanged;
        }
    }

    #endregion
}
