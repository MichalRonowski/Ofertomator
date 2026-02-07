using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ofertomator.Models;

/// <summary>
/// Grupa kategorii dla widoku oferty z zapamiętanym stanem rozwinięcia
/// </summary>
public partial class CategoryGroup : ObservableObject
{
    [ObservableProperty]
    private string _categoryName = string.Empty;
    
    [ObservableProperty]
    private List<SavedOfferItem> _items = new();
    
    /// <summary>
    /// Czy kategoria jest rozwinięta (domyślnie tak)
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;
}
