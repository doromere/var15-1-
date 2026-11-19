using Catering.Core.Common;
using Catering.Core.Data;
using Catering.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Catering.Core.Services;

/// <summary>
/// Аутентификация по логину и паролю. Хеш пароля сравнивается с users.password_hash (SHA256).
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly Func<AppDbContext> _contextFactory;

    public AuthService(Func<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public User? Authenticate(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return null;

        using AppDbContext context = _contextFactory();
        string hash = Sha256Helper.HashPassword(password);
        string normalized = login.Trim();

        return context.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.Login == normalized && u.PasswordHash == hash);
    }
}
