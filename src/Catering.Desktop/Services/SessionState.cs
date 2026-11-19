using Catering.Core.Models;

namespace Catering.Desktop.Services;

/// <summary>Текущий авторизованный сотрудник (singleton в DI-контейнере).</summary>
public sealed class SessionState
{
    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public string DisplayName => CurrentUser?.FullName ?? "—";

    public string RoleName => CurrentUser?.Role?.Name ?? "—";

    public void SignIn(User user) => CurrentUser = user;

    public void SignOut() => CurrentUser = null;
}
