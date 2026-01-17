using System;
using System.Globalization;
using System.Linq;

namespace Ofertomator.Helpers;

/// <summary>
/// Pomocnicze metody do parsowania danych z różnych formatów
/// </summary>
public static class DataParser
{
    /// <summary>
    /// Parsuje cenę z polskiego formatu (przecinek) lub międzynarodowego (kropka)
    /// </summary>
    public static decimal ParsePrice(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0m;

        // Usuń białe znaki
        input = input.Trim();

        // Zamień przecinek na kropkę
        input = input.Replace(',', '.');

        // Usuń inne znaki (spacje, waluty)
        input = new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());

        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0m;
    }

    /// <summary>
    /// Parsuje stawkę VAT z różnych formatów: "23%", "23", "0.23"
    /// Zwraca wartość procentową (np. 23 dla 23%)
    /// </summary>
    public static decimal ParseVatRate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 23m; // Domyślna stawka VAT

        // Usuń białe znaki i znak %
        input = input.Trim().Replace("%", "");

        // Zamień przecinek na kropkę
        input = input.Replace(',', '.');

        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            // Jeśli wartość jest w przedziale 0-1, to jest to format dziesiętny (0.23)
            // Konwertujemy na procenty
            if (result > 0 && result < 1)
                return result * 100m;

            return result;
        }

        return 23m; // Domyślna stawka VAT
    }

    /// <summary>
    /// Formatuje cenę do wyświetlania w polskim formacie
    /// </summary>
    public static string FormatPrice(decimal price)
    {
        return price.ToString("N2", new CultureInfo("pl-PL"));
    }

    /// <summary>
    /// Formatuje procent do wyświetlania
    /// </summary>
    public static string FormatPercent(decimal percent)
    {
        return $"{percent:F2}%";
    }
}
