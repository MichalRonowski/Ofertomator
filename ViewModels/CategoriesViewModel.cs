using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Ofertomator.Models;
using Ofertomator.Services;
using Ofertomator.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla widoku zarządzania kategoriami
/// </summary>
public partial class CategoriesViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly Func<Window?> _getMainWindow;

    #region Observable Properties

    /// <summary>
    /// Lista wszystkich kategorii
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    /// <summary>
    /// Wybrana kategoria w DataGrid
    /// </summary>
    [ObservableProperty]
    private Category? _selectedCategory;

    /// <summary>
    /// Flaga ładowania
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Komunikat statusu
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Gotowy";

    /// <summary>
    /// Liczba kategorii
    /// </summary>
    public int CategoriesCount => Categories.Count;

    #endregion

    #region Constructor

    public CategoriesViewModel(DatabaseService databaseService, Func<Window?> getMainWindow)
    {
        _databaseService = databaseService;
        _getMainWindow = getMainWindow;
        _ = LoadCategoriesAsync();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Ładuje kategorie z bazy danych
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Ładowanie kategorii...";

            var categories = await _databaseService.GetCategoriesAsync();
            
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Categories.Clear();
                
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                OnPropertyChanged(nameof(CategoriesCount));
                StatusMessage = $"Załadowano {Categories.Count} kategorii";
            });
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
    /// Komenda: Dodaj nową kategorię
    /// </summary>
    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        try
        {
            var viewModel = new CategoryEditorViewModel();
            var window = new CategoryWindow(viewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var mainWindow = _getMainWindow?.Invoke();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
            else
            {
                window.Show();
            }

            // Jeśli zapisano
            if (viewModel.DialogResult)
            {
                IsBusy = true;
                StatusMessage = "Dodawanie kategorii...";

                var category = viewModel.GetCategory();
                var id = await _databaseService.AddCategoryAsync(category);

                if (id > 0)
                {
                    category.Id = id;
                    Categories.Add(category);
                    OnPropertyChanged(nameof(CategoriesCount));
                    StatusMessage = $"Dodano kategorię: {category.Name}";
                }
                else
                {
                    StatusMessage = "Błąd dodawania kategorii";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się dodać kategorii:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Komenda: Edytuj wybraną kategorię
    /// </summary>
    [RelayCommand]
    private async Task EditCategoryAsync()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Wybierz kategorię do edycji";
            return;
        }

        // Sprawdź czy to nie "Bez kategorii"
        if (SelectedCategory.Name == "Bez kategorii")
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Edycja niemożliwa",
                "Nie można edytować domyślnej kategorii 'Bez kategorii'.",
                ButtonEnum.Ok,
                Icon.Warning);
            await box.ShowAsync();
            return;
        }

        try
        {
            var viewModel = new CategoryEditorViewModel(SelectedCategory);
            var window = new CategoryWindow(viewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var mainWindow = _getMainWindow?.Invoke();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
            else
            {
                window.Show();
            }

            // Jeśli zapisano
            if (viewModel.DialogResult)
            {
                IsBusy = true;
                StatusMessage = "Aktualizowanie kategorii...";

                var updatedCategory = viewModel.GetCategory();
                var success = await _databaseService.UpdateCategoryAsync(updatedCategory);

                if (success)
                {
                    // Zaktualizuj w kolekcji
                    var index = Categories.IndexOf(SelectedCategory);
                    if (index >= 0)
                    {
                        Categories[index] = updatedCategory;
                    }
                    StatusMessage = $"Zaktualizowano kategorię: {updatedCategory.Name}";
                }
                else
                {
                    StatusMessage = "Błąd aktualizacji kategorii";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się edytować kategorii:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Komenda: Usuń wybraną kategorię
    /// </summary>
    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "Wybierz kategorię do usunięcia";
            return;
        }

        // Sprawdź czy to nie "Bez kategorii"
        if (SelectedCategory.Name == "Bez kategorii")
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Usuwanie niemożliwe",
                "Nie można usunąć domyślnej kategorii 'Bez kategorii'.",
                ButtonEnum.Ok,
                Icon.Warning);
            await box.ShowAsync();
            return;
        }

        try
        {
            // Sprawdź liczbę produktów w kategorii
            var productCount = await _databaseService.GetProductsCountByCategoryAsync(SelectedCategory.Id);
            
            if (productCount > 0)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Usuwanie niemożliwe",
                    $"Nie można usunąć kategorii '{SelectedCategory.Name}'.\n\n" +
                    $"Kategoria zawiera {productCount} produktów.\n" +
                    $"Najpierw usuń lub przenieś produkty do innej kategorii.",
                    ButtonEnum.Ok,
                    Icon.Warning);
                await box.ShowAsync();
                return;
            }

            // Potwierdzenie usunięcia
            var confirmBox = MessageBoxManager.GetMessageBoxStandard(
                "Potwierdzenie",
                $"Czy na pewno chcesz usunąć kategorię '{SelectedCategory.Name}'?",
                ButtonEnum.YesNo,
                Icon.Question);
            
            var result = await confirmBox.ShowAsync();
            
            if (result == ButtonResult.Yes)
            {
                IsBusy = true;
                StatusMessage = "Usuwanie kategorii...";

                var success = await _databaseService.DeleteCategoryAsync(SelectedCategory.Id);

                if (success)
                {
                    Categories.Remove(SelectedCategory);
                    OnPropertyChanged(nameof(CategoriesCount));
                    SelectedCategory = null;
                    StatusMessage = "Kategoria usunięta";
                }
                else
                {
                    StatusMessage = "Błąd usuwania kategorii";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "Błąd",
                $"Nie udało się usunąć kategorii:\n{ex.Message}",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Komenda: Odśwież listę kategorii
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadCategoriesAsync();
    }

    #endregion
}
