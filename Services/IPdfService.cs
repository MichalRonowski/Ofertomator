using Ofertomator.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ofertomator.Services;

/// <summary>
/// Interfejs serwisu generowania PDF
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generuje PDF z ofertą handlową
    /// </summary>
    /// <param name="items">Produkty w ofercie</param>
    /// <param name="businessCard">Dane firmy/kontaktu</param>
    /// <param name="filePath">Ścieżka docelowa pliku PDF</param>
    /// <param name="offerName">Nazwa oferty</param>
    /// <param name="offerDate">Data oferty</param>
    /// <param name="categoryOrder">Kolejność kategorii (null = alfabetycznie)</param>
    Task GenerateOfferPdfAsync(IEnumerable<SavedOfferItem> items, BusinessCard businessCard, string filePath, string offerName, DateTime offerDate, IEnumerable<string>? categoryOrder = null);
}
