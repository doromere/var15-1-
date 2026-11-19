namespace Catering.Core.Models;

/// <summary>
/// Учетная запись сотрудника (таблица users).
/// Пароль хранится только в виде SHA256-хеша (PasswordHash).
/// </summary>
public class User
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public string Login { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Role? Role { get; set; }

    public ICollection<CateringContract> Contracts { get; set; } = new List<CateringContract>();
}
