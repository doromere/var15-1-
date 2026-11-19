using System;

namespace Catering.Desktop.Services;

/// <summary>
/// Строка подключения к локальной MariaDB/MySQL (phpMyAdmin: http://localhost/phpmyadmin/).
/// По умолчанию — XAMPP/OpenServer: root без пароля, база catering_db.
/// Переопределение: переменная окружения CATERING_CONNECTION_STRING.
/// </summary>
public static class ConnectionOptions
{
    public const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=catering_db;User Id=root;Password=;CharSet=utf8mb4;";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("CATERING_CONNECTION_STRING") is { Length: > 0 } value
            ? value
            : DefaultConnectionString;
}
