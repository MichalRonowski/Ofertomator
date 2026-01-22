using Avalonia.Controls;
using Avalonia.Interactivity;
using Ofertomator.ViewModels;

namespace Ofertomator.Views;

public partial class SavedOffersView : UserControl
{
    public SavedOffersView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Automatyczne załadowanie listy ofert przy załadowaniu kontrolki
        if (DataContext is SavedOffersViewModel viewModel)
        {
            await viewModel.LoadOffersCommand.ExecuteAsync(null);
        }
    }
}
