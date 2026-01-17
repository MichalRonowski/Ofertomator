using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ofertomator.Services;
using System;
using System.Threading.Tasks;

namespace Ofertomator.ViewModels;

/// <summary>
/// Główny ViewModel aplikacji
/// Zarządza nawigacją i stanem aplikacji
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly IPdfService _pdfService;
    private readonly Func<Avalonia.Controls.Window?> _getMainWindow;

    [ObservableProperty]
    private string _title = "Ofertomator 2.0";

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    /// <summary>
    /// ViewModel generatora ofert (współdzielony dla całej aplikacji)
    /// </summary>
    public OfferGeneratorViewModel OfferGeneratorViewModel { get; }

    public MainViewModel(DatabaseService databaseService, IPdfService pdfService, Func<Avalonia.Controls.Window?> getMainWindow)
    {
        _databaseService = databaseService;
        _pdfService = pdfService;
        _getMainWindow = getMainWindow;

        // Stwórz współdzielony ViewModel generatora ofert
        OfferGeneratorViewModel = new OfferGeneratorViewModel(_databaseService, _pdfService, _getMainWindow);

        InitializeAsync();
    }

    /// <summary>
    /// Inicjalizacja asynchroniczna (nie blokuje UI)
    /// </summary>
    private async void InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Inicjalizacja bazy danych...";

        try
        {
            await Task.Run(async () =>
            {
                await _databaseService.InitializeDatabaseAsync();
            });

            StatusMessage = "Gotowy";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Błąd inicjalizacji: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Nawiguje do widoku zarządzania produktami
    /// </summary>
    [RelayCommand]
    private void ShowProducts()
    {
        CurrentView = new ProductsViewModel(_databaseService, _getMainWindow);
        StatusMessage = "Zarządzanie produktami";
    }

    /// <summary>
    /// Nawiguje do generatora ofert
    /// </summary>
    [RelayCommand]
    public void ShowOfferGenerator()
    {
        CurrentView = OfferGeneratorViewModel;
        StatusMessage = "Generator ofert";
    }

    /// <summary>
    /// Nawiguje do listy zapisanych ofert
    /// </summary>
    [RelayCommand]
    private void ShowSavedOffers()
    {
        CurrentView = new SavedOffersViewModel(_databaseService, _pdfService, _getMainWindow, this);
        StatusMessage = "Zapisane oferty";
    }

    /// <summary>
    /// Nawiguje do zarządzania kategoriami
    /// </summary>
    [RelayCommand]
    private void ShowCategories()
    {
        CurrentView = new CategoriesViewModel(_databaseService, _getMainWindow);
        StatusMessage = "Zarządzanie kategoriami";
    }

    /// <summary>
    /// Nawiguje do edycji wizytyówki firmy
    /// </summary>
    [RelayCommand]
    private void ShowBusinessCard()
    {
        CurrentView = new BusinessCardViewModel(_databaseService);
        StatusMessage = "Moja firma";
    }

    /// <summary>
    /// Nawiguje do importu produktów z CSV
    /// </summary>
    [RelayCommand]
    private void ShowImport()
    {
        CurrentView = new ImportViewModel(_databaseService, _getMainWindow);
        StatusMessage = "Import produktów";
    }

    /// <summary>
    /// Nawiguje do ekranu głównego (welcome screen)
    /// </summary>
    [RelayCommand]
    private void ShowHome()
    {
        CurrentView = null;
        StatusMessage = "Gotowy";
    }

    /// <summary>
    /// Ustawia komunikat statusu (dla informacji zwrotnej użytkownika)
    /// </summary>
    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    /// <summary>
    /// Pokazuje ekran ładowania
    /// </summary>
    public void ShowLoading(string message = "Ładowanie...")
    {
        IsLoading = true;
        StatusMessage = message;
    }

    /// <summary>
    /// Ukrywa ekran ładowania
    /// </summary>
    public void HideLoading()
    {
        IsLoading = false;
        StatusMessage = "Gotowy";
    }
}
