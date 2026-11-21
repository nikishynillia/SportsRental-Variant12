namespace SportsRental.Core.Models;

/// <summary>Абстрактна модель тарифікації обладнання (ПР9).</summary>
public abstract class RentalItem
{
    public const int LongRentalThresholdHours = 72;
    public const int MaxRentalHours = 336;
    protected Tariff Tariff { get; }
    public bool StartsOnWeekend { get; }
    public int InventoryId { get; }

    protected RentalItem(int inventoryId, bool weekend, Tariff tariff)
    {
        if (inventoryId <= 0) throw new ArgumentOutOfRangeException(nameof(inventoryId));
        Tariff = tariff ?? throw new ArgumentNullException(nameof(tariff));
        InventoryId = inventoryId;
        StartsOnWeekend = weekend;
    }
    public abstract decimal CalculateRent(int hours);

    /// <summary>Чотири комбінації двох умов із таблиці рішень ПР7.</summary>
    protected decimal CalculateByDecisionTable(int hours)
    {
        if (hours is < 1 or > MaxRentalHours) throw new ArgumentOutOfRangeException(nameof(hours));
        decimal multiplier = (StartsOnWeekend, hours > LongRentalThresholdHours) switch
        {
            (true, true) => Tariff.WeekendMultiplier * Tariff.LongRentalMultiplier,
            (true, false) => Tariff.WeekendMultiplier,
            (false, true) => Tariff.LongRentalMultiplier,
            (false, false) => 1m
        };
        return decimal.Round(Tariff.HourlyRate * hours * multiplier, 2, MidpointRounding.AwayFromZero);
    }

    public virtual string GetDescription() => $"{GetType().Name}: {Tariff.HourlyRate:F2} грн/год";
}

public sealed class Bike : RentalItem
{
    public Bike(int id, bool weekend, Tariff tariff) : base(id, weekend, tariff)
    {
        if (tariff.Type != EquipmentType.Bike) throw new ArgumentException("Тариф має бути для велосипеда.");
    }
    public override decimal CalculateRent(int hours) => CalculateByDecisionTable(hours);
    public override string GetDescription() => base.GetDescription() + "; міський велосипед";
}
public sealed class Skis : RentalItem
{
    public Skis(int id, bool weekend, Tariff tariff) : base(id, weekend, tariff)
    {
        if (tariff.Type != EquipmentType.Skis) throw new ArgumentException("Тариф має бути для лиж.");
    }
    public override decimal CalculateRent(int hours) => CalculateByDecisionTable(hours);
}
public sealed class Tent : RentalItem
{
    public int Capacity { get; }
    public Tent(int id, bool weekend, Tariff tariff, int capacity = 4) : base(id, weekend, tariff)
    {
        if (tariff.Type != EquipmentType.Tent) throw new ArgumentException("Тариф має бути для намету.");
        if (capacity is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }
    public override decimal CalculateRent(int hours) => CalculateByDecisionTable(hours);
    // Навмисне new для демонстрації відмінності від override з практичної роботи №9.
    public new string GetDescription() => base.GetDescription() + $"; місць: {Capacity}";
}
