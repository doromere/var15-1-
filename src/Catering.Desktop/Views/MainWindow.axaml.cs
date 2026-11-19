using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Catering.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Catering.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.SignOutRequested += OnSignOutRequested;
        Opened += async (_, _) => await _viewModel.LoadAsyncSafe();
    }

    private void OnSignOutRequested(object? sender, EventArgs e) => App.ShowLoginWindow(this);

    private async void NewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        await OpenEditorAsync(null);

    private async void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel.SelectedContract is not null)
            await OpenEditorAsync(_viewModel.SelectedContract.Id);
    }

    private async void ContractsGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.CanEdit && _viewModel.SelectedContract is not null)
            await OpenEditorAsync(_viewModel.SelectedContract.Id);
    }

    private async Task OpenEditorAsync(int? contractId)
    {
        ContractEditorViewModel editorVm = App.Services.GetRequiredService<ContractEditorViewModel>();
        await editorVm.InitializeAsync(contractId);

        ContractEditorWindow editor = new(editorVm);
        await editor.ShowDialog(this);

        if (editorVm.Saved)
            await _viewModel.LoadAsyncSafe();
    }

    private async void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel.SelectedContract is null) return;

        ConfirmDeleteViewModel confirmVm = new();
        confirmVm.Prepare($"Удалить черновик {_viewModel.SelectedContract.ContractNumber} вместе со спецификацией? Действие необратимо.");
        ConfirmDeleteWindow confirm = new(confirmVm);
        await confirm.ShowDialog(this);

        if (confirmVm.Confirmed)
            await _viewModel.DeleteSelectedAsync();
    }

    private async void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel.SelectedContract is null) return;

        CancelReasonViewModel cancelVm = App.Services.GetRequiredService<CancelReasonViewModel>();
        CancelReasonWindow dialog = new(cancelVm);
        await dialog.ShowDialog(this);

        if (cancelVm.Confirmed)
            await _viewModel.CancelContractAsync(cancelVm.Reason);
    }

    private async void ExportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel.SelectedContract is null) return;

        FilePickerSaveOptions options = new()
        {
            Title = "Экспорт калькуляционной сметы",
            SuggestedFileName = _viewModel.SelectedContract.ContractNumber,
            DefaultExtension = "txt",
            FileTypeChoices =
            [
                new FilePickerFileType("Текстовый файл") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("CSV (таблица)") { Patterns = new[] { "*.csv" } },
                new FilePickerFileType("HTML (печать)") { Patterns = new[] { "*.html" } },
            ],
        };

        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(options);
        if (file is null) return;

        string path = file.TryGetLocalPath() ?? file.Path.AbsolutePath;
        await _viewModel.ExportToFileAsync(path);
    }

    private void SignOutButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        App.ShowLoginWindow(this);
}
