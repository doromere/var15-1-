using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Catering.Core.Services;
using Catering.Core.Data;
using Catering.Desktop.ViewModels;
using Catering.Desktop.Views;
using Catering.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Catering.Desktop;

public partial class App : Application
{
    /// <summary>DI-контейнер приложения (регистрация зависимостей).</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // --- Инфраструктура ---
        services.AddSingleton<SessionState>();
        services.AddSingleton<Func<AppDbContext>>(
            _ => () => AppDbContext.Create(ConnectionOptions.ConnectionString));

        // --- Бизнес-логика (Catering.Core), без UI-зависимостей ---
        services.AddSingleton<ICalculationService, CateringCalculationService>();
        services.AddSingleton<IContractStateMachine, ContractStateMachine>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IContractService, ContractService>();
        services.AddSingleton<ICatalogService, CatalogService>();
        services.AddSingleton<IDocumentExportService, DocumentExportService>();

        // --- ViewModels ---
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ContractEditorViewModel>();
        services.AddTransient<CancelReasonViewModel>();
        services.AddTransient<ConfirmDeleteViewModel>();
        services.AddTransient<ClientDialogViewModel>();

        // --- Окна ---
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<ContractEditorWindow>();
        services.AddTransient<CancelReasonWindow>();
        services.AddTransient<ConfirmDeleteWindow>();
        services.AddTransient<ClientDialogWindow>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.ShutdownMode = Avalonia.Controls.ApplicationLifetimes.ShutdownMode.OnLastWindowClose;
            lifetime.MainWindow = Services.GetRequiredService<LoginWindow>();
            lifetime.MainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Переключение на главное окно после успешной аутентификации.</summary>
    public static void ShowMainWindow(Window current)
    {
        if (ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
            return;
        var main = Services.GetRequiredService<MainWindow>();
        lifetime.MainWindow = main;
        main.Show();
        current.Close();
    }

    /// <summary>Возврат к окну авторизации («Сменить учетную запись»).</summary>
    public static void ShowLoginWindow(Window current)
    {
        if (ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
            return;
        var login = Services.GetRequiredService<LoginWindow>();
        lifetime.MainWindow = login;
        login.Show();
        current.Close();
    }
}
