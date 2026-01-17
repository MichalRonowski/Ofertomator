using Avalonia.Controls;
using Ofertomator.ViewModels;
using System;

namespace Ofertomator.Views;

public partial class CategoryWindow : Window
{
    public CategoryWindow()
    {
        InitializeComponent();
    }

    public CategoryWindow(CategoryEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += (s, e) => Close();
    }
}
