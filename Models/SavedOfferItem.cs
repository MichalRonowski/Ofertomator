using System;
using System.Globalization;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ofertomator.Models;

/// <summary>
/// Pozycja w zapisanej ofercie z live kalkulacjami
/// Dziedziczy po ObservableObject dla reaktywności UI
/// </summary>
public partial class SavedOfferItem : ObservableObject
{
    private Timer? _marginDebounceTimer;
    private Timer? _salePriceDebounceTimer;
    private const int DebounceDelayMs = 600;
    public int Id { get; set; }
    
    public int OfferId { get; set; }
    
    /// <summary>
    /// ID produktu z bazy (nullable - produkt mógł być usunięty)
    /// </summary>
    public int? ProductId { get; set; }
    
    /// <summary>
    /// Nazwa produktu (snapshot w momencie dodania do oferty)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _name = string.Empty;
    
    /// <summary>
    /// Niestandardowa nazwa produktu dla tej oferty (opcjonalna)
    /// Jeśli jest ustawiona, będzie używana zamiast oryginalnej nazwy
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string? _customName;
    
    /// <summary>
    /// Nazwa do wyświetlenia - customName jeśli jest ustawiona, w przeciwnym razie name
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(CustomName) ? CustomName : Name;
    
    /// <summary>
    /// Nazwa kategorii (snapshot)
    /// </summary>
    [ObservableProperty]
    private string? _categoryName;
    
    [ObservableProperty]
    private string _unit = "szt.";
    
    /// <summary>
    /// Cena zakupu netto (snapshot)
    /// </summary>
    [ObservableProperty]
    private decimal _purchasePriceNet = 0m;
    
    /// <summary>
    /// Stawka VAT w procentach (snapshot)
    /// </summary>
    [ObservableProperty]
    private decimal _vatRate = 23m;
    
    /// <summary>
    /// Czy produkt jest zaznaczony do masowej edycji
    /// </summary>
    [ObservableProperty]
    private bool _isSelected = false;
    
    /// <summary>
    /// Marża w procentach - EDYTOWALNA przez użytkownika
    /// OPTYMALIZACJA: Zgrupowane notyfikacje
    /// </summary>
    private decimal _margin = 0m;
    private bool _isUpdatingMargin = false;
    public decimal Margin
    {
        get => Math.Round(_margin, 2);
        set
        {
            if (_isUpdatingMargin) return;
            
            var roundedValue = Math.Round(value, 2);
            if (SetProperty(ref _margin, roundedValue))
            {
                _isUpdatingMargin = true;
                // Aktualizuj cenę sprzedaży na podstawie marży
                _salePriceNet = PurchasePriceNet * (1 + roundedValue / 100m);
                
                // Zaktualizuj input fields (jeśli zmiana nie pochodzi z input)
                _marginInput = roundedValue.ToString("F2", CultureInfo.CurrentCulture);
                _salePriceNetInput = Math.Round(_salePriceNet, 2).ToString("F2", CultureInfo.CurrentCulture);
                
                // OPTYMALIZACJA: Batch notification - jedna dla wszystkich zmian
                OnPropertiesChanged(
                    nameof(SalePriceNet),
                    nameof(MarginInput),
                    nameof(SalePriceNetInput),
                    nameof(SalePriceGross),
                    nameof(TotalNet),
                    nameof(VatAmount),
                    nameof(TotalGross)
                );
                _isUpdatingMargin = false;
            }
        }
    }

    /// <summary>
    /// Input marży z debouncing - bindowany w XAML
    /// </summary>
    private string _marginInput = "0,00";
    public string MarginInput
    {
        get => _marginInput;
        set
        {
            if (SetProperty(ref _marginInput, value))
            {
                // Anuluj poprzedni timer
                _marginDebounceTimer?.Dispose();
                
                // Ustaw nowy timer
                _marginDebounceTimer = new Timer(_ =>
                {
                    if (decimal.TryParse(value.Replace('.', ','), NumberStyles.Any, CultureInfo.CurrentCulture, out var parsedValue))
                    {
                        Margin = parsedValue;
                    }
                    _marginDebounceTimer?.Dispose();
                }, null, DebounceDelayMs, Timeout.Infinite);
            }
        }
    }
    
    /// <summary>
    /// Cena sprzedaży netto jednostkowa - EDYTOWALNA przez użytkownika
    /// OPTYMALIZACJA: Zgrupowane notyfikacje
    /// </summary>
    private decimal _salePriceNet = 0m;
    private bool _isUpdatingSalePrice = false;
    public decimal SalePriceNet
    {
        get => Math.Round(_salePriceNet, 2);
        set
        {
            if (_isUpdatingSalePrice) return;
            
            var roundedValue = Math.Round(value, 2);
            if (SetProperty(ref _salePriceNet, roundedValue))
            {
                _isUpdatingSalePrice = true;
                // Aktualizuj marżę na podstawie ceny sprzedaży
                if (PurchasePriceNet > 0)
                {
                    _margin = ((roundedValue / PurchasePriceNet) - 1) * 100m;
                    _marginInput = Math.Round(_margin, 2).ToString("F2", CultureInfo.CurrentCulture);
                }
                
                // Zaktualizuj input field
                _salePriceNetInput = roundedValue.ToString("F2", CultureInfo.CurrentCulture);
                
                // OPTYMALIZACJA: Batch notification - jedna dla wszystkich zmian
                OnPropertiesChanged(
                    nameof(Margin),
                    nameof(MarginInput),
                    nameof(SalePriceNetInput),
                    nameof(SalePriceGross),
                    nameof(TotalNet),
                    nameof(VatAmount),
                    nameof(TotalGross)
                );
                _isUpdatingSalePrice = false;
            }
        }
    }

    /// <summary>
    /// Input ceny sprzedaży z debouncing - bindowany w XAML
    /// </summary>
    private string _salePriceNetInput = "0,00";
    public string SalePriceNetInput
    {
        get => _salePriceNetInput;
        set
        {
            if (SetProperty(ref _salePriceNetInput, value))
            {
                // Anuluj poprzedni timer
                _salePriceDebounceTimer?.Dispose();
                
                // Ustaw nowy timer  
                _salePriceDebounceTimer = new Timer(_ =>
                {
                    if (decimal.TryParse(value.Replace('.', ','), NumberStyles.Any, CultureInfo.CurrentCulture, out var parsedValue))
                    {
                        SalePriceNet = parsedValue;
                    }
                    _salePriceDebounceTimer?.Dispose();
                }, null, DebounceDelayMs, Timeout.Infinite);
            }
        }
    }
    
    /// <summary>
    /// Ilość (domyślnie 1.0) - EDYTOWALNA przez użytkownika
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalNet))]
    [NotifyPropertyChangedFor(nameof(VatAmount))]
    [NotifyPropertyChangedFor(nameof(TotalGross))]
    private decimal _quantity = 1m;
    
    /// <summary>
    /// Kalkulowana cena sprzedaży brutto jednostkowa
    /// Automatycznie przeliczana gdy zmieni się SalePriceNet lub VatRate
    /// </summary>
    public decimal SalePriceGross => SalePriceNet * (1 + VatRate / 100m);
    
    /// <summary>
    /// Wartość netto całkowita (ilość × cena jednostkowa netto)
    /// Automatycznie przeliczana gdy zmieni się Quantity lub Margin
    /// </summary>
    public decimal TotalNet => SalePriceNet * Quantity;
    
    /// <summary>
    /// Kwota VAT
    /// Automatycznie przeliczana
    /// </summary>
    public decimal VatAmount => TotalNet * (VatRate / 100m);
    
    /// <summary>
    /// Wartość brutto całkowita
    /// Automatycznie przeliczana
    /// </summary>
    public decimal TotalGross => TotalNet + VatAmount;

    /// <summary>
    /// OPTYMALIZACJA: Helper do zgrupowanych notyfikacji
    /// </summary>
    private void OnPropertiesChanged(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            OnPropertyChanged(name);
        }
    }
}
