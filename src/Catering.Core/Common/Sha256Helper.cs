using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Catering.Core.Common;

/// <summary>
/// SHA256-хеширование: пароли пользователей и цифровая подпись экспортируемых документов.
/// </summary>
public static class Sha256Helper
{
    /// <summary>Вычисляет SHA256-хеш строки и возвращает его в виде lowercase-hex.</summary>
    public static string ComputeHash(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Хеш пароля пользователя (хранится в users.password_hash).</summary>
    public static string HashPassword(string password) => ComputeHash(password ?? string.Empty);

    /// <summary>
    /// Каноническая строка реквизитов документа для подписи:
    /// [Номер]+[Дата]+[Контрагент]+[Сумма]+[КоличествоПозиций].
    /// </summary>
    public static string BuildCanonicalString(
        string contractNumber,
        DateTime eventDatetime,
        string clientName,
        decimal totalBudget,
        int itemsCount)
    {
        string date = eventDatetime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        string sum = totalBudget.ToString("0.00", CultureInfo.InvariantCulture);
        return $"{contractNumber}|{date}|{clientName}|{sum}|{itemsCount}";
    }

    /// <summary>Цифровая подпись документа SHA256 от канонической строки реквизитов.</summary>
    public static string ComputeDocumentSignature(
        string contractNumber,
        DateTime eventDatetime,
        string clientName,
        decimal totalBudget,
        int itemsCount)
    {
        string canonical = BuildCanonicalString(contractNumber, eventDatetime, clientName, totalBudget, itemsCount);
        return ComputeHash(canonical);
    }
}
