using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Ofertomator.Helpers;
using Ofertomator.Models;
using Ofertomator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla importu produktów z plików CSV
/// </summary>
public partial class ImportViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly Func<Window?> _getMainWindow;

    #region Observable Properties

    /// <summary>
    /// Ścieżka do wybranego pliku
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private string _filePath = string.Empty;

    /// <summary>
    /// Nagłówki z pliku CSV (dostępne do mapowania)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _csvHeaders = new();

    /// <summary>
    /// Dane podglądu (pierwsze 5 wierszy)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Dictionary<string, string>> _previewData = new();

    /// <summary>
    /// Wykryty separator
    /// </summary>
    [ObservableProperty]
    private char _separator = ';';

    /// <summary>
    /// Całkowita liczba wierszy w pliku
    /// </summary>
    [ObservableProperty]
    private int _totalRows;

    /// <summary>
    /// Czy trwa import
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private bool _isImporting;

    /// <summary>
    /// Postęp importu (0-100%)
    /// </summary>
    [ObservableProperty]
    private int _importProgress;

    /// <summary>
    /// Status importu (np. "Przetworzono 50/1200")
    /// </summary>
    [ObservableProperty]
    private string _importStatus = string.Empty;

    #endregion

    #region Mapping Properties

    [ObservableProperty]
    private string? _selectedCodeColumn;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private string? _selectedNameColumn;

    [ObservableProperty]
    private string? _selectedPriceColumn;

    [ObservableProperty]
    private string? _selectedVatColumn;

    [ObservableProperty]
    private string? _selectedUnitColumn;

    #endregion

    private CsvParseResult? _parseResult;

    public ImportViewModel(DatabaseService databaseService, Func<Window?> getMainWindow)
    {
        _databaseService = databaseService;
        _getMainWindow = getMainWindow;
    }

    #region Commands

    /// <summary>
    /// Wybierz plik CSV
    /// </summary>
    [RelayCommand]
    private async Task SelectFileAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(_getMainWindow());
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Wybierz plik CSV",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Pliki CSV") { Patterns = new[] { "*.csv", "*.txt" } },
                    new FilePickerFileType("Wszystkie pliki") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count == 0) return;

            FilePath = files[0].Path.LocalPath;

            // Parsuj plik
            _parseResult = CsvParser.ParseFile(FilePath, 5);
            
            // Aktualizuj UI
            Separator = _parseResult.Separator;
            TotalRows = _parseResult.TotalRows;
            CsvHeaders.Clear();
            foreach (var header in _parseResult.Headers)
            {
                CsvHeaders.Add(header);
            }

            PreviewData.Clear();
            foreach (var row in _parseResult.PreviewData)
            {
                PreviewData.Add(row);
            }

            // Auto-mapowanie kolumn
            AutoMapColumns();

            var msgBox = MessageBoxManager.GetMessageBoxStandard(
                "Sukces",
                $"Wczytano plik CSV:\n- Wierszy: {TotalRows}\n- Separator: '{Separator}'\n- Kolumn: {CsvHeaders.Count}",
                ButtonEnum.Ok,
                Icon.Info);
            await msgBox.ShowWindowDialogAsync(_getMainWindow());
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się wczytać pliku:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    /// <summary>
    /// Importuj produkty
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedNameColumn))
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                "Musisz zmapować kolumnę 'Nazwa' - jest wymagana!",
                ButtonEnum.Ok,
                Icon.Warning);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
            return;
        }

        try
        {
            IsImporting = true;
            ImportProgress = 0;
            ImportStatus = "Rozpoczynam import...";

            // Pobierz ID kategorii "Bez kategorii"
            var categories = await _databaseService.GetCategoriesAsync();
            var defaultCategory = categories.FirstOrDefault(c => c.Name == "Bez kategorii");
            if (defaultCategory == null)
            {
                throw new InvalidOperationException("Nie znaleziono domyślnej kategorii 'Bez kategorii'.");
            }

            // Wczytaj wszystkie wiersze
            var allRows = CsvParser.ParseAllRows(FilePath, Separator);
            var totalCount = allRows.Count;
            var processed = 0;

            // Przygotuj produkty
            var products = new List<Product>();

            foreach (var row in allRows)
            {
                try
                {
                    var product = new Product
                    {
                        Code = GetCellValue(row, SelectedCodeColumn),
                        Name = GetCellValue(row, SelectedNameColumn) ?? "Brak nazwy",
                        Unit = GetCellValue(row, SelectedUnitColumn) ?? "szt.",
                        PurchasePriceNet = DataParser.ParsePrice(GetCellValue(row, SelectedPriceColumn) ?? "0"),
                        VatRate = DataParser.ParseVatRate(GetCellValue(row, SelectedVatColumn) ?? "23"),
                        PriceUpdateDate = DateTime.Now,
                        CategoryId = defaultCategory.Id // Użyj ID kategorii "Bez kategorii"
                    };

                    products.Add(product);
                }
                catch
                {
                    // Pomiń błędne wiersze
                }

                processed++;
                ImportProgress = (int)((processed / (double)totalCount) * 100);
                ImportStatus = $"Przygotowano {processed}/{totalCount}";
            }

            // Wykonaj import w tle
            ImportStatus = "Importuję do bazy danych...";
            var result = await Task.Run(async () => 
                await _databaseService.ImportProductsBatchAsync(products, updateExisting: true));

            IsImporting = false;
            ImportProgress = 100;
            ImportStatus = $"Zakończono! Dodano: {result.Added}, Zaktualizowano: {result.Updated}";

            var successBox = MessageBoxManager.GetMessageBoxStandard(
                "Sukces",
                $"Import zakończony pomyślnie!\n\nDodano: {result.Added}\nZaktualizowano: {result.Updated}",
                ButtonEnum.Ok,
                Icon.Success);
            await successBox.ShowWindowDialogAsync(_getMainWindow());
        }
        catch (Exception ex)
        {
            IsImporting = false;
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Błąd podczas importu:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Auto-mapowanie kolumn na podstawie nazw
    /// </summary>
    private void AutoMapColumns()
    {
        foreach (var header in CsvHeaders)
        {
            var lower = header.ToLower();

            // Kod
            if ((lower.Contains("kod") || lower.Contains("index") || lower.Contains("sku")) && SelectedCodeColumn == null)
            {
                SelectedCodeColumn = header;
            }
            // Nazwa
            else if ((lower.Contains("nazwa") || lower.Contains("name") || lower.Contains("produkt")) && SelectedNameColumn == null)
            {
                SelectedNameColumn = header;
            }
            // Cena
            else if ((lower.Contains("cena") || lower.Contains("price") || lower.Contains("zakup")) && SelectedPriceColumn == null)
            {
                SelectedPriceColumn = header;
            }
            // VAT
            else if ((lower.Contains("vat") || lower.Contains("stawka")) && SelectedVatColumn == null)
            {
                SelectedVatColumn = header;
            }
            // Jednostka
            else if ((lower.Contains("jednostka") || lower.Contains("unit") || lower.Contains("jm")) && SelectedUnitColumn == null)
            {
                SelectedUnitColumn = header;
            }
        }
    }

    /// <summary>
    /// Pobierz wartość komórki (bezpieczne)
    /// </summary>
    private string? GetCellValue(Dictionary<string, string> row, string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return null;
        return row.TryGetValue(columnName, out var value) ? value : null;
    }

    /// <summary>
    /// Parsuj decimal z bezpiecznym fallback
    /// </summary>
    private decimal ParseDecimal(string? value, decimal defaultValue = 0m)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        
        // Zamień przecinek na kropkę
        value = value.Replace(',', '.');
        
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out var result) 
            ? result 
            : defaultValue;
    }

    /// <summary>
    /// Czy można importować (nazwa zmapowana)
    /// </summary>
    public bool CanImport => !string.IsNullOrWhiteSpace(SelectedNameColumn) && !IsImporting && !string.IsNullOrWhiteSpace(FilePath);

    #endregion
}
