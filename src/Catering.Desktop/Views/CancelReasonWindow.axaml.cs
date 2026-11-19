using System;
using Avalonia.Controls;
using Catering.Desktop.ViewModels;

namespace Catering.Desktop.Views;

public partial class CancelReasonWindow : Window
{
    public CancelReasonWindow(CancelReasonViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, EventArgs e) => Close();
}
