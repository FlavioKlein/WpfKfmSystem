namespace WpfPorkProcessSystem.Models;

public class SprayChamberModel: BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Stock { get; set; }    
}