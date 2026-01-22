using Avalonia.Controls;
using Ofertomator.ViewModels;
using System;

namespace Ofertomator.Views;

public partial class ProductWindow : Window
{
    public ProductWindow()
    {
        InitializeComponent();
        
        // Podpnij zdarzenie gdy DataContext się zmieni
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Subskrybuj zdarzenie zamknięcia
        if (DataContext is ProductEditorViewModel viewModel)
        {
            viewModel.RequestClose += OnRequestClose;
        }
    }

    /// <summary>
    /// Handler zamknięcia okna - wywołany przez ViewModel
    /// </summary>
    private void OnRequestClose(object? sender, EventArgs e)
    {
        Close(true); // true = dialog result = success
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Wyczyść subskrypcje
        if (DataContext is ProductEditorViewModel viewModel)
        {
            viewModel.RequestClose -= OnRequestClose;
        }
        
        DataContextChanged -= OnDataContextChanged;
    }
}
