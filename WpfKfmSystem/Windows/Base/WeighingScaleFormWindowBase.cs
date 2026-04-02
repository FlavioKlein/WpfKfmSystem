using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows.Base;

public abstract class WeighingScaleFormWindowBase : BaseFormWindow<WeighingScaleModel, WeighingScaleService>
{
    public WeighingScaleFormWindowBase() : base()
    {
    }

    public WeighingScaleFormWindowBase(int id) : base(id)
    {
    }
}
