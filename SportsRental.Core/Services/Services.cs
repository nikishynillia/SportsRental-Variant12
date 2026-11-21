using SportsRental.Core.Models;
using SportsRental.Core.Repositories;

namespace SportsRental.Core.Services;

/// <summary>DFD 2: реєстрація та ведення карток клієнтів.</summary>
public sealed class ClientService
{
    private readonly IRepository<Client> _clients;
    private readonly IRepository<Rental> _rentals;
    public ClientService(IRepository<Client> clients, IRepository<Rental> rentals)
    { _clients = clients; _rentals = rentals; }

    public Client Register(int id, string name, string phone, string? email = null)
    {
        Client result = new(id, name, phone, email, DateTime.Today);
        _clients.Add(result); return result;
    }
    public Client? Find(int id) => _clients.Find(id);
    public IReadOnlyList<Client> All() => _clients.GetAll();
    public void Edit(int id, string name, string phone, string? email)
    {
        Client client = _clients.Find(id) ?? throw new KeyNotFoundException("Клієнт не знайдений.");
        client.Update(name, phone, email); _clients.Update(client);
    }
    public bool Delete(int id)
    {
        if (_rentals.GetAll().Any(r => r.ClientId == id))
            throw new InvalidOperationException("Клієнт має історію прокатів. Видалення заборонено.");
        return _clients.Remove(id);
    }
}

/// <summary>DFD 1: облік інвентарю та дозволені переходи його станів.</summary>
public sealed class InventoryService
{
    private readonly IRepository<InventoryItem> _items;
    private readonly IRepository<Rental> _rentals;
    public InventoryService(IRepository<InventoryItem> items, IRepository<Rental> rentals)
    { _items = items; _rentals = rentals; }

    public InventoryItem Add(int id, string name, EquipmentType type)
    {
        InventoryItem item = new(id, name, type); _items.Add(item); return item;
    }
    public InventoryItem? Find(int id) => _items.Find(id);
    public IReadOnlyList<InventoryItem> All() => _items.GetAll();
    public void Rename(int id, string name)
    {
        var item = _items.Find(id) ?? throw new KeyNotFoundException("Інвентар не знайдено.");
        item.Rename(name); _items.Update(item);
    }
    public void SetMaintenance(int id, bool maintenance)
    {
        var item = _items.Find(id) ?? throw new KeyNotFoundException("Інвентар не знайдено.");
        if (maintenance) item.SendToMaintenance(); else item.CompleteMaintenance();
        _items.Update(item);
    }
    public bool Delete(int id)
    {
        var item = _items.Find(id);
        if (item is null) return false;
        if (item.Status == EquipmentStatus.Rented || _rentals.GetAll().Any(r => r.InventoryId == id))
            throw new InvalidOperationException("Інвентар використано у прокатах. Видалення заборонено.");
        return _items.Remove(id);
    }
}

/// <summary>DFD D4: налаштування тарифів та правил розрахунку.</summary>
public sealed class TariffService
{
    private readonly IRepository<Tariff> _tariffs;
    public TariffService(IRepository<Tariff> tariffs)
    {
        _tariffs = tariffs;
        if (_tariffs.GetAll().Count == 0)
        {
            _tariffs.Add(new Tariff(1, EquipmentType.Bike, 40m, 1.20m, 0.90m, new DateTime(2026, 1, 1)));
            _tariffs.Add(new Tariff(2, EquipmentType.Skis, 55m, 1.25m, 0.85m, new DateTime(2026, 1, 1)));
            _tariffs.Add(new Tariff(3, EquipmentType.Tent, 35m, 1.10m, 0.80m, new DateTime(2026, 1, 1)));
        }
    }
    public Tariff ForType(EquipmentType type) =>
        _tariffs.GetAll().SingleOrDefault(t => t.Type == type) ?? throw new KeyNotFoundException("Тариф не знайдено.");
    public IReadOnlyList<Tariff> All() => _tariffs.GetAll();
    public void ChangeRate(EquipmentType type, decimal rate, decimal weekend, decimal longRental)
    {
        Tariff tariff = ForType(type); tariff.Update(rate, weekend, longRental); _tariffs.Update(tariff);
    }
}

/// <summary>DFD 3 і 4: видача та повернення; XOR за доступністю інвентарю.</summary>
public sealed class RentalService
{
    private readonly IRepository<Rental> _rentals;
    private readonly IRepository<Client> _clients;
    private readonly IRepository<InventoryItem> _inventory;
    private readonly TariffService _tariffs;
    public RentalService(IRepository<Rental> rentals, IRepository<Client> clients,
        IRepository<InventoryItem> inventory, TariffService tariffs)
    { _rentals = rentals; _clients = clients; _inventory = inventory; _tariffs = tariffs; }

    /// <summary>Розраховує оренду через поліморфний клас RentalItem.</summary>
    public decimal Quote(int inventoryId, DateTime start, int hours)
    {
        var equipment = _inventory.Find(inventoryId) ?? throw new KeyNotFoundException("Інвентар не знайдено.");
        if (hours is < 1 or > RentalItem.MaxRentalHours) throw new ArgumentOutOfRangeException(nameof(hours));
        bool weekend = start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        Tariff tariff = _tariffs.ForType(equipment.Type);
        RentalItem calculator = equipment.Type switch
        {
            EquipmentType.Bike => new Bike(equipment.Id, weekend, tariff),
            EquipmentType.Skis => new Skis(equipment.Id, weekend, tariff),
            EquipmentType.Tent => new Tent(equipment.Id, weekend, tariff),
            _ => throw new ArgumentOutOfRangeException(nameof(equipment.Type))
        };
        return calculator.CalculateRent(hours);
    }

    public Rental Issue(int id, int clientId, int equipmentId, DateTime start, int hours)
    {
        if (_clients.Find(clientId) is null) throw new KeyNotFoundException("Клієнта не знайдено.");
        var item = _inventory.Find(equipmentId) ?? throw new KeyNotFoundException("Інвентар не знайдено.");
        if (item.Status != EquipmentStatus.Available ||
            _rentals.GetAll().Any(r => r.InventoryId == equipmentId && r.Status == RentalStatus.Active))
            throw new InvalidOperationException("Відмова: інвентар недоступний.");
        if (start.Year is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(start));
        if (_rentals.Find(id) is not null) throw new ArgumentException("Номер прокату вже існує.");
        decimal cost = Quote(equipmentId, start, hours);
        Rental rental = new(id, clientId, equipmentId, start, hours, cost);
        item.Issue(); _inventory.Update(item);
        try { _rentals.Add(rental); }
        catch { item.Return(needsMaintenance: false); _inventory.Update(item); throw; }
        return rental;
    }

    public Rental Return(int id, DateTime returnedAt, bool needsMaintenance = false)
    {
        Rental rental = _rentals.Find(id) ?? throw new KeyNotFoundException("Прокат не знайдено.");
        if (rental.Status != RentalStatus.Active) throw new InvalidOperationException("Прокат уже завершено.");
        InventoryItem item = _inventory.Find(rental.InventoryId) ?? throw new KeyNotFoundException("Інвентар не знайдено.");
        if (item.Status != EquipmentStatus.Rented) throw new InvalidOperationException("Некоректний стан інвентарю.");
        if (returnedAt < rental.StartedAt) throw new ArgumentException("Дата повернення раніше видачі.");
        rental.Finish(returnedAt); _rentals.Update(rental);
        item.Return(needsMaintenance); _inventory.Update(item);
        return rental;
    }
    public Rental? Find(int id) => _rentals.Find(id);
    public IReadOnlyList<Rental> All() => _rentals.GetAll();
}

/// <summary>DFD 5: зведення за записаними прокатами; фільтр за статусом.</summary>
public sealed class ReportService
{
    private readonly IRepository<Rental> _rentals;
    public ReportService(IRepository<Rental> rentals) { _rentals = rentals; }
    public (int Count, decimal Total) Summary(RentalStatus? status = null)
    {
        var selected = _rentals.GetAll().Where(r => !status.HasValue || r.Status == status.Value).ToArray();
        return (selected.Length, selected.Sum(r => r.Cost));
    }
    public IReadOnlyList<Rental> Filter(RentalStatus? status = null) =>
        _rentals.GetAll().Where(r => !status.HasValue || r.Status == status.Value).ToArray();
}

/// <summary>Єдина точка складання шарів; файли зберігаються у data.</summary>
public sealed class ApplicationContext
{
    public IRepository<Client> Clients { get; }
    public IRepository<InventoryItem> Items { get; }
    public IRepository<Rental> Rentals { get; }
    public IRepository<Tariff> Tariffs { get; }
    public ClientService ClientService { get; }
    public InventoryService InventoryService { get; }
    public TariffService TariffService { get; }
    public RentalService RentalService { get; }
    public ReportService ReportService { get; }
    public string DataFolder { get; }

    public ApplicationContext(string dataFolder)
    {
        DataFolder = Path.GetFullPath(dataFolder);
        Directory.CreateDirectory(DataFolder);
        Clients = new JsonRepository<Client>(Path.Combine(DataFolder, "clients.json"));
        Items = new JsonRepository<InventoryItem>(Path.Combine(DataFolder, "inventory.json"));
        Rentals = new JsonRepository<Rental>(Path.Combine(DataFolder, "rentals.json"));
        Tariffs = new JsonRepository<Tariff>(Path.Combine(DataFolder, "tariffs.json"));
        ClientService = new ClientService(Clients, Rentals);
        InventoryService = new InventoryService(Items, Rentals);
        TariffService = new TariffService(Tariffs);
        RentalService = new RentalService(Rentals, Clients, Items, TariffService);
        ReportService = new ReportService(Rentals);
    }
}
