using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Catering.Core.Common;
using Catering.Core.Models;
using Catering.Core.Services;
using Catering.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Catering.Desktop.ViewModels;

/// <summary>
/// Окно авторизации: проверка логина/пароля (SHA256).
/// После 3 неудачных попыток — блокировка на 10 секунд с обратным отсчётом (DispatcherTimer).
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    public const int MaxFailedAttempts = 3;
    public const int LockoutSeconds = 10;

    private readonly IAuthService _auth;
    private readonly SessionState _session;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSignIn))]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordChar))]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _failedAttempts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSignIn))]
    private bool _isLocked;

    [ObservableProperty]
    private int _lockoutSecondsLeft;

    /// <summary>Маска поля пароля: '*' или '\0' (без маски) при включённой видимости.</summary>
    public char PasswordChar => IsPasswordVisible ? '\0' : '*';

    public bool CanSignIn => !IsLocked;

    /// <summary>Поднимается после успешной аутентификации (окно открывает MainWindow).</summary>
    public event EventHandler? LoginSucceeded;

    public LoginViewModel(IAuthService auth, SessionState session)
    {
        _auth = auth;
        _session = session;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnLockoutTick;
    }

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private void SignIn()
    {
        if (IsLocked)
            return;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль.";
            return;
        }

        User? user;
        try
        {
            user = _auth.Authenticate(Login, Password);
        }
        catch (Exception ex)
        {
            // Ошибка БД не должна ронять приложение — показываем понятное сообщение.
            ErrorMessage = "Нет соединения с базой данных (проверьте строку подключения и что catering_db импортирована). Детали: "
                           + ex.Message;
            return;
        }
        if (user is null)
        {
            FailedAttempts++;
            Password = string.Empty;
            if (FailedAttempts >= MaxFailedAttempts)
            {
                StartLockout();
            }
            else
            {
                ErrorMessage = $"Неверный логин или пароль. Осталось попыток: {MaxFailedAttempts - FailedAttempts}.";
            }
            return;
        }

        ErrorMessage = null;
        FailedAttempts = 0;
        _session.SignIn(user);
        LoginSucceeded?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    partial void OnIsLockedChanged(bool value) => SignInCommand.NotifyCanExecuteChanged();

    private void StartLockout()
    {
        IsLocked = true;
        LockoutSecondsLeft = LockoutSeconds;
        ErrorMessage = $"Превышено число попыток. Интерфейс заблокирован на {LockoutSeconds} секунд.";
        _timer.Start();
    }

    private void OnLockoutTick(object? sender, EventArgs e)
    {
        LockoutSecondsLeft--;
        if (LockoutSecondsLeft > 0)
            return;

        _timer.Stop();
        IsLocked = false;
        FailedAttempts = 0;
        ErrorMessage = "Блокировка снята. Попробуйте снова.";
    }
}
