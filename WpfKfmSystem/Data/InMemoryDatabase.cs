using System.Collections.Concurrent;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Data;

/// <summary>
/// Singleton class for simulate a data base control by memory, 
/// using a thread-safe dictionary to store collections of different types.
/// </summary>
public sealed class InMemoryDatabase
{
    private static readonly Lazy<InMemoryDatabase> _instance = new(() => new InMemoryDatabase());

    public static InMemoryDatabase Instance => _instance.Value;

    // Thread-safe dictionary for stock any type colections
    private readonly ConcurrentDictionary<Type, object> _collections;

    private InMemoryDatabase()
    {
        _collections = new ConcurrentDictionary<Type, object>();       
    }

    /// <summary>
    /// Initialize a colection for a specific type, case not exists yet.
    /// </summary>
    private void InitializeCollection<T>() where T : BaseModel
    {
        _collections.TryAdd(typeof(T), new List<T>());
    }

    /// <summary>
    /// Get the generic colection from a especific type
    /// </summary>
    public List<T> GetCollection<T>() where T : BaseModel
    {
        var type = typeof(T);

        if (!_collections.ContainsKey(type))
        {
            InitializeCollection<T>();
        }

        return (List<T>)_collections[type];
    }

    /// <summary>
    /// Adds an item to a corresponding colection
    /// </summary>
    public void Add<T>(T item) where T : BaseModel
    {
        var collection = GetCollection<T>();

        // Auto-incremento do ID se for zero
        if (item.Id == 0)
        {
            item.Id = collection.Any() ? collection.Max(x => x.Id) + 1 : 1;
        }

        collection.Add(item);
    }

    /// <summary>
    /// Remove an item from the colection by Id
    /// </summary>
    public bool Remove<T>(int id) where T : BaseModel
    {
        var collection = GetCollection<T>();
        var item = collection.FirstOrDefault(x => x.Id == id);

        return item != null && collection.Remove(item);
    }

    /// <summary>
    /// Gets an item from Id
    /// </summary>
    public T? GetById<T>(int id) where T : BaseModel
    {
        return GetCollection<T>().FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    /// Gets all items by the type
    /// </summary>
    public List<T> GetAll<T>() where T : BaseModel
    {
        return GetCollection<T>();
    }

    /// <summary>
    /// Clears a colection by the type
    /// </summary>
    public void Clear<T>() where T : BaseModel
    {
        GetCollection<T>().Clear();
    }

    /// <summary>
    /// Clears all the colections
    /// </summary>
    public void ClearAllData()
    {
        foreach (var collection in _collections.Values)
        {
            if (collection is System.Collections.IList list)
            {
                list.Clear();
            }
        }
    }

    /// <summary>
    /// Retrieve a items count by the colection
    /// </summary>
    public int Count<T>() where T : BaseModel
    {
        return GetCollection<T>().Count;
    }
}
