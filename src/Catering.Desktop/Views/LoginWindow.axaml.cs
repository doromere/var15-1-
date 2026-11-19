using System;
using Avalonia.Controls;
using Avalonia.Input;
using Catering.Desktop.ViewModels;

namespace Catering.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
        Opened += (_, _) => LoginBox.Focus();
    }

    private void OnLoginSucceeded(object? sender, EventArgs e) => App.ShowMainWindow(this);

    private void PasswordBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.SignInCommand.CanExecute(null))
            _viewModel.SignInCommand.Execute(null);
    }
}
