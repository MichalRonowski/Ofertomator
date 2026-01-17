using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Ofertomator.Models;
using Ofertomator.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla edycji wizytówki (dane firmy w nagłówku PDF)
/// </summary>
public partial class BusinessCardViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;

    #region Observable Properties

    [ObservableProperty]
    private string _company = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    #endregion

    #region Constructor

    public BusinessCardViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _ = InitializeAsync();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Inicjalizacja - pobierz dane wizytówki z bazy
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Ładowanie danych...";

            var businessCard = await _databaseService.GetBusinessCardAsync();
            
            if (businessCard != null)
            {
                Company = businessCard.Company;
                FullName = businessCard.FullName;
                Phone = businessCard.Phone;
                Email = businessCard.Email;
                StatusMessage = "Dane załadowane";
            }
            else
            {
                StatusMessage = "Brak danych - wprowadź informacje o firmie";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd ładowania: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Komenda: Zapisz zmiany wizytówki
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Walidacja
        if (string.IsNullOrWhiteSpace(Company))
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Błąd walidacji",
                "Nazwa firmy jest wymagana.",
                ButtonEnum.Ok,
                Icon.Warning);
            await box.ShowAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Błąd walidacji",
                "Podaj prawidłowy adres email.",
                ButtonEnum.Ok,
                Icon.Warning);
            await box.ShowAsync();
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Zapisywanie...";

            var businessCard = new BusinessCard
            {
                Id = 1, // Zawsze jeden rekord
                Company = Company.Trim(),
                FullName = FullName.Trim(),
                Phone = Phone.Trim(),
                Email = Email.Trim()
            };

            await _databaseService.SaveBusinessCardAsync(businessCard);

            StatusMessage = "Dane zapisane pomyślnie!";

            var successBox = MessageBoxManager.GetMessageBoxStandard(
                "Sukces",
                "Dane firmy zostały zapisane.\nBędą widoczne w nagłówku generowanych ofert PDF.",
                ButtonEnum.Ok,
                Icon.Success);
            await successBox.ShowAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd zapisu: {ex.Message}";
            
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się zapisać danych:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}
