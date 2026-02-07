using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ofertomator.Models;
using Ofertomator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla okna edycji/dodawania produktu z walidacją
/// Dziedziczy po ObservableValidator dla wsparcia ValidationAttributes
/// </summary>
public partial class ProductEditorViewModel : ObservableValidator
{
    private readonly DatabaseService _databaseService;
    private readonly Product? _originalProduct;

    /// <summary>
    /// Zdarzenie zamknięcia okna - emitowane po udanym zapisie
    /// </summary>
    public event EventHandler? RequestClose;

    #region Observable Properties z Walidacją

    [ObservableProperty]
    [MaxLength(50, ErrorMessage = "Kod może mieć maksymalnie 50 znaków")]
    private string? _code;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Nazwa produktu jest wymagana")]
    [MinLength(3, ErrorMessage = "Nazwa musi mieć co najmniej 3 znaki")]
    [MaxLength(200, ErrorMessage = "Nazwa może mieć maksymalnie 200 znaków")]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.01, double.MaxValue, ErrorMessage = "Cena zakupu musi być większa od 0")]
    private decimal _purchasePriceNet;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 100, ErrorMessage = "Stawka VAT musi być między 0 a 100")]
    private decimal _vatRate = 23; // Domyślnie 23%

    [ObservableProperty]
    [MaxLength(20, ErrorMessage = "Jednostka może mieć maksymalnie 20 znaków")]
    private string? _unit = "szt."; // Domyślnie "szt."

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Kategoria jest wymagana")]
    private int _categoryId;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _windowTitle = "Nowy Produkt";

    #endregion

    #region Computed Properties

    /// <summary>
    /// Lista dostępnych kategorii
    /// </summary>
    public ObservableCollection<Category> Categories { get; } = new();

    /// <summary>
    /// Czy to jest tryb edycji (true) czy dodawania (false)
    /// </summary>
    public bool IsEditMode => _originalProduct != null;

    /// <summary>
    /// Czy przycisk Zapisz powinien być aktywny
    /// </summary>
    public bool CanSave => !HasErrors && !IsBusy && SelectedCategory != null;

    /// <summary>
    /// Tekst przycisku Zapisz
    /// </summary>
    public string SaveButtonText => IsEditMode ? "Zapisz zmiany" : "Dodaj produkt";

    #endregion

    #region Constructors

    /// <summary>
    /// Konstruktor dla trybu dodawania nowego produktu
    /// </summary>
    public ProductEditorViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _originalProduct = null;
        WindowTitle = "Nowy Produkt";
        
        // TEST: Log konstruktora
        try
        {
            var testPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "debug_categories.txt");
            System.IO.File.AppendAllText(testPath, $"\n=== KONSTRUKTOR (ADD MODE): {DateTime.Now} ===\n");
        }
        catch { }
        
        // Subskrybuj zmiany błędów walidacji
        ErrorsChanged += (s, e) => OnPropertyChanged(nameof(CanSave));
    }

    /// <summary>
    /// Konstruktor dla trybu edycji istniejącego produktu
    /// </summary>
    public ProductEditorViewModel(DatabaseService databaseService, Product product)
    {
        _databaseService = databaseService;
        _originalProduct = product;
        WindowTitle = $"Edycja: {product.Name}";

        // TEST: Log konstruktora
        try
        {
            var testPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "debug_categories.txt");
            System.IO.File.AppendAllText(testPath, $"\n=== KONSTRUKTOR (EDIT MODE): {DateTime.Now} ===\n");
        }
        catch { }

        // Wypełnij formularz danymi produktu
        Code = product.Code ?? string.Empty;
        Name = product.Name;
        PurchasePriceNet = product.PurchasePriceNet;
        VatRate = product.VatRate;
        Unit = product.Unit;
        CategoryId = product.CategoryId;

        // Subskrybuj zmiany błędów walidacji
        ErrorsChanged += (s, e) => OnPropertyChanged(nameof(CanSave));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Czyści formularz do wartości domyślnych
    /// </summary>
    private void ClearForm()
    {
        Code = string.Empty;
        Name = string.Empty;
        PurchasePriceNet = 0;
        VatRate = 23;
        Unit = "szt.";
        SelectedCategory = null;
        
        // Wyczyść wszystkie błędy walidacji
        ClearErrors();
        
        // Powiadom o zmianach stanu
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
        
        StatusMessage = "Formularz wyczyszczony. Możesz dodać kolejny produkt.";
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Inicjalizacja ViewModelu - ładowanie kategorii
    /// Wywołaj w OnInitialized w code-behind
    /// </summary>
    public async Task InitializeAsync()
    {
        var logPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "debug_categories.txt");
        System.IO.File.AppendAllText(logPath, "\n=== ProductEditorViewModel.InitializeAsync START ===\n");
        
        await LoadCategoriesAsync();

        System.IO.File.AppendAllText(logPath, $"=== Categories.Count after load: {Categories.Count} ===\n");
        
        // Jeśli tryb edycji, ustaw wybraną kategorię
        if (IsEditMode && _originalProduct != null)
        {
            System.IO.File.AppendAllText(logPath, $"=== Edit mode: looking for category ID {_originalProduct.CategoryId} ===\n");
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == _originalProduct.CategoryId);
            System.IO.File.AppendAllText(logPath, $"=== Selected category: {SelectedCategory?.Name ?? "NULL"} ===\n");
        }
        // Jeśli tryb dodawania i jest tylko jedna kategoria, wybierz ją
        else if (Categories.Count == 1)
        {
            System.IO.File.AppendAllText(logPath, "=== Auto-selecting single category ===\n");
            SelectedCategory = Categories[0];
        }
        
        System.IO.File.AppendAllText(logPath, "=== ProductEditorViewModel.InitializeAsync END ===\n\n");
    }

    /// <summary>
    /// Ładowanie listy kategorii z bazy danych
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        var logPath = "debug_categories.txt";
        System.IO.File.AppendAllText(logPath, $"\n=== LoadCategoriesAsync START {DateTime.Now} ===\n");
        try
        {
            IsBusy = true;
            StatusMessage = "Ładowanie kategorii...";

            System.IO.File.AppendAllText(logPath, "=== Calling GetCategoriesAsync ===\n");
            var categories = await _databaseService.GetCategoriesAsync();
            System.IO.File.AppendAllText(logPath, $"=== Got {categories.Count()} categories from database ===\n");
            
            // Aktualizuj kolekcję w UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                System.IO.File.AppendAllText(logPath, "=== Clearing Categories collection ===\n");
                Categories.Clear();
                foreach (var category in categories)
                {
                    System.IO.File.AppendAllText(logPath, $"=== Adding category: {category.Name} (ID: {category.Id}) ===\n");
                    Categories.Add(category);
                }
                System.IO.File.AppendAllText(logPath, $"=== Categories.Count after adding: {Categories.Count} ===\n");
            });

            if (Categories.Count == 0)
            {
                StatusMessage = "Brak kategorii w bazie. Dodaj najpierw kategorię!";
                System.IO.File.AppendAllText(logPath, "=== No categories found ===\n");
            }
            else
            {
                StatusMessage = $"Załadowano {Categories.Count} kategorii";
                System.IO.File.AppendAllText(logPath, $"=== StatusMessage: {StatusMessage} ===\n");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd ładowania kategorii: {ex.Message}";
            System.IO.File.AppendAllText(logPath, $"=== ERROR: {ex.Message} ===\n");
            System.IO.File.AppendAllText(logPath, $"=== Stack: {ex.StackTrace} ===\n");
        }
        finally
        {
            IsBusy = false;
            System.IO.File.AppendAllText(logPath, "=== LoadCategoriesAsync END ===\n");
        }
    }

    #endregion

    #region Property Changed Handlers

    /// <summary>
    /// Handler zmiany wybranej kategorii
    /// </summary>
    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (value != null)
        {
            CategoryId = value.Id;
            // Wyczyść błąd walidacji dla CategoryId
            ClearErrors(nameof(CategoryId));
        }
        OnPropertyChanged(nameof(CanSave));
        SaveCommand?.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Handler zmiany statusu IsBusy
    /// </summary>
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand?.NotifyCanExecuteChanged();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Komenda zapisania produktu (dodanie lub aktualizacja)
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        // Dodatkowa ochrona przed podwójnym kliknięciem
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            SaveCommand.NotifyCanExecuteChanged(); // Natychmiastowe wyłączenie przycisku

            // Walidacja przed zapisem
            ValidateAllProperties();
            if (HasErrors)
            {
                StatusMessage = "Popraw błędy walidacji przed zapisaniem";
                return;
            }

            // Sprawdź czy kategoria wybrana
            if (SelectedCategory == null)
            {
                StatusMessage = "Wybierz kategorię produktu";
                return;
            }

            bool success;

            if (IsEditMode && _originalProduct != null)
            {
                // Tryb edycji - aktualizuj istniejący produkt
                StatusMessage = "Zapisywanie zmian...";

                var updatedProduct = new Product
                {
                    Id = _originalProduct.Id,
                    Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim(),
                    Name = Name.Trim(),
                    PurchasePriceNet = PurchasePriceNet,
                    VatRate = VatRate,
                    Unit = string.IsNullOrWhiteSpace(Unit) ? "szt." : Unit.Trim(),
                    CategoryId = CategoryId,
                    PriceUpdateDate = DateTime.Now
                };

                success = await _databaseService.UpdateProductAsync(updatedProduct);
                StatusMessage = success ? "Produkt zaktualizowany pomyślnie" : "Błąd aktualizacji produktu";
            }
            else
            {
                // Tryb dodawania - utwórz nowy produkt
                StatusMessage = "Dodawanie produktu...";

                var newProduct = new Product
                {
                    Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim(),
                    Name = Name.Trim(),
                    PurchasePriceNet = PurchasePriceNet,
                    VatRate = VatRate,
                    Unit = string.IsNullOrWhiteSpace(Unit) ? "szt." : Unit.Trim(),
                    CategoryId = CategoryId,
                    PriceUpdateDate = DateTime.Now
                };

                var productId = await _databaseService.AddProductAsync(newProduct);
                success = productId > 0;
                StatusMessage = success ? "Produkt dodany pomyślnie" : "Błąd dodawania produktu";
            }

            if (success)
            {
                if (IsEditMode)
                {
                    // Tryb edycji - zamknij okno po zapisie
                    await Task.Delay(500);
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Tryb dodawania - wyczyść formularz i zostaw okno otwarte
                    await Task.Delay(800); // Pokaż komunikat sukcesu
                    ClearForm();
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Komenda anulowania (zamknięcie okna bez zapisu)
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
