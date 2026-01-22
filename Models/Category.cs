using System;

namespace Ofertomator.Models;

/// <summary>
/// Kategoria produktów z domyślną marżą
/// </summary>
public class Category
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Domyślna marża w procentach
    /// </summary>
    public decimal DefaultMargin { get; set; } = 0m;
    
    /// <summary>
    /// Kolejność wyświetlania (dla sortowania w ofercie i PDF)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}
