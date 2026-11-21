using System.Text.Json;
using SportsRental.Core.Models;

namespace SportsRental.Core.Repositories;

public interface IRepository<T> where T : class, IEntity
{
    void Add(T item);
    T? Find(int id);
    IReadOnlyList<T> GetAll();
    void Update(T item);
    bool Remove(int id);
}

/// <summary>Універсальне JSON-сховище. Дублікати ключів заборонені.</summary>
public sealed class JsonRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly string _path;
    private readonly Dictionary<int, T> _items = new();
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public JsonRepository(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        string? dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(_path)) { Persist(); return; }
        string json = File.ReadAllText(_path);
        var loaded = JsonSerializer.Deserialize<List<T>>(json, _json) ??
            throw new InvalidDataException($"У файлі {_path} немає масиву даних.");
        foreach (T item in loaded)
        {
            if (item is null || item.Id <= 0 || !_items.TryAdd(item.Id, item))
                throw new InvalidDataException($"Помилковий або повторний ключ у {_path}.");
        }
    }

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Id <= 0 || !_items.TryAdd(item.Id, item))
            throw new ArgumentException("Некоректний або повторний ідентифікатор.");
        try { Persist(); }
        catch { _items.Remove(item.Id); throw; }
    }

    public T? Find(int id) => _items.TryGetValue(id, out T? value) ? value : null;
    public IReadOnlyList<T> GetAll() => _items.OrderBy(p => p.Key).Select(p => p.Value).ToArray();

    public void Update(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id)) throw new KeyNotFoundException("Запис не знайдено.");
        _items[item.Id] = item;
        Persist();
    }

    public bool Remove(int id)
    {
        if (!_items.Remove(id, out T? old)) return false;
        try { Persist(); return true; }
        catch { _items[id] = old; throw; }
    }

    private void Persist()
    {
        string temp = _path + ".tmp";
        string json = JsonSerializer.Serialize(GetAll(), _json);
        File.WriteAllText(temp, json);
        File.Move(temp, _path, overwrite: true);
    }
}
