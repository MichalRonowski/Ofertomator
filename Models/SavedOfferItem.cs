using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ofertomator.Models;

/// <summary>
/// Pozycja w zapisanej ofercie z live kalkulacjami
/// Dziedziczy po ObservableObject dla reaktywności UI
/// </summary>
public partial class SavedOfferItem : ObservableObject
{
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
    private string _name = string.Empty;
    
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
                OnPropertyChanged(nameof(SalePriceNet));
                OnPropertyChanged(nameof(SalePriceGross));
                OnPropertyChanged(nameof(TotalNet));
                OnPropertyChanged(nameof(VatAmount));
                OnPropertyChanged(nameof(TotalGross));
                _isUpdatingMargin = false;
            }
        }
    }
    
    /// <summary>
    /// Cena sprzedaży netto jednostkowa - EDYTOWALNA przez użytkownika
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
                    OnPropertyChanged(nameof(Margin));
                }
                OnPropertyChanged(nameof(SalePriceGross));
                OnPropertyChanged(nameof(TotalNet));
                OnPropertyChanged(nameof(VatAmount));
                OnPropertyChanged(nameof(TotalGross));
                _isUpdatingSalePrice = false;
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
}
