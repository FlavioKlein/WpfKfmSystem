using System.Reflection;
using WpfPorkProcessSystem.Data;
using WpfPorkProcessSystem.Interfaces;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Services;

/// <summary>
/// BaseService implements Repository patern
/// </summary>
public abstract class BaseService<TModel>: IBaseService<TModel> where TModel : BaseModel
{
    protected readonly InMemoryDatabase Database;

    protected BaseService()
    {
        Database = InMemoryDatabase.Instance;
    }

    /// <summary>
    /// Gets all rescords
    /// </summary>
    public virtual List<TModel> GetAll()
    {
        return Database.GetAll<TModel>();
    }

    /// <summary>
    /// Gets a record by ID
    /// </summary>
    public virtual TModel? GetById(int id)
    {
        return Database.GetById<TModel>(id);
    }


    /// <summary>
    /// Adds a new record
    /// </summary>
    public virtual void Add(TModel model)
    {
        Database.Add(model);
    }

    /// <summary>
    /// Updates an existing record.
    /// </summary>
    public virtual void Update(TModel model)
    {
        var registryModel = GetById(model.Id);

        if (registryModel == null)
        {
            throw new InvalidOperationException($"Registro com ID {model.Id} não encontrado.");
        }

        // Gets all public properties from TModel
        var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Ignore Id
            if (property.Name == nameof(BaseModel.Id))
                continue;

            // Ignore read only
            if (!property.CanWrite)
                continue;

            // Copy property value
            var value = property.GetValue(model);
            property.SetValue(registryModel, value);
        }
    }

    /// <summary>
    /// Remove a registry
    /// </summary>
    public virtual void Remove(int id)
    {
        Database.Remove<TModel>(id);
    }
}
