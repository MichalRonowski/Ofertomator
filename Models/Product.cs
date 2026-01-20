using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ofertomator.Models;

/// <summary>
/// Produkt w bazie danych
/// </summary>
public partial class Product : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;
    public int Id { get; set; }
    
    /// <summary>
    /// Kod produktu (opcjonalny, np. SKU)
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// Nazwa produktu (wymagana)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Jednostka miary (domyślnie "szt.")
    /// </summary>
    public string Unit { get; set; } = "szt.";
    
    /// <summary>
    /// Cena zakupu netto - UWAGA: używamy decimal dla precyzji finansowej
    /// </summary>
    public decimal PurchasePriceNet { get; set; } = 0m;
    
    /// <summary>
    /// Data ostatniej aktualizacji ceny
    /// </summary>
    public DateTime? PriceUpdateDate { get; set; }
    
    /// <summary>
    /// Stawka VAT w procentach (domyślnie 23%)
    /// </summary>
    public decimal VatRate { get; set; } = 23m;
    
    /// <summary>
    /// ID kategorii (FK do Categories)
    /// </summary>
    public int CategoryId { get; set; }
    
    /// <summary>
    /// Obiekt kategorii (nie przechowywany w DB, ładowany przez JOIN)
    /// </summary>
    public Category? Category { get; set; }
}
