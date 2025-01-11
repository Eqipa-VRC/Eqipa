using Newtonsoft.Json;
using Eqipa.Util;

namespace Eqipa.Model;

public class Registry<T> where T : class, new()
{
  private readonly string _folderPath;
  private readonly Dictionary<string, T> _cache = new();

  public Registry(string name)
  {
    _folderPath = Path.Combine("registry", name);
    Directory.CreateDirectory(_folderPath);
    LoadAllFiles();
    EnsureConsistency();
  }

  private void LoadAllFiles()
  {
    foreach (var file in Directory.GetFiles(_folderPath, "*.json"))
    {
      var id = Path.GetFileNameWithoutExtension(file);
      var content = File.ReadAllText(file);

      try
      {
        _cache[id] = JsonConvert.DeserializeObject<T>(content) ?? new T();
      }
      catch (JsonException ex)
      {
        Logger.Log(LogLevel.Warn, $"Failed to load entity {id}: {ex.Message}. Attempting to regenerate with default values.");
        var defaultItem = new T();
        Save(id, defaultItem);
      }
    }
  }

  private void EnsureConsistency()
  {
    foreach (var id in _cache.Keys.ToList())
    {
      var item = _cache[id];
      try
      {
        var serialized = JsonConvert.SerializeObject(item, Formatting.Indented);
        var deserialized = JsonConvert.DeserializeObject<T>(serialized) ?? new T();

        var currentFields = typeof(T).GetProperties();
        var deserializedFields = typeof(T).GetProperties();

        bool needsUpdate = false;
        foreach (var field in currentFields)
        {
          var currentValue = field.GetValue(item);
          var deserializedValue = field.GetValue(deserialized);

          if (currentValue == null && deserializedValue != null)
          {
            field.SetValue(item, deserializedValue);
            needsUpdate = true;
          }
        }

        if (needsUpdate)
        {
          Save(id, item);
        }
      }
      catch (JsonException ex)
      {
        Logger.Log(LogLevel.Warn, $"Consistency check failed for entity {id}: {ex.Message}. Regenerating with default values.");
        var defaultItem = new T();
        Save(id, defaultItem);
      }
    }
  }

  public void Save(string id, T item)
  {
    var path = Path.Combine(_folderPath, $"{id}.json");
    File.WriteAllText(path, JsonConvert.SerializeObject(item, Formatting.Indented));
    _cache[id] = item;

    Logger.Log(LogLevel.Info, $"Saved entity {id}");
  }

  public void Update(string id, Action<T> updateAction)
  {
    if (_cache.TryGetValue(id, out var item))
    {
      updateAction(item);
      Save(id, item);

      Logger.Log(LogLevel.Info, $"Updated entity {id}");
    }
    else
    {
      Logger.Log(LogLevel.Warn, $"Entity {id} not found for update.");
    }
  }

  public void UpdateAll(Action<T> updateAction)
  {
    foreach (var id in _cache.Keys.ToList())
    {
      var item = _cache[id];
      updateAction(item);
      Save(id, item);
    }

    Logger.Log(LogLevel.Info, $"Updated all entities.");
  }

  public void Delete(string id)
  {
    var path = Path.Combine(_folderPath, $"{id}.json");
    if (File.Exists(path))
    {
      File.Delete(path);
      _cache.Remove(id);

      Logger.Log(LogLevel.Info, $"Deleted entity {id}");
    }
    else
    {
      Logger.Log(LogLevel.Warn, $"Entity {id} not found for deletion.");
    }
  }

  public bool Has(string id) => _cache.ContainsKey(id);

  public T? Get(string id) => _cache.TryGetValue(id, out var item) ? item : null;

  public List<T> GetAll() => _cache.Values.ToList();

  public int GetCountByCondition(Func<T, bool> condition)
  {
    return _cache.Values.Count(condition);
  }

  public List<T> GetAllFiltered(Func<T, bool> condition)
  {
    return _cache.Values.Where(condition).ToList();
  }
}