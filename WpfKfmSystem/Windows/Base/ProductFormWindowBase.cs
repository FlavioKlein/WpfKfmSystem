using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows.Base;


/// <summary>
/// Class intermediate between ProductFormWindow and BaseFormWindow.
/// It's necessary because ProductFormWindow inherits from BaseFormWindow with generics types.
/// And the .xaml partial class not support generics types.
/// </summary>
public abstract class ProductFormWindowBase : BaseFormWindow<ProductModel, ProductService>
{
    public ProductFormWindowBase() : base()
    {
    }

    public ProductFormWindowBase(int id) : base(id)
    {
    }
}