using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Interfaces;

public interface IBaseService<TModel> where TModel : BaseModel
{
    List<TModel> GetAll();

    TModel? GetById(int id);

    void Add(TModel model);

    void Update(TModel model);

    void Remove(int id);
}
