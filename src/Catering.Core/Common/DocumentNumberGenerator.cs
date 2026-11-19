namespace Catering.Core.Common;

/// <summary>
/// Маска номера документа: ПР23-КТР-{Год}-{Номер:D5}.
/// Пример: ПР23-КТР-2026-00042.
/// </summary>
public static class DocumentNumberGenerator
{
    public const string Prefix = "ПР23-КТР";

    public static string Generate(int year, int index)
    {
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "Порядковый номер должен быть >= 1.");
        return $"{Prefix}-{year}-{index:D5}";
    }

    /// <summary>Проверка номера документа по маске ПР23-КТР-{Год}-{5 цифр}.</summary>
    public static bool IsValid(string? contractNumber)
    {
        if (string.IsNullOrWhiteSpace(contractNumber))
            return false;
        string[] parts = contractNumber.Split('-');
        if (parts.Length != 4)
            return false;
        if (parts[0] != "ПР23" || parts[1] != "КТР")
            return false;
        if (!int.TryParse(parts[2], out int year) || year < 2000 || year > 2100)
            return false;
        return parts[3].Length == 5 && parts[3].All(char.IsDigit);
    }
}
