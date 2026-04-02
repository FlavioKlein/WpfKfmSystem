using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows.Base;

public abstract class ClassificationWeighingFormWindowBase : BaseFormWindow<ClassificationWeighingModel, ClassificationWeighingService>
{
    public ClassificationWeighingFormWindowBase() : base()
    {
    }

    public ClassificationWeighingFormWindowBase(int id) : base(id)
    {
    }
}