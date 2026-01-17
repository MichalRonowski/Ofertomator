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
    /// Marża w procentach - EDYTOWALNA przez użytkownika
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SalePriceNet))]
    [NotifyPropertyChangedFor(nameof(SalePriceGross))]
    [NotifyPropertyChangedFor(nameof(TotalNet))]
    [NotifyPropertyChangedFor(nameof(VatAmount))]
    [NotifyPropertyChangedFor(nameof(TotalGross))]
    private decimal _margin = 0m;
    
    /// <summary>
    /// Ilość (domyślnie 1.0) - EDYTOWALNA przez użytkownika
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalNet))]
    [NotifyPropertyChangedFor(nameof(VatAmount))]
    [NotifyPropertyChangedFor(nameof(TotalGross))]
    private decimal _quantity = 1m;
    
    /// <summary>
    /// Kalkulowana cena sprzedaży netto jednostkowa
    /// Automatycznie przeliczana gdy zmieni się Margin
    /// </summary>
    public decimal SalePriceNet => PurchasePriceNet * (1 + Margin / 100m);
    
    /// <summary>
    /// Kalkulowana cena sprzedaży brutto jednostkowa
    /// Automatycznie przeliczana gdy zmieni się Margin lub VatRate
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
