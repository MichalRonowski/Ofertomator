using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Ofertomator.Views;

public partial class InputDialog : Window
{
    public string InputValue { get; private set; } = string.Empty;
    public bool DialogResult { get; private set; }

    public InputDialog()
    {
        InitializeComponent();
        
        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
        InputTextBox.KeyDown += InputTextBox_KeyDown;
        
        // Focus na TextBox po załadowaniu
        Opened += (s, e) => InputTextBox.Focus();
    }

    public InputDialog(string title, string prompt, string watermark = "", string defaultValue = "") : this()
    {
        Title = title;
        PromptText.Text = prompt;
        if (!string.IsNullOrEmpty(watermark))
            InputTextBox.Watermark = watermark;
        if (!string.IsNullOrEmpty(defaultValue))
            InputTextBox.Text = defaultValue;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            ErrorText.Text = "Pole nie może być puste";
            ErrorText.IsVisible = true;
            return;
        }

        InputValue = InputTextBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputTextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            OkButton_Click(sender, new RoutedEventArgs());
        }
        else if (e.Key == Avalonia.Input.Key.Escape)
        {
            CancelButton_Click(sender, new RoutedEventArgs());
        }
    }
}
