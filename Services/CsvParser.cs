using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ofertomator.Services;

/// <summary>
/// Parser plików CSV z automatycznym wykrywaniem separatora
/// </summary>
public class CsvParser
{
    /// <summary>
    /// Wczytaj plik CSV i zwróć nagłówki + pierwsze N wierszy
    /// </summary>
    public static CsvParseResult ParseFile(string filePath, int previewRows = 5)
    {
        try
        {
            // Wczytaj wszystkie linie
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            
            if (lines.Length == 0)
                throw new InvalidOperationException("Plik jest pusty.");

            // Wykryj separator (średnik lub przecinek)
            var separator = DetectSeparator(lines[0]);

            // Parsuj nagłówki (pierwszy wiersz)
            var headers = ParseLine(lines[0], separator);

            // Parsuj wiersze podglądu (następne N wierszy)
            var previewData = new List<Dictionary<string, string>>();
            for (int i = 1; i < Math.Min(lines.Length, previewRows + 1); i++)
            {
                var values = ParseLine(lines[i], separator);
                var row = new Dictionary<string, string>();
                
                for (int j = 0; j < headers.Count; j++)
                {
                    row[headers[j]] = j < values.Count ? values[j] : string.Empty;
                }
                
                previewData.Add(row);
            }

            return new CsvParseResult
            {
                Headers = headers,
                PreviewData = previewData,
                Separator = separator,
                TotalRows = lines.Length - 1 // -1 bo nagłówek
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Błąd podczas parsowania pliku CSV: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Wczytaj wszystkie wiersze z pliku CSV
    /// </summary>
    public static List<Dictionary<string, string>> ParseAllRows(string filePath, char separator)
    {
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        if (lines.Length == 0) return new List<Dictionary<string, string>>();

        var headers = ParseLine(lines[0], separator);
        var result = new List<Dictionary<string, string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseLine(lines[i], separator);
            var row = new Dictionary<string, string>();
            
            for (int j = 0; j < headers.Count; j++)
            {
                row[headers[j]] = j < values.Count ? values[j] : string.Empty;
            }
            
            result.Add(row);
        }

        return result;
    }

    /// <summary>
    /// Wykryj separator (średnik lub przecinek) na podstawie pierwszego wiersza
    /// </summary>
    private static char DetectSeparator(string firstLine)
    {
        var semicolonCount = firstLine.Count(c => c == ';');
        var commaCount = firstLine.Count(c => c == ',');

        return semicolonCount > commaCount ? ';' : ',';
    }

    /// <summary>
    /// Parsuj pojedynczy wiersz CSV (obsługa cudzysłowów)
    /// </summary>
    private static List<string> ParseLine(string line, char separator)
    {
        var result = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Toggle quotes mode
                inQuotes = !inQuotes;
            }
            else if (c == separator && !inQuotes)
            {
                // Separator poza cudzysłowami - koniec wartości
                result.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Dodaj ostatnią wartość
        result.Add(currentValue.ToString().Trim());

        return result;
    }
}

/// <summary>
/// Wynik parsowania CSV
/// </summary>
public class CsvParseResult
{
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string>> PreviewData { get; set; } = new();
    public char Separator { get; set; }
    public int TotalRows { get; set; }
}
