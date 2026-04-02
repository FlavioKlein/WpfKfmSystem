namespace WpfPorkProcessSystem.Models;

public class ClassificationWeighingModel : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public ProductModel? Product { get; set; }

    #region Entrance weighing limits
    public decimal LowerLimit { get; set; }
    public decimal UpperLimit { get; set; }
    #endregion
}
