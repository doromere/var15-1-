using System;
using Avalonia.Controls;
using Catering.Desktop.ViewModels;

namespace Catering.Desktop.Views;

public partial class ClientDialogWindow : Window
{
    public ClientDialogWindow(ClientDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, EventArgs e) => Close();
}
