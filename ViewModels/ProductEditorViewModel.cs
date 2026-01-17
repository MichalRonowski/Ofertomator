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
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Kod produktu jest wymagany")]
    [MinLength(1, ErrorMessage = "Kod musi mieć co najmniej 1 znak")]
    [MaxLength(50, ErrorMessage = "Kod może mieć maksymalnie 50 znaków")]
    private string _code = string.Empty;

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

    #region Initialization

    /// <summary>
    /// Inicjalizacja ViewModelu - ładowanie kategorii
    /// Wywołaj w OnInitialized w code-behind
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();

        // Jeśli tryb edycji, ustaw wybraną kategorię
        if (IsEditMode && _originalProduct != null)
        {
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == _originalProduct.CategoryId);
        }
        // Jeśli tryb dodawania i jest tylko jedna kategoria, wybierz ją
        else if (Categories.Count == 1)
        {
            SelectedCategory = Categories[0];
        }
    }

    /// <summary>
    /// Ładowanie listy kategorii z bazy danych
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Ładowanie kategorii...";

            var categories = await _databaseService.GetCategoriesAsync();
            
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            if (Categories.Count == 0)
            {
                StatusMessage = "Brak kategorii w bazie. Dodaj najpierw kategorię!";
            }
            else
            {
                StatusMessage = $"Załadowano {Categories.Count} kategorii";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd ładowania kategorii: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
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
    }

    /// <summary>
    /// Handler zmiany statusu IsBusy
    /// </summary>
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    #endregion

    #region Commands

    /// <summary>
    /// Komenda zapisania produktu (dodanie lub aktualizacja)
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

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
                    Unit = string.IsNullOrWhiteSpace(Unit) ? null : Unit.Trim(),
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
                    Unit = string.IsNullOrWhiteSpace(Unit) ? null : Unit.Trim(),
                    CategoryId = CategoryId,
                    PriceUpdateDate = DateTime.Now
                };

                var productId = await _databaseService.AddProductAsync(newProduct);
                success = productId > 0;
                StatusMessage = success ? "Produkt dodany pomyślnie" : "Błąd dodawania produktu";
            }

            if (success)
            {
                // Zamknij okno po udanym zapisie
                await Task.Delay(500); // Krótkie opóźnienie, żeby użytkownik zobaczył komunikat
                RequestClose?.Invoke(this, EventArgs.Empty);
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
