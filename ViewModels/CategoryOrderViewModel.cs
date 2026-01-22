using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ofertomator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla okna ustawiania kolejności kategorii
/// </summary>
public partial class CategoryOrderViewModel : ViewModelBase
{
    #region Observable Properties

    /// <summary>
    /// Lista kategorii z ich kolejnością
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CategoryOrderItem> _orderedCategories = new();

    /// <summary>
    /// Wybrana kategoria
    /// </summary>
    [ObservableProperty]
    private CategoryOrderItem? _selectedCategory;

    #endregion

    #region Properties

    /// <summary>
    /// Czy użytkownik zaakceptował zmiany
    /// </summary>
    public bool DialogResult { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Event wywoływany gdy okno powinno się zamknąć
    /// </summary>
    public event EventHandler? RequestClose;

    #endregion

    #region Constructor

    public CategoryOrderViewModel(IEnumerable<string> categoryNames)
    {
        var order = 1;
        foreach (var name in categoryNames)
        {
            OrderedCategories.Add(new CategoryOrderItem
            {
                Name = name,
                Order = order++
            });
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Przesuń wybraną kategorię w górę
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (SelectedCategory == null) return;

        var index = OrderedCategories.IndexOf(SelectedCategory);
        if (index > 0)
        {
            OrderedCategories.Move(index, index - 1);
            UpdateOrderNumbers();
        }
    }

    private bool CanMoveUp() => SelectedCategory != null && OrderedCategories.IndexOf(SelectedCategory) > 0;

    /// <summary>
    /// Przesuń wybraną kategorię w dół
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (SelectedCategory == null) return;

        var index = OrderedCategories.IndexOf(SelectedCategory);
        if (index < OrderedCategories.Count - 1)
        {
            OrderedCategories.Move(index, index + 1);
            UpdateOrderNumbers();
        }
    }

    private bool CanMoveDown() => SelectedCategory != null && OrderedCategories.IndexOf(SelectedCategory) < OrderedCategories.Count - 1;

    /// <summary>
    /// Zastosuj zmiany i zamknij
    /// </summary>
    [RelayCommand]
    private void Apply()
    {
        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Anuluj i zamknij
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Aktualizuje numery kolejności po przesunięciu
    /// </summary>
    private void UpdateOrderNumbers()
    {
        for (int i = 0; i < OrderedCategories.Count; i++)
        {
            OrderedCategories[i].Order = i + 1;
        }
        
        // Odśwież stany przycisków
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Zwraca uporządkowaną listę nazw kategorii
    /// </summary>
    public List<string> GetOrderedCategoryNames()
    {
        return OrderedCategories.Select(c => c.Name).ToList();
    }

    #endregion

    /// <summary>
    /// Handler zmiany wybranej kategorii
    /// </summary>
    partial void OnSelectedCategoryChanged(CategoryOrderItem? value)
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// Item reprezentujący kategorię z jej kolejnością
/// </summary>
public partial class CategoryOrderItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _order;
}
