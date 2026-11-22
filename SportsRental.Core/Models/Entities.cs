using System.Text.Json.Serialization;

namespace SportsRental.Core.Models;

public interface IEntity { int Id { get; } }
public enum EquipmentType { Bike = 1, Skis = 2, Tent = 3 }
public enum EquipmentStatus { Available = 1, Rented = 2, Maintenance = 3 }
public enum RentalStatus { Active = 1, Returned = 2 }

/// <summary>Клієнт (DFD D2). Поле Id є незмінним ключем.</summary>
public sealed class Client : IEntity, IEquatable<Client>
{
    public const int MaxNameLength = 150;
    public int Id { get; }
    public string FullName { get; private set; }
    public string Phone { get; private set; }
    public string? Email { get; private set; }
    public DateTime RegisteredAt { get; }
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);

    public Client(int id, string fullName, string phone)
        : this(id, fullName, phone, null, DateTime.Today) { }

    [JsonConstructor]
    public Client(int id, string fullName, string phone, string? email, DateTime registeredAt)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
        FullName = "";
        Phone = "";
        Update(fullName, phone, email);
        RegisteredAt = registeredAt.Date;
        if (RegisteredAt.Year < 2000 || RegisteredAt > DateTime.Today)
            throw new ArgumentOutOfRangeException(nameof(registeredAt));
    }

    /// <summary>Редагує клієнта після перевірки всіх обов'язкових полів.</summary>
    public void Update(string name, string phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
            throw new ArgumentException("ПІБ має містити від 1 до 150 символів.");
        if (string.IsNullOrWhiteSpace(phone) || phone.Count(char.IsDigit) < 7 || phone.Length > 25)
            throw new ArgumentException("Телефон має містити щонайменше 7 цифр.");
        if (!string.IsNullOrWhiteSpace(email) &&
            (email.Contains(' ') || !email.Contains('@') || email.LastIndexOf('@') >= email.Length - 2 ||
             !email[(email.LastIndexOf('@') + 1)..].Contains('.')))
            throw new ArgumentException("Некоректна електронна адреса.");
        FullName = name.Trim(); Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    public string GetContact() => GetContact(includeEmail: false);
    public string GetContact(bool includeEmail) => includeEmail && HasEmail ? $"{Phone}; {Email}" : Phone;
    public static bool TryCreate(int id, string name, string phone, string? email,
        out Client? client, out string error)
    {
        try { client = new Client(id, name, phone, email, DateTime.Today); error = ""; return true; }
        catch (ArgumentException e) { client = null; error = e.Message; return false; }
    }
    public bool Equals(Client? other) => other is not null && Id == other.Id;
    public override bool Equals(object? obj) => obj is Client other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => $"#{Id} {FullName}; {GetContact(true)}";
}

/// <summary>Окрема фізична одиниця інвентарю (DFD D1).</summary>
public sealed class InventoryItem : IEntity
{
    public int Id { get; }
    public string Name { get; private set; }
    public EquipmentType Type { get; }
    public EquipmentStatus Status { get; private set; }

    public InventoryItem(int id, string name, EquipmentType type)
        : this(id, name, type, EquipmentStatus.Available) { }

    [JsonConstructor]
    public InventoryItem(int id, string name, EquipmentType type, EquipmentStatus status)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (!Enum.IsDefined(type) || !Enum.IsDefined(status)) throw new ArgumentException("Невідомий тип або стан.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new ArgumentException("Некоректна назва.");
        Id = id; Name = name.Trim(); Type = type; Status = status;
    }
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new ArgumentException("Некоректна назва.");
        Name = name.Trim();
    }
    /// <summary>Переводить інвентар у виданий стан. Зайнятий або несправний предмет видати не можна.</summary>
    public void Issue()
    {
        if (Status != EquipmentStatus.Available) throw new InvalidOperationException("Інвентар недоступний.");
        Status = EquipmentStatus.Rented;
    }
    /// <summary>Повертає виданий предмет у доступний стан або на обслуговування.</summary>
    public void Return(bool needsMaintenance)
    {
        if (Status != EquipmentStatus.Rented) throw new InvalidOperationException("Цей інвентар не виданий.");
        Status = needsMaintenance ? EquipmentStatus.Maintenance : EquipmentStatus.Available;
    }
    public void SendToMaintenance()
    {
        if (Status != EquipmentStatus.Available) throw new InvalidOperationException("Не можна ремонтувати виданий предмет.");
        Status = EquipmentStatus.Maintenance;
    }
    public void CompleteMaintenance()
    {
        if (Status != EquipmentStatus.Maintenance) throw new InvalidOperationException("Предмет не на обслуговуванні.");
        Status = EquipmentStatus.Available;
    }
    public override string ToString() => $"#{Id} {Name}, {Type}, {Status}";
}

/// <summary>Прокат (DFD D3). Вартість фіксується під час оформлення.</summary>
public sealed class Rental : IEntity
{
    public int Id { get; }
    public int ClientId { get; }
    public int InventoryId { get; }
    public DateTime StartedAt { get; private set; }
    public int Hours { get; private set; }
    public DateTime PlannedReturnAt => StartedAt.AddHours(Hours);
    public DateTime? ReturnedAt { get; private set; }
    public decimal Cost { get; private set; }
    public RentalStatus Status { get; private set; }

    public Rental(int id, int clientId, int inventoryId, DateTime startedAt, int hours, decimal cost)
        : this(id, clientId, inventoryId, startedAt, hours, cost, RentalStatus.Active, null) { }

    [JsonConstructor]
    public Rental(int id, int clientId, int inventoryId, DateTime startedAt, int hours,
        decimal cost, RentalStatus status, DateTime? returnedAt)
    {
        if (id <= 0 || clientId <= 0 || inventoryId <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (hours < 1 || hours > 336) throw new ArgumentOutOfRangeException(nameof(hours));
        if (cost < 0m) throw new ArgumentOutOfRangeException(nameof(cost));
        if (!Enum.IsDefined(status)) throw new ArgumentException("Некоректний статус прокату.");
        if (status == RentalStatus.Returned && returnedAt is null) throw new ArgumentException("Потрібна дата повернення.");
        if (returnedAt.HasValue && returnedAt.Value < startedAt) throw new ArgumentException("Повернення раніше видачі.");
        Id = id; ClientId = clientId; InventoryId = inventoryId; StartedAt = startedAt;
        Hours = hours; Cost = cost; Status = status; ReturnedAt = returnedAt;
    }
    /// <summary>Виправляє помилкові реквізити активного прокату. Завершені записи незмінні.</summary>
    public void CorrectActive(DateTime startedAt, int hours, decimal cost)
    {
        if (Status != RentalStatus.Active)
            throw new InvalidOperationException("Завершений прокат не можна змінити.");
        if (startedAt.Year is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(startedAt));
        if (hours is < 1 or > 336)
            throw new ArgumentOutOfRangeException(nameof(hours));
        if (cost < 0m)
            throw new ArgumentOutOfRangeException(nameof(cost));
        StartedAt = startedAt;
        Hours = hours;
        Cost = cost;
    }

    /// <summary>Завершує активний прокат, забороняючи повторне повернення.</summary>
    public void Finish(DateTime returnedAt)
    {
        if (Status != RentalStatus.Active) throw new InvalidOperationException("Прокат уже завершений.");
        if (returnedAt < StartedAt) throw new ArgumentException("Повернення раніше видачі.");
        Status = RentalStatus.Returned; ReturnedAt = returnedAt;
    }
    public override string ToString() =>
        $"#{Id} клієнт {ClientId}, інвентар {InventoryId}, {Hours} год, {Cost:F2} грн, {Status}";
}

/// <summary>Налаштування тарифу (DFD D4), навчальні ставки в гривнях за годину.</summary>
public sealed class Tariff : IEntity
{
    public int Id { get; }
    public EquipmentType Type { get; }
    public decimal HourlyRate { get; private set; }
    public decimal WeekendMultiplier { get; private set; }
    public decimal LongRentalMultiplier { get; private set; }
    public DateTime EffectiveFrom { get; }

    [JsonConstructor]
    public Tariff(int id, EquipmentType type, decimal hourlyRate,
        decimal weekendMultiplier, decimal longRentalMultiplier, DateTime effectiveFrom)
    {
        if (id <= 0 || !Enum.IsDefined(type)) throw new ArgumentException("Некоректний тариф.");
        Id = id; Type = type; HourlyRate = 0m; WeekendMultiplier = 1m; LongRentalMultiplier = 1m;
        Update(hourlyRate, weekendMultiplier, longRentalMultiplier);
        EffectiveFrom = effectiveFrom.Date;
    }
    public void Update(decimal rate, decimal weekend, decimal longRental)
    {
        if (rate <= 0m || rate > 100000m || weekend <= 0m || weekend > 3m || longRental <= 0m || longRental > 3m)
            throw new ArgumentOutOfRangeException(nameof(rate), "Недопустимі ставки або коефіцієнти.");
        HourlyRate = rate; WeekendMultiplier = weekend; LongRentalMultiplier = longRental;
    }
}

/// <summary>Період без власної ідентичності (структура з ПР8).</summary>
public readonly struct RentalPeriod
{
    public DateTime Start { get; }
    public DateTime End { get; }
    public RentalPeriod(DateTime start, DateTime end)
    {
        if (end <= start || (end - start).TotalHours > 336)
            throw new ArgumentException("Період має становити від 1 до 336 годин.");
        Start = start; End = end;
    }
    public int Hours => (int)Math.Ceiling((End - Start).TotalHours);
    public bool StartsOnWeekend => Start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public override string ToString() => $"{Start:dd.MM.yyyy HH:mm} — {End:dd.MM.yyyy HH:mm} ({Hours} год)";
}
