namespace Catering.Core.Models;

/// <summary>
/// Роль пользователя (таблица roles).
/// </summary>
public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}
