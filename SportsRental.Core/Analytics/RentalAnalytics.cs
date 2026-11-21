namespace SportsRental.Core.Analytics;

/// <summary>Навчальні масиви з ПР8; не замінюють реальні записи у rentals.json.</summary>
public static class RentalAnalytics
{
    public static readonly int[] DailyRentals =
    {
        3, 5, 4, 7, 0, 6, 9, 8, 7, 10,
        6, 5, 0, 4, 8, 11, 9, 7, 6, 12,
        10, 8, 4, 3, 6, 9, 11, 8, 7, 5
    };
    public static readonly int[,] WeeklyByType =
    {
        { 4, 6, 5, 7, 8, 10, 9 },
        { 3, 4, 5, 4, 7, 8, 6 },
        { 2, 4, 3, 5, 6, 7, 5 },
        { 1, 2, 2, 3, 4, 6, 5 },
        { 2, 3, 4, 4, 5, 6, 7 },
        { 1, 2, 1, 3, 4, 5, 4 }
    };
    public static readonly string[] TypeLabels =
        { "Велосипеди", "Самокати", "Ролики", "Шоломи", "Лижі", "Сноуборди" };
    public static int Sum(params int[] values) => values.Sum();
    public static decimal Average(int[] values) => values.Length == 0 ? 0m : (decimal)Sum(values) / values.Length;
    public static int FindMax(int[] values, out int index)
    {
        if (values.Length == 0) throw new ArgumentException("Порожній масив.");
        index = 0;
        for (int i = 1; i < values.Length; i++) if (values[i] > values[index]) index = i;
        return values[index];
    }
    public static int CountAbove(int[] values, int threshold = 8) => values.Count(v => v > threshold);
    public static int FindFirstZero(int[] values) => Array.IndexOf(values, 0);
    public static int[] RowSums(int[,] table) => Enumerable.Range(0, table.GetLength(0))
        .Select(row => Enumerable.Range(0, table.GetLength(1)).Sum(col => table[row, col])).ToArray();
    public static int[] ColumnSums(int[,] table) => Enumerable.Range(0, table.GetLength(1))
        .Select(col => Enumerable.Range(0, table.GetLength(0)).Sum(row => table[row, col])).ToArray();
    public static int MatrixMax(int[,] table, out int row, out int column)
    {
        if (table.Length == 0) throw new ArgumentException("Порожня таблиця.");
        row = 0; column = 0;
        for (int i = 0; i < table.GetLength(0); i++)
            for (int j = 0; j < table.GetLength(1); j++)
                if (table[i, j] > table[row, column]) { row = i; column = j; }
        return table[row, column];
    }
    public static int RecursiveSum(int[] values, int index = 0)
    {
        // Базовий випадок: усі елементи вже пройдено.
        if (index >= values.Length) return 0;
        // Рекурсивний крок: поточний елемент і менша задача.
        return values[index] + RecursiveSum(values, index + 1);
    }
}
