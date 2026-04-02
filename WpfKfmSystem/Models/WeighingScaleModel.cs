using WpfPorkProcessSystem.Enums;

namespace WpfPorkProcessSystem.Models;

public class WeighingScaleModel : BaseModel
{
    public string Name { get; set; } = string.Empty;

    public WeighingScaleType Type { get; set; }
}
