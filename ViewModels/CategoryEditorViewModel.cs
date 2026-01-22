using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ofertomator.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ofertomator.ViewModels;

/// <summary>
/// ViewModel dla okna edycji/dodawania kategorii
/// </summary>
public partial class CategoryEditorViewModel : ObservableValidator
{
    #region Observable Properties

    [ObservableProperty]
    [Required(ErrorMessage = "Nazwa kategorii jest wymagana")]
    [MinLength(2, ErrorMessage = "Nazwa musi mieć minimum 2 znaki")]
    [NotifyDataErrorInfo]
    private string _name = string.Empty;

    [ObservableProperty]
    [Range(0, 1000, ErrorMessage = "Marża musi być w zakresie 0-1000%")]
    [NotifyDataErrorInfo]
    private decimal _defaultMargin;

    [ObservableProperty]
    [Range(0, 9999, ErrorMessage = "Kolejność musi być w zakresie 0-9999")]
    [NotifyDataErrorInfo]
    private int _displayOrder;

    [ObservableProperty]
    private string _windowTitle = "Dodaj Kategorię";

    #endregion

    #region Properties

    /// <summary>
    /// Flaga określająca tryb (true = edycja, false = dodawanie)
    /// </summary>
    public bool IsEditMode { get; }

    /// <summary>
    /// ID edytowanej kategorii (null dla nowej)
    /// </summary>
    public int? CategoryId { get; }

    /// <summary>
    /// Czy okno zostało zamknięte przez "Zapisz" (true) czy "Anuluj" (false)
    /// </summary>
    public bool DialogResult { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Event wywoływany gdy okno powinno się zamknąć
    /// </summary>
    public event EventHandler? RequestClose;

    #endregion

    #region Constructors

    /// <summary>
    /// Konstruktor dla trybu dodawania
    /// </summary>
    public CategoryEditorViewModel()
    {
        IsEditMode = false;
        WindowTitle = "Dodaj Kategorię";
        DefaultMargin = 30m; // Domyślna marża 30%
        DisplayOrder = 0; // Domyślna kolejność
    }

    /// <summary>
    /// Konstruktor dla trybu edycji
    /// </summary>
    public CategoryEditorViewModel(Category category)
    {
        IsEditMode = true;
        CategoryId = category.Id;
        WindowTitle = "Edytuj Kategorię";
        
        Name = category.Name;
        DefaultMargin = category.DefaultMargin;
        DisplayOrder = category.DisplayOrder;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Komenda: Zapisz i zamknij
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        // Walidacja
        ValidateAllProperties();
        
        if (HasErrors)
        {
            return;
        }

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Komenda: Anuluj i zamknij
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
    /// Zwraca obiekt Category z aktualnymi wartościami
    /// </summary>
    public Category GetCategory()
    {
        return new Category
        {
            Id = CategoryId ?? 0,
            Name = Name.Trim(),
            DefaultMargin = DefaultMargin,
            DisplayOrder = DisplayOrder
        };
    }

    #endregion
}
