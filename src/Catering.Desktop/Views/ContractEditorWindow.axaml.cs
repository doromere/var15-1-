using System;
using Avalonia.Controls;
using Catering.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Catering.Desktop.Views;

public partial class ContractEditorWindow : Window
{
    private readonly ContractEditorViewModel _viewModel;

    public ContractEditorWindow(ContractEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, EventArgs e) => Close();

    private async void NewClientButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ClientDialogViewModel clientVm = App.Services.GetRequiredService<ClientDialogViewModel>();
        ClientDialogWindow dialog = new(clientVm);
        await dialog.ShowDialog(this);

        if (clientVm.CreatedClient is not null)
            _viewModel.AcceptNewClient(clientVm.CreatedClient);
    }
}
