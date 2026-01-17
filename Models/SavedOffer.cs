using System;
using System.Collections.Generic;

namespace Ofertomator.Models;

/// <summary>
/// Zapisana oferta (szablon)
/// </summary>
public class SavedOffer
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
    
    /// <summary>
    /// Niestandardowa kolejność kategorii (JSON array)
    /// </summary>
    public string? CategoryOrder { get; set; }
    
    /// <summary>
    /// Pozycje w ofercie (nie przechowywane w tym obiekcie, ładowane osobno)
    /// </summary>
    public List<SavedOfferItem> Items { get; set; } = new();
}
