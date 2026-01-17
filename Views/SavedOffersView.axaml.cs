using Avalonia.Controls;
using Ofertomator.ViewModels;

namespace Ofertomator.Views;

public partial class SavedOffersView : Window
{
    public SavedOffersView()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);

        // Automatyczne załadowanie listy ofert przy otwarciu okna
        if (DataContext is SavedOffersViewModel viewModel)
        {
            await viewModel.LoadOffersCommand.ExecuteAsync(null);
        }
    }
}
