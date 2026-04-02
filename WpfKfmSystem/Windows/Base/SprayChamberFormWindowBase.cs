using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows.Base;

public abstract class SprayChamberFormWindowBase : BaseFormWindow<SprayChamberModel, SprayChamberService>
{
    public SprayChamberFormWindowBase() : base()
    {
    }

    public SprayChamberFormWindowBase(int id) : base(id)
    {
    }
}