using System.Collections.Concurrent;

namespace SnowbreakTC.GameLogic.Resources;

public sealed class ResourceManager
{
    private readonly string _root;
    private readonly ConcurrentDictionary<string, object?> _tables = new();

    public bool IsLoaded { get; private set; }

    public string Root => _root;

    public ResourceManager(string root) => _root = root;

    public bool Init()
    {
        IsLoaded = Directory.Exists(_root);
        return IsLoaded;
    }

    public string? Raw(string path)
    {
        var file = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(file) ? File.ReadAllText(file) : null;
    }

    public List<T>? Table<T>(string path) where T : class
    {
        if (_tables.TryGetValue(path, out var cached))
            return cached as List<T>;

        var file = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(file))
        {
            _tables[path] = null;
            return null;
        }

        try
        {
            var table = System.Text.Json.JsonSerializer.Deserialize<List<T>>(File.ReadAllText(file));
            _tables[path] = table;
            return table;
        }
        catch
        {
            _tables[path] = null;
            return null;
        }
    }

    public int Count() => _tables.Count;
}
