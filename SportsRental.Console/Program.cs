using System.Globalization;
using System.Text;
using SportsRental.ConsoleApp.Diagnostics;
using SportsRental.Core.Models;
using SportsRental.Core.Services;
using SportsRental.Core.Analytics;

namespace SportsRental.ConsoleApp;

internal static class Program
{
    private static ApplicationContext _app = null!;
    private static int Main(string[] args)
    {
        System.Console.InputEncoding = Encoding.UTF8;
        System.Console.OutputEncoding = Encoding.UTF8;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("uk-UA");
        if (args.Contains("--self-test")) return SelfTest.Run();
        string folder = "data";
        int flag = Array.IndexOf(args, "--data");
        if (flag >= 0 && flag + 1 < args.Length) folder = args[flag + 1];
        try
        {
            _app = new ApplicationContext(folder);
            System.Console.WriteLine("Облік прокату спортивного інвентарю — варіант 12");
            System.Console.WriteLine($"Файли даних: {_app.DataFolder}");
            RunMenu();
            return 0;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine("Помилка запуску: " + ex.Message);
            return 1;
        }
    }

    private static void RunMenu()
    {
        int choice;
        do
        {
            System.Console.WriteLine("\n===== ГОЛОВНЕ МЕНЮ =====");
            System.Console.WriteLine("1. Інвентар (DFD 1 / A1)");
            System.Console.WriteLine("2. Клієнти (DFD 2 / A2)");
            System.Console.WriteLine("3. Оформити прокат (DFD 3 / A3)");
            System.Console.WriteLine("4. Повернути інвентар (DFD 4 / A4)");
            System.Console.WriteLine("5. Звіти та фільтри (DFD 5 / A5)");
            System.Console.WriteLine("6. Тарифи (D4)");
            System.Console.WriteLine("7. Поліморфізм класів (ПР9)");
            System.Console.WriteLine("8. Табуляція вартості (ПР7)");
            System.Console.WriteLine("9. Масиви і рекурсія (ПР8)");
            System.Console.WriteLine("10. Керування прокатами (CRUD D3)");
            System.Console.WriteLine("0. Вихід");
            choice = ConsoleInput.ReadInt("Ваш вибір", 0, 99);
            try
            {
                switch (choice)
                {
                    case 1: InventoryMenu(); break;
                    case 2: ClientMenu(); break;
                    case 3: Issue(); break;
                    case 4: Return(); break;
                    case 5: Reports(); break;
                    case 6: Tariffs(); break;
                    case 7: Polymorphism(); break;
                    case 8: PriceTable(); break;
                    case 9: Analytics(); break;
                    case 10: RentalMenu(); break;
                    case 0: System.Console.WriteLine("Завершення роботи."); break;
                    default: System.Console.WriteLine("Немає такого пункту меню."); break;
                }
            }
            catch (Exception ex) { System.Console.WriteLine("Операція не виконана: " + ex.Message); }
        } while (choice != 0);
    }

    private static void ClientMenu()
    {
        System.Console.WriteLine("\n1 Додати | 2 Знайти | 3 Перелік | 4 Змінити | 5 Видалити | 0 Назад");
        int ch = ConsoleInput.ReadInt("Операція", 0, 5);
        if (ch == 0) return;
        if (ch == 3) { foreach (var c in _app.ClientService.All()) System.Console.WriteLine(c); return; }
        int id = ch == 1 ? NextId(_app.ClientService.All().Select(c => c.Id)) : ConsoleInput.ReadInt("ID клієнта", 1, 1000000);
        if (ch == 2) { System.Console.WriteLine(_app.ClientService.Find(id)?.ToString() ?? "Не знайдено."); return; }
        if (ch == 5) { System.Console.WriteLine(_app.ClientService.Delete(id) ? "Видалено." : "Не знайдено."); return; }
        string name = ConsoleInput.ReadRequired("ПІБ");
        string phone = ConsoleInput.ReadRequired("Телефон");
        string? email = ConsoleInput.ReadOptional("Email");
        if (ch == 1) System.Console.WriteLine("Зареєстровано: " + _app.ClientService.Register(id, name, phone, email));
        else { _app.ClientService.Edit(id, name, phone, email); System.Console.WriteLine("Змінено."); }
    }

    private static void InventoryMenu()
    {
        System.Console.WriteLine("\n1 Додати | 2 Знайти | 3 Перелік | 4 Змінити назву | 5 Видалити | 6 У ремонт | 7 Із ремонту | 0 Назад");
        int ch = ConsoleInput.ReadInt("Операція", 0, 7);
        if (ch == 0) return;
        if (ch == 3) { foreach (var i in _app.InventoryService.All()) System.Console.WriteLine(i); return; }
        int id = ch == 1 ? NextId(_app.InventoryService.All().Select(x => x.Id)) : ConsoleInput.ReadInt("ID інвентарю", 1, 1000000);
        if (ch == 2) { System.Console.WriteLine(_app.InventoryService.Find(id)?.ToString() ?? "Не знайдено."); return; }
        if (ch == 5) { System.Console.WriteLine(_app.InventoryService.Delete(id) ? "Видалено." : "Не знайдено."); return; }
        if (ch is 6 or 7) { _app.InventoryService.SetMaintenance(id, maintenance: ch == 6); System.Console.WriteLine("Статус оновлено."); return; }
        string name = ConsoleInput.ReadRequired("Назва одиниці інвентарю");
        if (ch == 4) { _app.InventoryService.Rename(id, name); System.Console.WriteLine("Оновлено."); return; }
        System.Console.WriteLine("1 Велосипед | 2 Лижі | 3 Намет");
        EquipmentType type = (EquipmentType)ConsoleInput.ReadInt("Категорія", 1, 3);
        System.Console.WriteLine("Додано: " + _app.InventoryService.Add(id, name, type));
    }

    private static void Issue()
    {
        if (_app.ClientService.All().Count == 0 || _app.InventoryService.All().Count == 0)
        { System.Console.WriteLine("Спершу додайте клієнта та інвентар."); return; }
        int clientId = ConsoleInput.ReadInt("ID клієнта", 1, 1000000);
        int equipmentId = ConsoleInput.ReadInt("ID інвентарю", 1, 1000000);
        int hours = ConsoleInput.ReadInt("Годин оренди", 1, 336);
        DateTime start = ConsoleInput.ReadDateTime("Дата й час видачі");
        decimal quote = _app.RentalService.Quote(equipmentId, start, hours);
        System.Console.WriteLine($"Вартість за тарифом: {quote:F2} грн.");
        if (!ConsoleInput.ReadYesNo("Підтвердити видачу?")) return;
        int id = NextId(_app.RentalService.All().Select(r => r.Id));
        System.Console.WriteLine("Оформлено: " + _app.RentalService.Issue(id, clientId, equipmentId, start, hours));
    }

    private static void Return()
    {
        int id = ConsoleInput.ReadInt("ID прокату", 1, 1000000);
        bool needRepair = ConsoleInput.ReadYesNo("Потрібне обслуговування? ");
        System.Console.WriteLine("Повернення: " + _app.RentalService.Return(id, DateTime.Now, needRepair));
    }

    private static void RentalMenu()
    {
        System.Console.WriteLine("\n1 Створити | 2 Знайти | 3 Перелік | 4 Виправити активний | 5 Скасувати активний | 0 Назад");
        int ch = ConsoleInput.ReadInt("Операція", 0, 5);
        if (ch == 0) return;
        if (ch == 1) { Issue(); return; }
        if (ch == 3)
        {
            foreach (var rental in _app.RentalService.All()) System.Console.WriteLine(rental);
            return;
        }
        int id = ConsoleInput.ReadInt("ID прокату", 1, 1000000);
        if (ch == 2)
        {
            System.Console.WriteLine(_app.RentalService.Find(id)?.ToString() ?? "Не знайдено.");
            return;
        }
        if (ch == 4)
        {
            int hours = ConsoleInput.ReadInt("Виправлена тривалість у годинах", 1, 336);
            DateTime newStart = ConsoleInput.ReadDateTime("Виправлені дата і час видачі");
            System.Console.WriteLine("Оновлено: " + _app.RentalService.EditActive(id, newStart, hours));
            return;
        }
        Rental? original = _app.RentalService.Find(id);
        if (original is null) { System.Console.WriteLine("Не знайдено."); return; }
        System.Console.WriteLine("Скасування видаляє лише помилковий АКТИВНИЙ прокат; завершені записи захищено.");
        if (!ConsoleInput.ReadYesNo("Підтвердити скасування?")) return;
        System.Console.WriteLine(_app.RentalService.CancelActive(id) ? "Скасовано. Інвентар знову доступний." : "Не знайдено.");
    }

    private static void Reports()
    {
        System.Console.WriteLine("0 Усі | 1 Активні | 2 Завершені");
        int ch = ConsoleInput.ReadInt("Фільтр", 0, 2);
        RentalStatus? status = ch == 0 ? null : ch == 1 ? RentalStatus.Active : RentalStatus.Returned;
        foreach (var r in _app.ReportService.Filter(status)) System.Console.WriteLine(r);
        var sum = _app.ReportService.Summary(status);
        System.Console.WriteLine($"Кількість: {sum.Count}; вартість: {sum.Total:F2} грн (сума оформлених прокатів).");
    }

    private static void Tariffs()
    {
        foreach (Tariff t in _app.TariffService.All())
            System.Console.WriteLine($"{t.Type}: {t.HourlyRate:F2} грн/год; вихідний ×{t.WeekendMultiplier:F2}; понад 72 год ×{t.LongRentalMultiplier:F2}");
        if (!ConsoleInput.ReadYesNo("Змінити тариф?")) return;
        EquipmentType type = (EquipmentType)ConsoleInput.ReadInt("1 Bike, 2 Skis, 3 Tent", 1, 3);
        decimal rate = ConsoleInput.ReadDecimal("Ціна за годину", 0.01m, 100000m);
        decimal weekend = ConsoleInput.ReadDecimal("Коефіцієнт вихідного", 0.01m, 3m);
        decimal longRental = ConsoleInput.ReadDecimal("Коефіцієнт понад 72 год", 0.01m, 3m);
        _app.TariffService.ChangeRate(type, rate, weekend, longRental);
        System.Console.WriteLine("Тариф збережено. Старі прокати не перераховуються.");
    }

    private static void Polymorphism()
    {
        var tariffs = _app.TariffService;
        var list = new List<RentalItem>
        {
            new Bike(1, false, tariffs.ForType(EquipmentType.Bike)),
            new Bike(2, true, tariffs.ForType(EquipmentType.Bike)),
            new Skis(3, false, tariffs.ForType(EquipmentType.Skis)),
            new Skis(4, true, tariffs.ForType(EquipmentType.Skis)),
            new Tent(5, false, tariffs.ForType(EquipmentType.Tent)),
            new Tent(6, true, tariffs.ForType(EquipmentType.Tent), 6)
        };
        decimal total = 0m;
        foreach (RentalItem item in list)
        {
            decimal rent = item.CalculateRent(24); total += rent;
            System.Console.WriteLine($"{item.GetDescription()} — {rent:F2} грн / 24 год");
            if (item is Tent t) System.Console.WriteLine($"  Додатково: місткість {t.Capacity} осіб.");
        }
        System.Console.WriteLine($"Разом: {total:F2} грн");
        Tent tent = new(7, false, tariffs.ForType(EquipmentType.Tent), 4);
        RentalItem baseReference = tent;
        System.Console.WriteLine("new через Tent: " + tent.GetDescription());
        System.Console.WriteLine("new через RentalItem: " + baseReference.GetDescription());
    }

    private static void PriceTable()
    {
        var all = _app.InventoryService.All();
        if (all.Count == 0) { System.Console.WriteLine("Спочатку додайте інвентар."); return; }
        int id = ConsoleInput.ReadInt("ID інвентарю", 1, 1000000);
        DateTime start = ConsoleInput.ReadDateTime("Дата початку");
        decimal total = 0m;
        System.Console.WriteLine(" Годин | Вартість, грн");
        for (int h = 12; h <= 96; h += 12)
        {
            decimal cost = _app.RentalService.Quote(id, start, h); total += cost;
            System.Console.WriteLine($"{h,6} | {cost,13:F2}");
        }
        System.Console.WriteLine($"Сума значень таблиці: {total:F2} грн");
    }
    private static void Analytics()
    {
        int[] a = RentalAnalytics.DailyRentals;
        int max = RentalAnalytics.FindMax(a, out int day);
        System.Console.WriteLine($"30 днів: сума {RentalAnalytics.Sum(a)}, середнє {RentalAnalytics.Average(a):F2}, максимум {max} у день {day + 1}");
        System.Console.WriteLine($"Перший нуль: день {RentalAnalytics.FindFirstZero(a) + 1}; понад 8: {RentalAnalytics.CountAbove(a, threshold: 8)}; рекурсивна сума {RentalAnalytics.RecursiveSum(a)}");
        int[,] matrix = RentalAnalytics.WeeklyByType;
        int[] rowTotals = RentalAnalytics.RowSums(matrix);
        int[] colTotals = RentalAnalytics.ColumnSums(matrix);
        System.Console.WriteLine("Вид          Пн Вт Ср Чт Пт Сб Нд | Разом");
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            System.Console.Write($"{RentalAnalytics.TypeLabels[i],-12} ");
            for (int j = 0; j < matrix.GetLength(1); j++) System.Console.Write($"{matrix[i,j],2} ");
            System.Console.WriteLine($"| {rowTotals[i],4}");
        }
        System.Console.Write("Разом        ");
        foreach (int value in colTotals) System.Console.Write($"{value,2} ");
        System.Console.WriteLine($"| {colTotals.Sum(),4}");
        int valueMax = RentalAnalytics.MatrixMax(matrix, out int r, out int col);
        System.Console.WriteLine($"Максимальна комірка: {valueMax}; рядок {r + 1}, день {col + 1}");
    }
    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;
}
