using Catering.Core.Models;

namespace Catering.Core.Services;

/// <summary>Аутентификация сотрудников по логину и паролю (SHA256).</summary>
public interface IAuthService
{
    /// <summary>Возвращает пользователя при верном логине/пароле, иначе null.</summary>
    User? Authenticate(string login, string password);
}
