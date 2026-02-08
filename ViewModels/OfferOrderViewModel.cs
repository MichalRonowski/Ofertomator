using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ofertomator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla okna zarządzania kolejnością produktów w ofercie
/// </summary>
public partial class OfferOrderViewModel : ViewModelBase
{
    private static string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "OfferOrderLogs.txt");
    
    private static void Log(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            File.AppendAllText(LogFile, $"[{timestamp}] {message}\n");
        }
        catch { }
    }
    private readonly Action<List<SavedOfferItem>> _onApply;
    private readonly Action? _onCancel;

    /// <summary>
    /// Kopia robocza produktów z oferty (aby nie modyfikować oryginalnej listy do momentu zatwierdzenia)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SavedOfferItem> _workingItems = new();

    /// <summary>
    /// Czy grupować po kategoriach
    /// </summary>
    [ObservableProperty]
    private bool _groupByCategory = true;

    /// <summary>
    /// Produkty pogrupowane według kategorii (dla widoku z grupowaniem)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CategoryGroup> _groupedItems = new();

    /// <summary>
    /// Zaznaczony item (dla operacji strzałkami)
    /// </summary>
    [ObservableProperty]
    private SavedOfferItem? _selectedItem;

    /// <summary>
    /// Zaznaczona grupa kategorii (dla zmiany kolejności kategorii)
    /// </summary>
    [ObservableProperty]
    private CategoryGroup? _selectedGroup;

    [ObservableProperty]
    private string _statusMessage = "Zmień kolejność produktów lub kategorii";

    /// <summary>
    /// Czy są niezapisane zmiany
    /// </summary>
    [ObservableProperty]
    private bool _hasChanges = false;

    private readonly List<SavedOfferItem> _originalOrder;
    
    // Flag zapobiegająca rekursji podczas aktualizacji widoku
    private bool _isUpdatingView = false;

    public OfferOrderViewModel(
        List<SavedOfferItem> offerItems, 
        Action<List<SavedOfferItem>> onApply,
        Action? onCancel = null)
    {
        ArgumentNullException.ThrowIfNull(offerItems);
        
        Log("===== OfferOrderViewModel Constructor START =====");
        Log($"offerItems count: {offerItems.Count}");
        
        try
        {
            _onApply = onApply;
            _onCancel = onCancel;
            Log("Assigned callbacks");

            // Zapisz oryginalną kolejność
            _originalOrder = new List<SavedOfferItem>(offerItems);
            Log($"Saved original order: {_originalOrder.Count} items");

            // Stwórz kopie robocze - używamy bezpośrednio oryginalnych obiektów
            // (zmiany będą widoczne natychmiast, ale ostateczne zatwierdzenie nastąpi po Apply)
            foreach (var item in offerItems)
            {
                WorkingItems.Add(item);
            }
            Log($"Added {WorkingItems.Count} items to WorkingItems");

            // Nasłuchuj zmian w kolekcji
            WorkingItems.CollectionChanged += (_, args) => 
            {
                Log($"CollectionChanged: Action={args.Action}, Count={WorkingItems.Count}");
                HasChanges = true;
                
                // Aktualizuj widok tylko jeśli już nie jesteśmy w trakcie aktualizacji
                if (!_isUpdatingView)
                {
                    UpdateGroupedView();
                }
            };
            Log("Attached CollectionChanged handler");

            UpdateGroupedView();
            Log("Initial UpdateGroupedView completed");
            Log("===== OfferOrderViewModel Constructor END - SUCCESS =====");
        }
        catch (Exception ex)
        {
            Log($"===== OfferOrderViewModel Constructor ERROR: {ex}");
            Log($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Aktualizuj widok zgrupowany po kategoriach
    /// </summary>
    private void UpdateGroupedView()
    {
        Console.WriteLine($"[OfferOrder] UpdateGroupedView START: GroupByCategory={GroupByCategory}, WorkingItems.Count={WorkingItems.Count}, _isUpdatingView={_isUpdatingView}");
        
        if (_isUpdatingView)
        {
            Console.WriteLine("[OfferOrder] UpdateGroupedView: Already updating, skipping");
            return;
        }
        
        if (!GroupByCategory)
        {
            GroupedItems.Clear();
            return;
        }

        try
        {
            _isUpdatingView = true;
            
            // Zapamiętaj stan rozwinięcia kategorii przed aktualizacją
            var expandedStates = GroupedItems.ToDictionary(
                g => g.CategoryName,
                g => g.IsExpanded
            );
            Console.WriteLine($"[OfferOrder] Saved {expandedStates.Count} expansion states");
            
            var grouped = WorkingItems
                .GroupBy(item => item.CategoryName ?? "Bez kategorii")
                .Select(g => new CategoryGroup
                {
                    CategoryName = g.Key,
                    Items = g.ToList(),
                    // Przywróć zapisany stan lub domyślnie rozwinięta dla nowych kategorii
                    IsExpanded = expandedStates.TryGetValue(g.Key, out var isExpanded) ? isExpanded : true
                })
                .ToList();

            Console.WriteLine($"[OfferOrder] Grouped into {grouped.Count} categories");
            
            GroupedItems.Clear();
            foreach (var group in grouped)
            {
                Console.WriteLine($"[OfferOrder] Adding group: {group.CategoryName} with {group.Items.Count} items, IsExpanded={group.IsExpanded}");
                GroupedItems.Add(group);
            }
            
            Console.WriteLine("[OfferOrder] UpdateGroupedView: Completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfferOrder] ERROR in UpdateGroupedView: {ex}");
            StatusMessage = $"Błąd aktualizacji widoku: {ex.Message}";
        }
        finally
        {
            _isUpdatingView = false;
        }
    }

    partial void OnGroupByCategoryChanged(bool value)
    {
        UpdateGroupedView();
        StatusMessage = value 
            ? "Widok zgrupowany - możesz zmieniać kolejność kategorii i produktów w kategoriach"
            : "Widok płaski - zmiana kolejności wszystkich produktów";
    }

    #region Commands - Strzałki (pojedyncze produkty)

    /// <summary>
    /// Przesuń zaznaczony produkt w górę
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        Log("MoveUp called");
        
        if (SelectedItem == null)
        {
            Log("MoveUp: SelectedItem is null");
            return;
        }

        // Zapisz referencję przed operacją (może zostać znullowana podczas UpdateGroupedView)
        var itemToMove = SelectedItem;
        var index = WorkingItems.IndexOf(itemToMove);
        Log($"MoveUp: Current index={index}, Count={WorkingItems.Count}");
        
        if (index > 0)
        {
            Log($"MoveUp: Moving from {index} to {index - 1}");
            try
            {
                _isUpdatingView = true;
                WorkingItems.Move(index, index - 1);
                _isUpdatingView = false;
                
                // Wyzeruj zaznaczenie przed aktualizacją widoku
                SelectedItem = null;
                
                UpdateGroupedView();
                
                // Przywróć zaznaczenie i wymuś aktualizację przycisków
                SelectedItem = itemToMove;
                MoveUpCommand.NotifyCanExecuteChanged();
                MoveDownCommand.NotifyCanExecuteChanged();
                MoveToTopCommand.NotifyCanExecuteChanged();
                MoveToBottomCommand.NotifyCanExecuteChanged();
                
                Log($"SelectedItem restored to: {itemToMove.DisplayName}");
                StatusMessage = $"Przesunięto w górę: {itemToMove.DisplayName}";
            }
            catch (Exception ex)
            {
                _isUpdatingView = false;
                Log($"MoveUp ERROR: {ex}");
                StatusMessage = $"Błąd przesuwania: {ex.Message}";
            }
        }
    }

    private bool CanMoveUp()
    {
        if (SelectedItem == null) return false;
        var index = WorkingItems.IndexOf(SelectedItem);
        return index > 0;
    }

    /// <summary>
    /// Przesuń zaznaczony produkt w dół
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        Log("===== MoveDown START =====");
        
        if (SelectedItem == null)
        {
            Log("MoveDown: SelectedItem is null - RETURN");
            return;
        }

        // Zapisz referencję przed operacją (może zostać znullowana podczas UpdateGroupedView)
        var itemToMove = SelectedItem;
        var index = WorkingItems.IndexOf(itemToMove);
        Log($"SelectedItem={itemToMove.DisplayName}, index={index}, Count={WorkingItems.Count}");
        
        if (index >= 0 && index < WorkingItems.Count - 1)
        {
            Log($"About to move from {index} to {index + 1}");
            try
            {
                _isUpdatingView = true;
                Log("Set _isUpdatingView = true");
                
                WorkingItems.Move(index, index + 1);
                Log("Move completed");
                
                _isUpdatingView = false;
                Log("Set _isUpdatingView = false");
                
                // Wyzeruj zaznaczenie przed aktualizacją widoku
                SelectedItem = null;
                Log("SelectedItem cleared");
                
                UpdateGroupedView();
                Log("UpdateGroupedView called");
                
                // Przywróć zaznaczenie i wymuś aktualizację przycisków
                SelectedItem = itemToMove;
                MoveUpCommand.NotifyCanExecuteChanged();
                MoveDownCommand.NotifyCanExecuteChanged();
                MoveToTopCommand.NotifyCanExecuteChanged();
                MoveToBottomCommand.NotifyCanExecuteChanged();
                
                Log($"SelectedItem restored to: {itemToMove.DisplayName}");
                StatusMessage = $"Przesunięto w dół: {itemToMove.DisplayName}";
                Log("SUCCESS");
            }
            catch (Exception ex)
            {
                _isUpdatingView = false;
                Log($"ERROR: {ex}");
                Log($"Stack trace: {ex.StackTrace}");
                StatusMessage = $"Błąd przesuwania: {ex.Message}";
            }
        }
        else
        {
            Log($"Cannot move - conditions not met (index={index}, Count={WorkingItems.Count})");
        }
        
        Log("===== MoveDown END =====");
    }

    private bool CanMoveDown()
    {
        if (SelectedItem == null) return false;
        var index = WorkingItems.IndexOf(SelectedItem);
        return index >= 0 && index < WorkingItems.Count - 1;
    }

    /// <summary>
    /// Przesuń na sam początek
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveToTop))]
    private void MoveToTop()
    {
        if (SelectedItem == null) return;

        var itemToMove = SelectedItem;
        var index = WorkingItems.IndexOf(itemToMove);
        if (index > 0)
        {
            try
            {
                _isUpdatingView = true;
                WorkingItems.Move(index, 0);
                _isUpdatingView = false;
                
                // Wyzeruj zaznaczenie przed aktualizacją widoku
                SelectedItem = null;
                
                UpdateGroupedView();
                
                // Przywróć zaznaczenie i wymuś aktualizację przycisków
                SelectedItem = itemToMove;
                MoveUpCommand.NotifyCanExecuteChanged();
                MoveDownCommand.NotifyCanExecuteChanged();
                MoveToTopCommand.NotifyCanExecuteChanged();
                MoveToBottomCommand.NotifyCanExecuteChanged();
                
                Log($"SelectedItem restored to: {itemToMove.DisplayName}");
                StatusMessage = $"Przesunięto na początek: {itemToMove.DisplayName}";
            }
            catch (Exception ex)
            {
                _isUpdatingView = false;
                Log($"MoveToTop ERROR: {ex}");
                StatusMessage = $"Błąd przesuwania: {ex.Message}";
            }
        }
    }

    private bool CanMoveToTop()
    {
        if (SelectedItem == null) return false;
        var index = WorkingItems.IndexOf(SelectedItem);
        return index > 0;
    }

    /// <summary>
    /// Przesuń na sam koniec
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveToBottom))]
    private void MoveToBottom()
    {
        if (SelectedItem == null) return;

        var itemToMove = SelectedItem;
        var index = WorkingItems.IndexOf(itemToMove);
        if (index >= 0 && index < WorkingItems.Count - 1)
        {
            try
            {
                _isUpdatingView = true;
                WorkingItems.Move(index, WorkingItems.Count - 1);
                _isUpdatingView = false;
                
                // Wyzeruj zaznaczenie przed aktualizacją widoku
                SelectedItem = null;
                
                UpdateGroupedView();
                
                // Przywróć zaznaczenie i wymuś aktualizację przycisków
                SelectedItem = itemToMove;
                MoveUpCommand.NotifyCanExecuteChanged();
                MoveDownCommand.NotifyCanExecuteChanged();
                MoveToTopCommand.NotifyCanExecuteChanged();
                MoveToBottomCommand.NotifyCanExecuteChanged();
                
                Log($"SelectedItem restored to: {itemToMove.DisplayName}");
                StatusMessage = $"Przesunięto na koniec: {itemToMove.DisplayName}";
            }
            catch (Exception ex)
            {
                _isUpdatingView = false;
                Log($"MoveToBottom ERROR: {ex}");
                StatusMessage = $"Błąd przesuwania: {ex.Message}";
            }
        }
    }

    private bool CanMoveToBottom()
    {
        if (SelectedItem == null) return false;
        var index = WorkingItems.IndexOf(SelectedItem);
        return index >= 0 && index < WorkingItems.Count - 1;
    }

    #endregion

    #region Commands - Kategorie (grupowanie)

    /// <summary>
    /// Przesuń całą kategorię w górę
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveCategoryUp))]
    private void MoveCategoryUp()
    {
        if (SelectedGroup == null) return;

        // Zapisz referencję przed operacją (może zostać znullowana)
        var categoryToMove = SelectedGroup;
        var index = GroupedItems.IndexOf(categoryToMove);
        if (index > 0)
        {
            try
            {
                Log($"MoveCategoryUp: {categoryToMove.CategoryName} from {index} to {index - 1}");
                
                // Zapobiegaj automatycznej aktualizacji widoku - robimy to ręcznie
                _isUpdatingView = true;
                
                // Przenieś grupę w kolekcji GroupedItems
                GroupedItems.Move(index, index - 1);

                // Przebuduj WorkingItems według nowej kolejności kategorii
                RebuildWorkingItemsFromGroups();
                
                _isUpdatingView = false;

                // Zachowaj zaznaczenie kategorii - wymuś aktualizację
                SelectedGroup = null;
                SelectedGroup = categoryToMove;
                MoveCategoryUpCommand.NotifyCanExecuteChanged();
                MoveCategoryDownCommand.NotifyCanExecuteChanged();
                
                Log($"SelectedGroup restored to: {categoryToMove.CategoryName}");

                HasChanges = true;
                StatusMessage = $"Przesunięto kategorię w górę: {categoryToMove.CategoryName}";
                Log("MoveCategoryUp: SUCCESS");
            }
            catch (Exception ex)
            {
                _isUpdatingView = false;
                Log($"MoveCategoryUp ERROR: {ex}");
                StatusMessage = $"Błąd przesuwania kategorii: {ex.Message}";
            }
        }
    }

    private bool CanMoveCategoryUp() => GroupByCategory && 
                                         SelectedGroup != null && 
                                         GroupedItems.IndexOf(SelectedGroup) > 0;

    /// <summary>
    /// Przesuń całą kategorię w dół
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveCategoryDown))]
    private void MoveCategoryDown()
    {
        if (SelectedGroup == null) return;

        // Zapisz referencję przed operacją (może zostać znullowana)
        var categoryToMove = SelectedGroup;
        var index = GroupedItems.IndexOf(categoryToMove);
        if (index < GroupedItems.Count - 1)
        {
            try
            {
                Log($"MoveCategoryDown: {categoryToMove.CategoryName} from {index} to {index + 1}");
                
                // Zapobiegaj automatycznej aktualizacji widoku - robimy to ręcznie
                _isUpdatingView = true;
                
                GroupedItems.Move(index, index + 1);
                RebuildWorkingItemsFromGroups();
                
                _isUpdatingView = false;

                // Zachowaj zaznaczenie kategorii - wymuś aktualizację
                SelectedGroup = null;
                SelectedGroup = categoryToMove;
                MoveCategoryUpCommand.NotifyCanExecuteChanged();
                MoveCategoryDownCommand.NotifyCanExecuteChanged();
                
                Log($"SelectedGroup restored to: {categoryToMove.CategoryName}");

                HasChanges = true;
                StatusMessage = $"Przesunięto kategorię w dół: {categoryToMove.CategoryName}";
                Log("MoveCategoryDown: SUCCESS");
            }
            catch (Exception ex)
            {
                _isUpdatingView = false;
                Log($"MoveCategoryDown ERROR: {ex}");
                StatusMessage = $"Błąd przesuwania kategorii: {ex.Message}";
            }
        }
    }

    private bool CanMoveCategoryDown() => GroupByCategory && 
                                           SelectedGroup != null && 
                                           GroupedItems.IndexOf(SelectedGroup) < GroupedItems.Count - 1;

    /// <summary>
    /// Przebuduj WorkingItems na podstawie aktualnej kolejności grup
    /// </summary>
    private void RebuildWorkingItemsFromGroups()
    {
        var newOrder = new List<SavedOfferItem>();
        
        foreach (var group in GroupedItems)
        {
            newOrder.AddRange(group.Items);
        }

        WorkingItems.Clear();
        foreach (var item in newOrder)
        {
            WorkingItems.Add(item);
        }
    }

    #endregion

    #region Commands - Akcje okna

    /// <summary>
    /// Zastosuj zmiany i zamknij okno
    /// </summary>
    [RelayCommand]
    private void Apply(Window? window)
    {
        _onApply(WorkingItems.ToList());
        window?.Close();
    }

    /// <summary>
    /// Anuluj i zamknij okno
    /// </summary>
    [RelayCommand]
    private void Cancel(Window? window)
    {
        _onCancel?.Invoke();
        window?.Close();
    }

    /// <summary>
    /// Przywróć oryginalną kolejność
    /// </summary>
    [RelayCommand]
    private void ResetToOriginal()
    {
        WorkingItems.Clear();
        foreach (var item in _originalOrder)
        {
            WorkingItems.Add(item);
        }

        HasChanges = false;
        StatusMessage = "Przywrócono oryginalną kolejność";
    }

    /// <summary>
    /// Sortuj alfabetycznie po nazwach produktów
    /// </summary>
    [RelayCommand]
    private void SortAlphabetically()
    {
        var sorted = WorkingItems.OrderBy(i => i.DisplayName).ToList();
        WorkingItems.Clear();
        foreach (var item in sorted)
        {
            WorkingItems.Add(item);
        }

        HasChanges = true;
        StatusMessage = "Posortowano alfabetycznie";
    }

    /// <summary>
    /// Sortuj po kategoriach, potem alfabetycznie
    /// </summary>
    [RelayCommand]
    private void SortByCategory()
    {
        var sorted = WorkingItems
            .OrderBy(i => i.CategoryName ?? "ZZZZZ") // "Bez kategorii" na końcu
            .ThenBy(i => i.DisplayName)
            .ToList();

        WorkingItems.Clear();
        foreach (var item in sorted)
        {
            WorkingItems.Add(item);
        }

        HasChanges = true;
        StatusMessage = "Posortowano według kategorii";
    }

    #endregion

    partial void OnSelectedItemChanged(SavedOfferItem? value)
    {
        Console.WriteLine($"[OfferOrder] OnSelectedItemChanged: {value?.DisplayName ?? "NULL"}");
        
        // Odśwież CanExecute dla komend strzałek
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        MoveToTopCommand.NotifyCanExecuteChanged();
        MoveToBottomCommand.NotifyCanExecuteChanged();
        
        Console.WriteLine($"[OfferOrder] Commands NotifyCanExecuteChanged completed");
    }

    partial void OnSelectedGroupChanged(CategoryGroup? value)
    {
        Console.WriteLine($"[OfferOrder] OnSelectedGroupChanged: {value?.CategoryName ?? "NULL"}");
        
        // Odśwież CanExecute dla komend kategorii
        MoveCategoryUpCommand.NotifyCanExecuteChanged();
        MoveCategoryDownCommand.NotifyCanExecuteChanged();
    }
}
