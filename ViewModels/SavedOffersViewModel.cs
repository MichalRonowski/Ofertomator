using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Ofertomator.Models;
using Ofertomator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla listy zapisanych ofert (historia/szablony)
/// </summary>
public partial class SavedOffersViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly IPdfService _pdfService;
    private readonly Func<Window?> _getMainWindow;
    private readonly MainViewModel _mainViewModel;

    #region Observable Properties

    /// <summary>
    /// Lista zapisanych ofert
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SavedOfferViewModel> _savedOffers = new();

    /// <summary>
    /// Wybrana oferta
    /// </summary>
    [ObservableProperty]
    private SavedOfferViewModel? _selectedOffer;

    /// <summary>
    /// Czy trwa ładowanie danych
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    #endregion

    public SavedOffersViewModel(
        DatabaseService databaseService,
        IPdfService pdfService,
        Func<Window?> getMainWindow,
        MainViewModel mainViewModel)
    {
        _databaseService = databaseService;
        _pdfService = pdfService;
        _getMainWindow = getMainWindow;
        _mainViewModel = mainViewModel;
    }

    #region Commands

    /// <summary>
    /// Wczytaj listę zapisanych ofert
    /// </summary>
    [RelayCommand]
    private async Task LoadOffersAsync()
    {
        try
        {
            IsLoading = true;
            SavedOffers.Clear();

            var offers = await _databaseService.GetSavedOffersAsync();
            
            foreach (var offer in offers)
            {
                var items = (await _databaseService.LoadOfferItemsAsync(offer.Id)).ToList();
                
                var vm = new SavedOfferViewModel
                {
                    Id = offer.Id,
                    Title = offer.Title,
                    CreatedDate = offer.CreatedDate,
                    ItemsCount = items.Count,
                    TotalNet = items.Sum(i => i.Quantity * (i.PurchasePriceNet * (1 + i.Margin / 100m)))
                };
                
                SavedOffers.Add(vm);
            }
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się wczytać listy ofert:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Wczytaj/Edytuj wybraną ofertę
    /// </summary>
    [RelayCommand]
    private async Task LoadOfferAsync()
    {
        if (SelectedOffer == null) return;

        try
        {
            // Pobierz pełne dane oferty
            var offers = await _databaseService.GetSavedOffersAsync();
            var offer = offers.FirstOrDefault(o => o.Id == SelectedOffer.Id);
            
            if (offer == null)
            {
                var errorBox = MessageBoxManager.GetMessageBoxStandard(
                    "Błąd",
                    "Nie znaleziono oferty w bazie danych.",
                    ButtonEnum.Ok,
                    Icon.Error);
                await errorBox.ShowWindowDialogAsync(_getMainWindow());
                return;
            }

            var items = (await _databaseService.LoadOfferItemsAsync(offer.Id)).ToList();

            // Przejdź do generatora i wczytaj ofertę
            _mainViewModel.ShowOfferGenerator();
            await _mainViewModel.OfferGeneratorViewModel.LoadOfferAsync(offer, items);
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się wczytać oferty:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    /// <summary>
    /// Generuj PDF bezpośrednio z listy (bez wchodzenia do kreatora)
    /// </summary>
    [RelayCommand]
    private async Task GeneratePdfAsync()
    {
        if (SelectedOffer == null) return;

        try
        {
            // Pobierz pozycje oferty
            var items = (await _databaseService.LoadOfferItemsAsync(SelectedOffer.Id)).ToList();

            if (items.Count == 0)
            {
                var errorBox = MessageBoxManager.GetMessageBoxStandard(
                    "Błąd",
                    "Oferta nie zawiera żadnych pozycji.",
                    ButtonEnum.Ok,
                    Icon.Warning);
                await errorBox.ShowWindowDialogAsync(_getMainWindow());
                return;
            }

            // Konwertuj SavedOfferItem → SavedOfferItem (już jest właściwy typ)
            var offerItems = items.ToList();

            // Pobierz dane firmy
            var businessCard = await _databaseService.GetBusinessCardAsync();

            // Otwórz dialog zapisu pliku
            var topLevel = TopLevel.GetTopLevel(_getMainWindow());
            if (topLevel == null) return;

            var storageFile = await topLevel.StorageProvider.SaveFilePickerAsync(new()
            {
                Title = "Zapisz ofertę jako PDF",
                SuggestedFileName = $"Oferta_{SelectedOffer.Title.Replace(" ", "_")}_{DateTime.Now:yyyy-MM-dd}.pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (storageFile == null) return;

            // Generuj PDF (z alfabetycznym sortowaniem kategorii)
            await _pdfService.GenerateOfferPdfAsync(offerItems, businessCard, storageFile.Path.LocalPath, null);

            var successBox = MessageBoxManager.GetMessageBoxStandard(
                "Sukces",
                $"Plik PDF został wygenerowany:\n{storageFile.Path.LocalPath}",
                ButtonEnum.Ok,
                Icon.Success);
            await successBox.ShowWindowDialogAsync(_getMainWindow());
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się wygenerować PDF:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    /// <summary>
    /// Usuń wybraną ofertę
    /// </summary>
    [RelayCommand]
    private async Task DeleteOfferAsync()
    {
        if (SelectedOffer == null) return;

        try
        {
            // Potwierdź usunięcie
            var confirmBox = MessageBoxManager.GetMessageBoxStandard(
                "Potwierdź usunięcie",
                $"Czy na pewno chcesz usunąć ofertę '{SelectedOffer.Title}'?\nTej operacji nie można cofnąć.",
                ButtonEnum.YesNo,
                Icon.Question);

            var result = await confirmBox.ShowWindowDialogAsync(_getMainWindow());
            if (result != ButtonResult.Yes) return;

            // Usuń z bazy
            await _databaseService.DeleteOfferAsync(SelectedOffer.Id);

            // Usuń z listy
            SavedOffers.Remove(SelectedOffer);
            SelectedOffer = null;

            var successBox = MessageBoxManager.GetMessageBoxStandard(
                "Sukces",
                "Oferta została usunięta.",
                ButtonEnum.Ok,
                Icon.Success);
            await successBox.ShowWindowDialogAsync(_getMainWindow());
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się usunąć oferty:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(_getMainWindow());
        }
    }

    #endregion
}

/// <summary>
/// ViewModel dla pojedynczej zapisanej oferty (do DataGrid)
/// </summary>
public partial class SavedOfferViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private DateTime _createdDate;

    [ObservableProperty]
    private int _itemsCount;

    [ObservableProperty]
    private decimal _totalNet;

    public string CreatedDateFormatted => CreatedDate.ToString("yyyy-MM-dd HH:mm");
    public string TotalNetFormatted => $"{TotalNet:N2} zł";
}
