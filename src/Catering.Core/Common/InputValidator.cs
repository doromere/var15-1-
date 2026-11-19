using System.Text.RegularExpressions;

namespace Catering.Core.Common;

/// <summary>
/// Валидация полей ввода: телефоны и номера документов — через Regex,
/// суммы и количества — без отрицательных значений.
/// </summary>
public static class InputValidator
{
    // +7 (XXX) XXX-XX-XX или 8XXXXXXXXXX, допустимы пробелы/скобки/дефисы.
    private static readonly Regex PhoneRegex = new(
        @"^(\+7|8)[\s\-]?\(?\d{3}\)?[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}$",
        RegexOptions.Compiled);

    public static bool IsValidPhone(string? phone) =>
        !string.IsNullOrWhiteSpace(phone) && PhoneRegex.IsMatch(phone.Trim());

    public static bool IsValidContractNumber(string? contractNumber) =>
        DocumentNumberGenerator.IsValid(contractNumber);

    public static bool IsNonNegative(decimal value) => value >= 0;

    public static bool IsPositive(int value) => value > 0;

    /// <summary>Причина отмены обязательна и должна содержать не менее 10 символов.</summary>
    public static bool IsValidCancelReason(string? cancelReason) =>
        !string.IsNullOrWhiteSpace(cancelReason) && cancelReason.Trim().Length >= 10;
}
