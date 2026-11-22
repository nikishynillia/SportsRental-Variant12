using SportsRental.Core.Analytics;
using SportsRental.Core.Models;
using SportsRental.Core.Services;

namespace SportsRental.ConsoleApp.Diagnostics;

/// <summary>Самотести без зовнішніх залежностей; ніколи не змінюють реальні дані.</summary>
public static class SelfTest
{
    private static int passed, failed;
    private static readonly List<string> output = new();
    private static void Check(string id, string name, Func<bool> action)
    {
        try
        {
            if (!action()) throw new InvalidOperationException("Не збігся результат.");
            passed++; output.Add($"{id} | {name} | Так");
        }
        catch (Exception e) { failed++; output.Add($"{id} | {name} | Ні: {e.Message}"); }
    }
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; } catch (T) { return true; }
    }
    public static int Run()
    {
        passed = 0; failed = 0; output.Clear();
        string folder = Path.Combine(Path.GetTempPath(), "sportsrental-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var app = new ApplicationContext(folder);
            DateTime weekday = new(2026, 11, 23, 10, 0, 0);
            DateTime weekend = new(2026, 11, 21, 10, 0, 0);
            Check("T01", "Клієнт: створити", () => app.ClientService.Register(1, "Іван Петренко", "+380671234567").Id == 1);
            Check("T02", "Клієнт: телефон короткий", () => Throws<ArgumentException>(() => app.ClientService.Register(2, "Олена", "12")));
            Check("T03", "Клієнт: повторний ключ", () => Throws<ArgumentException>(() => app.ClientService.Register(1, "Ірина", "+380661111111")));
            Check("T04", "Клієнт: змінити", () => { app.ClientService.Edit(1, "Іван Коваль", "+380671234567", null); return app.ClientService.Find(1)?.FullName == "Іван Коваль"; });
            Check("T05", "Клієнт: пошук відсутнього", () => app.ClientService.Find(999) is null);
            Check("T06", "Інвентар: створити", () => app.InventoryService.Add(1, "Велосипед №1", EquipmentType.Bike).Status == EquipmentStatus.Available);
            Check("T07", "Інвентар: порожня назва", () => Throws<ArgumentException>(() => app.InventoryService.Add(2, "", EquipmentType.Skis)));
            Check("T08", "Тариф D4: велосипед 40 грн", () => app.TariffService.ForType(EquipmentType.Bike).HourlyRate == 40m);
            Check("T09", "ПР7: будній день, до 72 год", () => app.RentalService.Quote(1, weekday, 24) == 960m);
            Check("T10", "ПР7: вихідний, до 72 год", () => app.RentalService.Quote(1, weekend, 24) == 1152m);
            Check("T11", "ПР7: будній день, понад 72 год", () => app.RentalService.Quote(1, weekday, 96) == 3456m);
            Check("T12", "ПР7: вихідний, понад 72 год", () => app.RentalService.Quote(1, weekend, 96) == 4147.20m);
            Check("T13", "ПР9: лижі, 24 год", () => new Skis(2, false, app.TariffService.ForType(EquipmentType.Skis)).CalculateRent(24) == 1320m);
            Check("T14", "ПР9: намет, 24 год", () => new Tent(3, false, app.TariffService.ForType(EquipmentType.Tent)).CalculateRent(24) == 840m);
            Check("T15", "XOR: клієнта немає", () => Throws<KeyNotFoundException>(() => app.RentalService.Issue(1, 99, 1, weekday, 24)));
            Check("T16", "Видача: активний прокат", () => app.RentalService.Issue(1, 1, 1, weekend, 24).Cost == 1152m && app.InventoryService.Find(1)?.Status == EquipmentStatus.Rented);
            Check("T17", "XOR: зайнятий інвентар", () => Throws<InvalidOperationException>(() => app.RentalService.Issue(2, 1, 1, weekend, 24)));
            Check("T18", "Повернення: доступний стан", () => app.RentalService.Return(1, weekend.AddHours(24)).Status == RentalStatus.Returned && app.InventoryService.Find(1)?.Status == EquipmentStatus.Available);
            Check("T19", "Повернення повторно: відмова", () => Throws<InvalidOperationException>(() => app.RentalService.Return(1, weekend.AddHours(24))));
            Check("T20", "Повторна видача після повернення", () => app.RentalService.Issue(2, 1, 1, weekday, 24).Status == RentalStatus.Active);
            Check("T21", "Повернення в ремонт", () => app.RentalService.Return(2, weekday.AddHours(24), true).Status == RentalStatus.Returned && app.InventoryService.Find(1)?.Status == EquipmentStatus.Maintenance);
            Check("T22", "Ремонт завершено", () => { app.InventoryService.SetMaintenance(1, false); return app.InventoryService.Find(1)?.Status == EquipmentStatus.Available; });
            Check("T23", "Гранично 0 год: відмова", () => Throws<ArgumentOutOfRangeException>(() => app.RentalService.Quote(1, weekday, 0)));
            Check("T24", "Гранично 336 год: дозволено", () => app.RentalService.Quote(1, weekday, 336) > 0m);
            Check("T25", "Гранично 337 год: відмова", () => Throws<ArgumentOutOfRangeException>(() => app.RentalService.Quote(1, weekday, 337)));
            Check("T26", "Зв'язки: заборона видаляти клієнта з історією", () => Throws<InvalidOperationException>(() => app.ClientService.Delete(1)));
            Check("T27", "Звіт: 2 повернення й сума", () => { var s = app.ReportService.Summary(RentalStatus.Returned); return s.Count == 2 && s.Total == 2112m; });
            Check("T28", "Чотири фізичні JSON", () => new[] { "clients.json", "inventory.json", "rentals.json", "tariffs.json" }.All(n => File.Exists(Path.Combine(folder, n))));
            Check("T29", "Повторний запуск: дані збережено", () =>
            {
                var restored = new ApplicationContext(folder);
                return restored.ClientService.Find(1)?.FullName == "Іван Коваль" && restored.RentalService.All().Count == 2 &&
                       restored.InventoryService.Find(1)?.Status == EquipmentStatus.Available;
            });
            Check("T30", "Зміна тарифу переживає перезапуск", () =>
            {
                app.TariffService.ChangeRate(EquipmentType.Bike, 45m, 1.20m, 0.90m);
                return new ApplicationContext(folder).TariffService.ForType(EquipmentType.Bike).HourlyRate == 45m;
            });
            Check("T31", "ПР8: сума за 30 днів", () => RentalAnalytics.Sum(RentalAnalytics.DailyRentals) == 198);
            Check("T32", "ПР8: рекурсивна сума", () => RentalAnalytics.RecursiveSum(new[] { 5, 0, 7 }) == 12);
            Check("T33", "ПР8: матриця 6×7", () => RentalAnalytics.ColumnSums(RentalAnalytics.WeeklyByType).Sum() == 192);
            Check("T34", "ПР8: максимум у кінці", () => RentalAnalytics.FindMax(new[] { 1, 2, 9 }, out int i) == 9 && i == 2);
            Check("T35", "ПР8: один елемент", () => RentalAnalytics.FindMax(new[] { 7 }, out int i) == 7 && i == 0);
            Check("T36", "ПР8: однакові числа", () => RentalAnalytics.FindMax(new[] { 4, 4, 4 }, out int i) == 4 && i == 0);
            Check("T37", "Введення: текст", () => !ConsoleInput.TryReadInt("abc", 1, 10, out _));
            Check("T38", "Введення: від'ємне", () => !ConsoleInput.TryReadInt("-1", 1, 10, out _));
            Check("T39", "override: пізнє зв'язування", () => ((RentalItem)new Bike(10, false, app.TariffService.ForType(EquipmentType.Bike))).GetDescription().Contains("міський велосипед"));
            Check("T40", "new: приховування в похідному", () =>
            {
                Tent tent = new(11, false, app.TariffService.ForType(EquipmentType.Tent), 6);
                RentalItem baseItem = tent;
                return tent.GetDescription().Contains("місць: 6") && !baseItem.GetDescription().Contains("місць: 6");
            });
            // Курсовий проєкт: повний прикладний CRUD третьої сутності Rental (D3).
            // Окремий контекст не змінює результати попередніх тестів T01–T40.
            var crud = new ApplicationContext(Path.Combine(folder, "rental-crud-tests"));
            DateTime activeStart = new(2026, 11, 23, 10, 0, 0);
            crud.ClientService.Register(101, "Тестовий клієнт", "+380671234567");
            crud.InventoryService.Add(101, "Тестовий велосипед", EquipmentType.Bike);
            Check("T41", "D3 CRUD: створити прокат", () =>
                crud.RentalService.Issue(101, 101, 101, activeStart, 12).Status == RentalStatus.Active);
            Check("T42", "D3 CRUD: знайти і переглянути", () =>
                crud.RentalService.Find(101)?.Hours == 12 && crud.RentalService.All().Count == 1);
            Check("T43", "D3 CRUD: виправити активний", () =>
            {
                Rental edited = crud.RentalService.EditActive(101, activeStart, 24);
                return edited.Hours == 24 && edited.Cost == 960m && crud.InventoryService.Find(101)?.Status == EquipmentStatus.Rented;
            });
            Check("T44", "D3 CRUD: заборона тривалості 0 год", () =>
                Throws<ArgumentOutOfRangeException>(() => crud.RentalService.EditActive(101, activeStart, 0)) &&
                crud.RentalService.Find(101)?.Hours == 24);
            Check("T45", "D3 CRUD: скасувати активний", () =>
                crud.RentalService.CancelActive(101) && crud.RentalService.Find(101) is null &&
                crud.InventoryService.Find(101)?.Status == EquipmentStatus.Available);
            Check("T46", "D3 CRUD: немає запису для скасування", () => !crud.RentalService.CancelActive(101));
            crud.RentalService.Issue(102, 101, 101, activeStart, 12);
            crud.RentalService.Return(102, activeStart.AddHours(12));
            Check("T47", "D3 CRUD: зберегти історію повернень", () =>
                Throws<InvalidOperationException>(() => crud.RentalService.CancelActive(102)) &&
                crud.RentalService.Find(102)?.Status == RentalStatus.Returned);
            Check("T48", "D3 CRUD: не редагувати завершений; JSON", () =>
                Throws<InvalidOperationException>(() => crud.RentalService.EditActive(102, activeStart, 24)) &&
                new ApplicationContext(Path.Combine(folder, "rental-crud-tests")).RentalService.Find(102)?.Hours == 12);
        }
        catch (Exception e) { failed++; output.Add("SETUP | Неочікувана помилка: " + e.Message); }
        finally
        {
            try { Directory.Delete(folder, recursive: true); }
            catch (Exception e) { output.Add("NOTE | Не вдалося очистити тимчасові дані: " + e.Message); }
        }
        foreach (string row in output) System.Console.WriteLine(row);
        System.Console.WriteLine($"Підсумок: {passed} пройдено / {failed} не пройдено (усього {passed + failed}).");
        return failed == 0 ? 0 : 1;
    }
}
