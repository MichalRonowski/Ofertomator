using Ofertomator.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ofertomator.Services;

/// <summary>
/// Serwis generowania PDF z ofertami przy użyciu QuestPDF
/// </summary>
public class PdfGeneratorService : IPdfService
{
    private const string AccentColor = "#C8102E"; // Czerwony kolor akcentu
    private const string HeaderBackground = "#F5F5F5";
    private const string ZebraGray = "#F9F9F9";

    /// <summary>
    /// Generuje PDF z ofertą handlową
    /// </summary>
    public Task GenerateOfferPdfAsync(IEnumerable<SavedOfferItem> items, BusinessCard businessCard, string filePath, string offerName, DateTime offerDate, IEnumerable<string>? categoryOrder = null)
    {
        return Task.Run(() =>
        {
            var itemsList = items.ToList();
            var orderList = categoryOrder?.ToList();

            // Grupowanie produktów według kategorii i sortowanie po własnej kolejności lub alfabetycznie
            var groupedByCategory = itemsList
                .GroupBy(x => x.CategoryName ?? "Bez kategorii");

            List<IGrouping<string, SavedOfferItem>> sortedGroups;
            
            if (orderList != null && orderList.Count > 0)
            {
                // Sortuj według podanej kolejności
                var orderDict = orderList
                    .Select((name, index) => new { name, index })
                    .ToDictionary(x => x.name, x => x.index);
                
                sortedGroups = groupedByCategory
                    .OrderBy(g => orderDict.TryGetValue(g.Key, out var order) ? order : 9999)
                    .ToList();
            }
            else
            {
                // Sortuj alfabetycznie
                sortedGroups = groupedByCategory.OrderBy(g => g.Key).ToList();
            }

            // Generowanie dokumentu
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    
                    // Ustaw czcionkę systemową dla obsługi polskich znaków
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, businessCard, offerName, offerDate));
                    page.Content().Element(c => ComposeContent(c, sortedGroups));
                    page.Footer().Element(ComposeFooter);
                });
            })
            .GeneratePdf(filePath);
        });
    }

    /// <summary>
    /// Nagłówek strony: Logo + Dane firmy + Dane kontaktowe
    /// </summary>
    private void ComposeHeader(IContainer container, BusinessCard businessCard, string offerName, DateTime offerDate)
    {
        container.Column(column =>
        {
            // Górna sekcja: Logo i Tytuł
            column.Item().Row(row =>
            {
                // Lewa strona: Logo + Nazwa Firmy
                row.RelativeItem(2).Column(col =>
                {
                    // Logo w lewym górnym rogu
                    var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                    if (System.IO.File.Exists(logoPath))
                    {
                        col.Item().Width(250).Height(140).Image(logoPath);
                    }
                    else
                    {
                        col.Item().Width(250).Height(140).Border(1).BorderColor(Colors.Grey.Lighten1);
                    }
                    
                    col.Item().PaddingTop(5).Text(businessCard.Company ?? "Moja Firma")
                        .FontSize(18)
                        .Bold()
                        .FontColor(AccentColor);
                });

                // Prawa strona: OFERTA HANDLOWA + Data + Kontakt
                row.RelativeItem(2).Column(col =>
                {
                    col.Item().AlignRight().Text(offerName.ToUpper())
                        .FontSize(16)
                        .Bold()
                        .FontColor(AccentColor);

                    col.Item().AlignRight().PaddingTop(5).Text($"Data: {offerDate:dd.MM.yyyy}");

                    col.Item().AlignRight().PaddingTop(10).Column(contactCol =>
                    {
                        if (!string.IsNullOrWhiteSpace(businessCard.FullName))
                            contactCol.Item().Text($"Kontakt: {businessCard.FullName}").FontSize(9);
                        
                        if (!string.IsNullOrWhiteSpace(businessCard.Phone))
                            contactCol.Item().Text($"Tel: {businessCard.Phone}").FontSize(9);
                        
                        if (!string.IsNullOrWhiteSpace(businessCard.Email))
                            contactCol.Item().Text($"Email: {businessCard.Email}").FontSize(9);
                    });
                });
            });

            // Linia separująca
            column.Item().PaddingTop(10).LineHorizontal(2).LineColor(AccentColor);
        });
    }

    /// <summary>
    /// Zawartość dokumentu: Tabele produktów pogrupowane według kategorii
    /// </summary>
    private void ComposeContent(IContainer container, List<IGrouping<string, SavedOfferItem>> groupedByCategory)
    {
        container.PaddingTop(20).Column(column =>
        {
            // Dla każdej kategorii osobna tabela
            foreach (var categoryGroup in groupedByCategory)
            {
                column.Item().Element(c => ComposeCategorySection(c, categoryGroup));
            }
        });
    }

    /// <summary>
    /// Sekcja kategorii: Nagłówek kategorii + Tabela produktów
    /// </summary>
    private void ComposeCategorySection(IContainer container, IGrouping<string, SavedOfferItem> categoryGroup)
    {
        container.Column(column =>
        {
            // Nagłówek kategorii (czerwony pasek)
            column.Item().Background(AccentColor).Padding(8).Text(categoryGroup.Key)
                .FontSize(12)
                .Bold()
                .FontColor(Colors.White);

            // Tabela produktów w kategorii
            column.Item().PaddingTop(5).Table(table =>
            {
                // Definicja kolumn
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(5); // Nazwa (szeroka)
                    columns.RelativeColumn(2); // Cena Netto
                    columns.RelativeColumn(1.5f); // Jednostka
                    columns.RelativeColumn(1.5f); // VAT %
                    columns.RelativeColumn(2); // Cena Brutto
                });

                // Nagłówek tabeli
                table.Header(header =>
                {
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Nazwa produktu").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Cena Netto").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Jednostka").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("VAT %").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Cena Brutto").Bold().FontSize(9);
                });

                // Wiersze z produktami (zebra styling)
                int rowIndex = 0;
                foreach (var item in categoryGroup)
                {
                    var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                    rowIndex++;

                    table.Cell().Background(bgColor).Padding(5).Text(item.DisplayName ?? "-");
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{item.SalePriceNet:N2} zł");
                    table.Cell().Background(bgColor).Padding(5).AlignCenter().Text($"zł/{item.Unit ?? "szt."}");
                    table.Cell().Background(bgColor).Padding(5).AlignCenter().Text($"{item.VatRate:F0}%");
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{item.SalePriceGross:N2} zł").Bold();
                }
            });

            // Odstęp między kategoriami
            column.Item().PaddingBottom(15);
        });
    }

    /// <summary>
    /// Stopka: Numeracja stron + informacja o aplikacji
    /// </summary>
    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(text =>
            {
                text.Span("Wygenerowano w aplikacji ").FontSize(8).FontColor(Colors.Grey.Medium);
                text.Span("Ofertomator 2.0").FontSize(8).FontColor(Colors.Grey.Medium).Italic();
            });

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Strona ").FontSize(8);
                text.CurrentPageNumber().FontSize(8);
                text.Span(" z ").FontSize(8);
                text.TotalPages().FontSize(8);
            });
        });
    }
}
