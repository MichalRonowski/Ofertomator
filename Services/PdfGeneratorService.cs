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
    public Task GenerateOfferPdfAsync(IEnumerable<SavedOfferItem> items, BusinessCard businessCard, string filePath)
    {
        return Task.Run(() =>
        {
            var itemsList = items.ToList();

            // Grupowanie produktów według kategorii
            var groupedByCategory = itemsList
                .GroupBy(x => x.CategoryName ?? "Bez kategorii")
                .OrderBy(g => g.Key)
                .ToList();

            // Generowanie dokumentu
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    
                    // Ustaw czcionkę systemową dla obsługi polskich znaków
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, businessCard));
                    page.Content().Element(c => ComposeContent(c, groupedByCategory));
                    page.Footer().Element(ComposeFooter);
                });
            })
            .GeneratePdf(filePath);
        });
    }

    /// <summary>
    /// Nagłówek strony: Logo + Dane firmy + Dane kontaktowe
    /// </summary>
    private void ComposeHeader(IContainer container, BusinessCard businessCard)
    {
        container.Column(column =>
        {
            // Górna sekcja: Logo i Tytuł
            column.Item().Row(row =>
            {
                // Lewa strona: Logo + Nazwa Firmy
                row.RelativeItem(2).Column(col =>
                {
                    col.Item().Height(50).Width(50).Placeholder();
                    
                    col.Item().PaddingTop(5).Text(businessCard.Company ?? "Moja Firma")
                        .FontSize(18)
                        .Bold()
                        .FontColor(AccentColor);
                });

                // Prawa strona: OFERTA HANDLOWA + Data + Kontakt
                row.RelativeItem(2).Column(col =>
                {
                    col.Item().AlignRight().Text("OFERTA HANDLOWA")
                        .FontSize(16)
                        .Bold()
                        .FontColor(AccentColor);

                    col.Item().AlignRight().PaddingTop(5).Text($"Data: {DateTime.Now:dd.MM.yyyy}");

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
    /// Zawartość dokumentu: Tabele produktów pogrupowane według kategorii + Podsumowanie
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

            // Podsumowanie na końcu
            column.Item().PaddingTop(20).Element(c => ComposeSummary(c, groupedByCategory.SelectMany(g => g)));
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
                    columns.RelativeColumn(4); // Nazwa (szeroka)
                    columns.RelativeColumn(1); // Ilość
                    columns.RelativeColumn(1); // J.M.
                    columns.RelativeColumn(1.5f); // Cena Netto
                    columns.RelativeColumn(1); // VAT %
                    columns.RelativeColumn(1.5f); // Wartość Brutto
                });

                // Nagłówek tabeli
                table.Header(header =>
                {
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Nazwa produktu").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Ilość").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("J.M.").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Cena Netto").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("VAT %").Bold().FontSize(9);
                    header.Cell().Background(HeaderBackground).Padding(5).Text("Wartość Brutto").Bold().FontSize(9);
                });

                // Wiersze z produktami (zebra styling)
                int rowIndex = 0;
                foreach (var item in categoryGroup)
                {
                    var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                    rowIndex++;

                    table.Cell().Background(bgColor).Padding(5).Text(item.Name ?? "-");
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{item.Quantity:F2}");
                    table.Cell().Background(bgColor).Padding(5).Text(item.Unit ?? "-");
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{item.SalePriceNet:N2} zł");
                    table.Cell().Background(bgColor).Padding(5).AlignCenter().Text($"{item.VatRate:F0}%");
                    table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{item.TotalGross:N2} zł").Bold();
                }
            });

            // Odstęp między kategoriami
            column.Item().PaddingBottom(15);
        });
    }

    /// <summary>
    /// Podsumowanie: Suma Netto, VAT, Brutto
    /// </summary>
    private void ComposeSummary(IContainer container, IEnumerable<SavedOfferItem> allItems)
    {
        var itemsList = allItems.ToList();
        var totalNet = itemsList.Sum(x => x.TotalNet);
        var totalVat = itemsList.Sum(x => x.VatAmount);
        var totalGross = itemsList.Sum(x => x.TotalGross);

        container.AlignRight().Width(250).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            // Suma Netto
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                .Text("Suma Netto:").FontSize(11);
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                .AlignRight().Text($"{totalNet:N2} zł").FontSize(11);

            // Suma VAT
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                .Text("Suma VAT:").FontSize(11);
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                .AlignRight().Text($"{totalVat:N2} zł").FontSize(11);

            // Suma Brutto (wyróżniona)
            table.Cell().Background(AccentColor).Padding(8)
                .Text("Suma do zapłaty:").FontSize(12).Bold().FontColor(Colors.White);
            table.Cell().Background(AccentColor).Padding(8)
                .AlignRight().Text($"{totalGross:N2} zł").FontSize(12).Bold().FontColor(Colors.White);
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
