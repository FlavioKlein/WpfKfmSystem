namespace WpfPorkProcessSystem.Models;

/// <summary>
/// Production Notes model.
/// Realize each weighing of the production order, 
/// with the date, shift, batch, hammer, weight, and other relevant details.
/// </summary>
public class ProductionNotesModel : BaseModel
{
    public int ProductionOrderId { get; set; }
    public int ProductId { get; set; }
    public ProductModel? Product { get; set; }
    public int WeighingScaleId { get; set; }
    public DateTime ExecutionDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime FacturingDate { get; set; }
    public string Shift { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string Hammer { get; set; } = string.Empty;

    public int? ClassificationId { get; set; }
    public int SprayChamberId { get; set; }
    public int Weight { get; set; }

    /// <summary>
    /// Helper property to determine if the item was imported from an entrance order 
    /// to an exit order on exit simulation.
    /// </summary>
    public bool ImportedToExitOrder { get; set; }
}