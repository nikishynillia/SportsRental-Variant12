using System.Globalization;

namespace SportsRental.ConsoleApp;

/// <summary>Безпечне введення даних із повторним запитом, без винятків від TryParse.</summary>
public static class ConsoleInput
{
    private static readonly CultureInfo Ua = CultureInfo.GetCultureInfo("uk-UA");
    public static bool TryReadInt(string? text, int min, int max, out int value) =>
        int.TryParse(text, out value) && value >= min && value <= max;
    public static int ReadInt(string label, int min, int max)
    {
        int value;
        bool ok;
        do
        {
            System.Console.Write($"{label} ({min}–{max}): ");
            ok = TryReadInt(System.Console.ReadLine(), min, max, out value);
            if (!ok) System.Console.WriteLine("Помилка: введіть ціле число в допустимих межах.");
        } while (!ok);
        return value;
    }
    public static decimal ReadDecimal(string label, decimal min, decimal max)
    {
        decimal value;
        bool ok;
        do
        {
            System.Console.Write($"{label} ({min:F2}–{max:F2}): ");
            string normalized = (System.Console.ReadLine() ?? "").Trim().Replace('.', ',');
            ok = decimal.TryParse(normalized, NumberStyles.Number, Ua, out value) && value >= min && value <= max;
            if (!ok) System.Console.WriteLine("Помилка: введіть коректне число.");
        } while (!ok);
        return value;
    }
    public static string ReadRequired(string label)
    {
        string? value;
        do
        {
            System.Console.Write(label + ": "); value = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(value)) System.Console.WriteLine("Поле обов'язкове.");
        } while (string.IsNullOrWhiteSpace(value));
        return value.Trim();
    }
    public static string? ReadOptional(string label)
    {
        System.Console.Write(label + " (Enter — пропустити): ");
        string? value = System.Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
    public static DateTime ReadDateTime(string label)
    {
        while (true)
        {
            System.Console.Write(label + " (рррр-мм-дд гг:хх; Enter — зараз): ");
            string? input = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return DateTime.Now;
            if (DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime date) && date.Year is >= 2000 and <= 2100)
                return date;
            System.Console.WriteLine("Помилка формату дати.");
        }
    }
    public static bool ReadYesNo(string label)
    {
        while (true)
        {
            System.Console.Write(label + " (т/н): ");
            string? text = System.Console.ReadLine()?.Trim().ToLowerInvariant();
            if (text is "т" or "так" or "y" or "yes") return true;
            if (text is "н" or "ні" or "n" or "no") return false;
            System.Console.WriteLine("Введіть т або н.");
        }
    }
}
