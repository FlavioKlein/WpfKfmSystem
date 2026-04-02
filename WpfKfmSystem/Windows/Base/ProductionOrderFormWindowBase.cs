using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows.Base;

/// <summary>
/// Class intermediate between ProductionOrderFormWindow and BaseFormWindow.
/// It's necessary because ProductionOrderFormWindow inherits from BaseFormWindow with generics types.
/// And the .xaml partial class not support generics types.
/// </summary>
public abstract class ProductionOrderFormWindowBase : BaseFormWindow<ProductionOrderModel, ProductionOrderService>
{
    public ProductionOrderFormWindowBase() : base()
    {
    }

    public ProductionOrderFormWindowBase(int id) : base(id)
    {
    }
}
