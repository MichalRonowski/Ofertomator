using Avalonia.Controls;
using Ofertomator.ViewModels;
using System;

namespace Ofertomator.Views;

public partial class ProductWindow : Window
{
    public ProductWindow()
    {
        InitializeComponent();
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();

        // Inicjalizacja ViewModelu (ładowanie kategorii)
        if (DataContext is ProductEditorViewModel viewModel)
        {
            // Subskrybuj zdarzenie zamknięcia
            viewModel.RequestClose += OnRequestClose;

            // Załaduj dane asynchronicznie
            await viewModel.InitializeAsync();
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
    }
}
