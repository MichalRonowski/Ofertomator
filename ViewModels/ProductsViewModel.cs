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
        
        // Inicjalizacja - załaduj pierwszą stronę
        _ = LoadProductsAsync();
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
            async _ => await PerformSearchAsync(),
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
    }

    /// <summary>
    /// Handler dla zmiany liczby stron
    /// </summary>
    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(PageInfo));
    }

    /// <summary>
    /// Handler dla zmiany statusu busy
    /// </summary>
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    /// <summary>
    /// Handler dla zmiany liczby produktów
    /// </summary>
    partial void OnTotalProductsChanged(int value)
    {
        OnPropertyChanged(nameof(ProductsInfo));
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
            OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
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

            var result = await dialog.ShowDialog<bool>(mainWindow);

            // Jeśli użytkownik zapisał produkt, odśwież listę
            if (result)
            {
                StatusMessage = "Produkt dodany pomyślnie";
                await LoadProductsAsync();
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

            var result = await dialog.ShowDialog<bool>(mainWindow);

            // Jeśli użytkownik zapisał zmiany, odśwież listę
            if (result)
            {
                StatusMessage = "Produkt zaktualizowany pomyślnie";
                await LoadProductsAsync();
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
    /// TODO: Implementacja w przyszłości (zaznaczanie checkboxami)
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedProductsAsync()
    {
        StatusMessage = "Masowe usuwanie - TODO: Implementacja w przyszłości";
        await Task.Delay(100);
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
