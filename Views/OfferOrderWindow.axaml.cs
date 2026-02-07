using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Ofertomator.Models;
using Ofertomator.ViewModels;
using System;

namespace Ofertomator.Views;

public partial class OfferOrderWindow : Window
{
    private SavedOfferItem? _draggedItem;

    public OfferOrderWindow()
    {
        InitializeComponent();
        
        Console.WriteLine("[OfferOrderWindow] Initializing...");
        
        // Obsługa selection w zagnieżdżonym ListBox
        this.AddHandler(ListBox.SelectionChangedEvent, OnListBoxSelectionChanged, handledEventsToo: true);
        
        // Obsługa Drag & Drop
        this.AddHandler(DragDrop.DropEvent, OnDrop);
        this.AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Console.WriteLine($"[OfferOrderWindow] OnListBoxSelectionChanged: Added={e.AddedItems.Count}, Removed={e.RemovedItems.Count}");
        
        if (DataContext is not OfferOrderViewModel viewModel)
        {
            Console.WriteLine("[OfferOrderWindow] DataContext is not OfferOrderViewModel");
            return;
        }

        // Obsługa dla obu widoków (płaski i zgrupowany)
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is SavedOfferItem selectedItem)
        {
            Console.WriteLine($"[OfferOrderWindow] Setting SelectedItem to: {selectedItem.DisplayName}");
            viewModel.SelectedItem = selectedItem;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Zaznacz, że możemy upuścić tutaj
        e.DragEffects = DragDropEffects.Move;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        Console.WriteLine($"[OfferOrderWindow] OnDrop: _draggedItem={_draggedItem?.DisplayName ?? "NULL"}");
        
        if (DataContext is not OfferOrderViewModel viewModel || _draggedItem == null)
        {
            Console.WriteLine("[OfferOrderWindow] OnDrop: Early return - no viewModel or draggedItem");
            return;
        }

        // Znajdź target item (nad którym upuszczamy)
        var targetControl = e.Source as Control;
        var targetListBoxItem = targetControl?.FindAncestorOfType<ListBoxItem>();
        
        Console.WriteLine($"[OfferOrderWindow] OnDrop: targetListBoxItem found={targetListBoxItem != null}");
        
        if (targetListBoxItem?.DataContext is SavedOfferItem targetItem)
        {
            Console.WriteLine($"[OfferOrderWindow] OnDrop: targetItem={targetItem.DisplayName}");
            
            var oldIndex = viewModel.WorkingItems.IndexOf(_draggedItem);
            var newIndex = viewModel.WorkingItems.IndexOf(targetItem);

            Console.WriteLine($"[OfferOrderWindow] OnDrop: oldIndex={oldIndex}, newIndex={newIndex}");

            if (oldIndex != -1 && newIndex != -1 && oldIndex != newIndex)
            {
                try
                {
                    Console.WriteLine($"[OfferOrderWindow] OnDrop: Moving from {oldIndex} to {newIndex}");
                    viewModel.WorkingItems.Move(oldIndex, newIndex);
                    viewModel.SelectedItem = _draggedItem; // Zachowaj selekcję
                    viewModel.StatusMessage = $"Przeniesiono: {_draggedItem.DisplayName}";
                    Console.WriteLine("[OfferOrderWindow] OnDrop: Success");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OfferOrderWindow] OnDrop ERROR: {ex}");
                }
            }
        }

        _draggedItem = null;
    }

    // Metoda do inicjacji przeciągania (będzie wywołana z XAML)
    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Console.WriteLine("[OfferOrderWindow] OnPointerPressed");
        
        // Tylko lewy przycisk myszy
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
            sender is Control control && 
            control.DataContext is SavedOfferItem item &&
            DataContext is OfferOrderViewModel viewModel)
        {
            Console.WriteLine($"[OfferOrderWindow] OnPointerPressed: Starting drag for {item.DisplayName}");
            
            _draggedItem = item;
            viewModel.SelectedItem = item;
            
            var dragData = new DataObject();
            dragData.Set("item", item);
            
            _ = DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
        }
        else
        {
            Console.WriteLine("[OfferOrderWindow] OnPointerPressed: Conditions not met for drag");
        }
    }
}
