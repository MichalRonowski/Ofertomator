using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ofertomator.Models;
using Ofertomator.Services;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla zarządzania produktami
/// Implementuje paginację, wyszukiwanie z debouncing i CRUD operations
/// </summary>
public partial class ProductsViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly Func<Avalonia.Controls.Window?> _getMainWindow;
    private Timer? _searchDebounceTimer;
    private const int SearchDebounceMs = 300; // 300ms zgodnie ze specyfikacją
    private const int PageSize = 100; // 100 produktów na stronę

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalProducts = 0;

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategoryForBulk;

    /// <summary>
    /// Liczba zaznaczonych produktów
    /// </summary>
    public int SelectedCount => Products.Count(p => p.IsSelected);

    /// <summary>
    /// Czy można wykonać masową zmianę kategorii
    /// </summary>
    public bool CanChangeBulkCategory => SelectedCount > 0 && SelectedCategoryForBulk != null && !IsBusy;

    /// <summary>
    /// Czy przycisk "Poprzednia" jest dostępny
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1 && !IsBusy;

    /// <summary>
    /// Czy przycisk "Następna" jest dostępny
    /// </summary>
    public bool CanGoToNextPage => CurrentPage < TotalPages && !IsBusy;

    /// <summary>
    /// Informacja o aktualnej stronie
    /// </summary>
    public string PageInfo => TotalPages > 0 
        ? $"Strona {CurrentPage} z {TotalPages}" 
        : "Brak danych";

    /// <summary>
    /// Informacja o liczbie produktów
    /// </summary>
    public string ProductsInfo => $"Produktów: {TotalProducts}";

    #endregion

    public ProductsViewModel(DatabaseService databaseService, Func<Avalonia.Controls.Window?> getMainWindow)
    {
        _databaseService = databaseService;
        _getMainWindow = getMainWindow;
        
        // Inicjalizacja - załaduj pierwszą stronę i kategorie
        _ = LoadProductsAsync();
        _ = LoadCategoriesAsync();
    }

    #region Property Changed Handlers

    /// <summary>
    /// Handler dla zmiany SearchQuery - implementuje debouncing
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // Anuluj poprzedni timer
        _searchDebounceTimer?.Dispose();

        // Ustaw nowy timer na 300ms
        _searchDebounceTimer = new Timer(
            _ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await PerformSearchAsync();
                });
            },
            null,
            SearchDebounceMs,
            Timeout.Infinite
        );
    }

    /// <summary>
    /// Handler dla zmiany strony - aktualizuj dostępność przycisków
    /// </summary>
    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(PageInfo));
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Handler dla zmiany liczby stron
    /// </summary>
    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(PageInfo));
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Handler dla zmiany statusu busy
    /// </summary>
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Handler dla zmiany liczby produktów
    /// </summary>
    partial void OnTotalProductsChanged(int value)
    {
        OnPropertyChanged(nameof(ProductsInfo));
    }

    /// <summary>
    /// Handler dla zmiany wybranej kategorii do masowej zmiany
    /// </summary>
    partial void OnSelectedCategoryForBulkChanged(Category? value)
    {
        ChangeBulkCategoryCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Loading and Search

    /// <summary>
    /// Ładuje produkty z bazy danych z paginacją
    /// </summary>
    private async Task LoadProductsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Ładowanie produktów...";

        try
        {
            // Pobierz produkty i całkowitą liczbę
            var searchQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
            var (products, totalCount) = await _databaseService.GetProductsAsync(
                CurrentPage, 
                PageSize, 
                searchQuery
            );

            // Aktualizuj kolekcję w UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Products.Clear();
                foreach (var product in products)
                {
                    // Subskrybuj zmiany IsSelected dla każdego produktu
                    product.PropertyChanged += Product_PropertyChanged;
                    Products.Add(product);
                }

                // Aktualizuj statystyki
                TotalProducts = totalCount;
                TotalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / PageSize) : 1;

                StatusMessage = Products.Count > 0 
                    ? $"Załadowano {Products.Count} produktów" 
                    : "Brak produktów do wyświetlenia";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd ładowania: {ex.Message}";
            Console.WriteLine($"Błąd w LoadProductsAsync: {ex.Message}");
            
            // Graceful degradation
            Products.Clear();
            TotalProducts = 0;
            TotalPages = 1;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Wykonuje wyszukiwanie (wywołane przez debounce timer)
    /// </summary>
    private async Task PerformSearchAsync()
    {
        // Resetuj do pierwszej strony przy wyszukiwaniu
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    /// <summary>
    /// Ładuje kategorie z bazy danych
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _databaseService.GetCategoriesAsync();
            
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd ładowania kategorii: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler dla zmian w produktach (np. IsSelected)
    /// </summary>
    private void Product_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Product.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedCount));
            ChangeBulkCategoryCommand.NotifyCanExecuteChanged();
        }
    }

    #endregion

    #region Pagination Commands

    /// <summary>
    /// Komenda: Przejdź do poprzedniej strony
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadProductsAsync();
        }
    }

    /// <summary>
    /// Komenda: Przejdź do następnej strony
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task GoToNextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadProductsAsync();
        }
    }

    /// <summary>
    /// Komenda: Odśwież listę produktów
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadProductsAsync();
    }

    #endregion

    #region CRUD Commands

    /// <summary>
    /// Komenda: Dodaj nowy produkt
    /// TODO: Otworzyć dialog dodawania produktu (KROK 3)
    /// </summary>
    [RelayCommand]
    private async Task AddProductAsync()
    {
        try
        {
            var mainWindow = _getMainWindow();
            if (mainWindow == null)
            {
                StatusMessage = "Błąd: Nie można otworzyć okna dialogowego";
                return;
            }

            // Utwórz ViewModel dla trybu dodawania
            var editorViewModel = new ProductEditorViewModel(_databaseService);

            // Utwórz i pokaż okno dialogowe
            var dialog = new Views.ProductWindow
            {
                DataContext = editorViewModel
            };

            // Inicjalizuj ViewModel (załaduj kategorie)
            await editorViewModel.InitializeAsync();

            var result = await dialog.ShowDialog<bool>(mainWindow);

            // Odśwież listę po zamknięciu okna (niezależnie od wyniku)
            await LoadProductsAsync();
            
            if (result)
            {
                StatusMessage = "Produkt dodany pomyślnie";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd dodawania produktu: {ex.Message}";
            Console.WriteLine($"Błąd w AddProductAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Komenda: Edytuj wybrany produkt
    /// </summary>
    [RelayCommand]
    private async Task EditProductAsync()
    {
        if (SelectedProduct == null)
        {
            StatusMessage = "Wybierz produkt do edycji";
            return;
        }

        try
        {
            var mainWindow = _getMainWindow();
            if (mainWindow == null)
            {
                StatusMessage = "Błąd: Nie można otworzyć okna dialogowego";
                return;
            }

            // Utwórz ViewModel dla trybu edycji (przekaż istniejący produkt)
            var editorViewModel = new ProductEditorViewModel(_databaseService, SelectedProduct);

            // Utwórz i pokaż okno dialogowe
            var dialog = new Views.ProductWindow
            {
                DataContext = editorViewModel
            };

            // Inicjalizuj ViewModel (załaduj kategorie)
            await editorViewModel.InitializeAsync();

            var result = await dialog.ShowDialog<bool>(mainWindow);

            // Odśwież listę po zamknięciu okna (niezależnie od wyniku)
            await LoadProductsAsync();
            
            if (result)
            {
                StatusMessage = "Produkt zaktualizowany pomyślnie";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd edycji produktu: {ex.Message}";
            Console.WriteLine($"Błąd w EditProductAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Komenda: Usuń wybrany produkt
    /// </summary>
    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null)
        {
            StatusMessage = "Wybierz produkt do usunięcia";
            return;
        }

        var productToDelete = SelectedProduct;

        // TODO: W przyszłości dodać MessageBox z potwierdzeniem
        // Na razie usuwamy bezpośrednio

        IsBusy = true;
        StatusMessage = $"Usuwanie produktu: {productToDelete.Name}...";

        try
        {
            var success = await _databaseService.DeleteProductAsync(productToDelete.Id);

            if (success)
            {
                StatusMessage = $"Produkt '{productToDelete.Name}' został usunięty";
                
                // Odśwież listę
                await LoadProductsAsync();
            }
            else
            {
                StatusMessage = "Nie można usunąć produktu";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd usuwania: {ex.Message}";
            Console.WriteLine($"Błąd w DeleteProductAsync: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Komenda: Usuń wiele zaznaczonych produktów
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedProductsAsync()
    {
        var selectedProducts = Products.Where(p => p.IsSelected).ToList();
        if (selectedProducts.Count == 0)
        {
            StatusMessage = "Nie zaznaczono żadnych produktów do usunięcia";
            return;
        }

        IsBusy = true;
        var count = selectedProducts.Count;
        StatusMessage = $"Usuwanie {count} produktów...";

        try
        {
            // Usuń z bazy danych
            foreach (var product in selectedProducts)
            {
                await _databaseService.DeleteProductAsync(product.Id);
            }

            // Usuń z kolekcji Products w UI thread (tak jak w ChangeBulkCategory)
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var product in selectedProducts)
                {
                    Products.Remove(product);
                }
            });

            StatusMessage = $"Usunięto {count} produktów";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd podczas usuwania: {ex.Message}";
            Console.WriteLine($"Błąd w DeleteSelectedProductsAsync: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Komenda: Zaznacz wszystkie produkty na stronie
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var product in Products)
        {
            product.IsSelected = true;
        }
    }

    /// <summary>
    /// Komenda: Odznacz wszystkie produkty
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var product in Products)
        {
            product.IsSelected = false;
        }
    }

    /// <summary>
    /// Komenda: Zmień kategorię dla wszystkich zaznaczonych produktów
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanChangeBulkCategory))]
    private async Task ChangeBulkCategoryAsync()
    {
        if (SelectedCategoryForBulk == null || SelectedCount == 0)
            return;

        IsBusy = true;
        var count = SelectedCount;
        StatusMessage = $"Zmiana kategorii dla {count} produktów...";

        try
        {
            var selectedProducts = Products.Where(p => p.IsSelected).ToList();
            var categoryId = SelectedCategoryForBulk.Id;
            var category = SelectedCategoryForBulk;

            // Zapisz zmiany w bazie danych
            foreach (var product in selectedProducts)
            {
                product.CategoryId = categoryId;
                product.Category = category;
                await _databaseService.UpdateProductAsync(product);
            }

            StatusMessage = $"Zmieniono kategorię dla {count} produktów";
            
            // Odznacz wszystkie
            DeselectAll();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd podczas zmiany kategorii: {ex.Message}";
            Console.WriteLine($"Błąd w ChangeBulkCategoryAsync: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup przy zamykaniu widoku
    /// </summary>
    public void Dispose()
    {
        _searchDebounceTimer?.Dispose();
    }

    #endregion
}
