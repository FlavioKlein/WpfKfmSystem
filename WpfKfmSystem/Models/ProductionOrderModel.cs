using WpfPorkProcessSystem.Enums;

namespace WpfPorkProcessSystem.Models;

/// <summary>
/// PO Production Order model.
/// </summary>
public class ProductionOrderModel : BaseModel
{
    public ProductionOrderStatusType Status { get; set; }
    /// <summary>
    /// Entrance or Leave spray chamber.
    /// </summary>
    public WeighingType Type { get; set; }

    /// <summary>
    /// Only in type SprayChamberExit for link to the corresponding entrance order.
    /// By simulations.
    /// </summary>
    public int? EntranceOrderNumber { get; set; }

    public int WeighingScaleId { get; set; }

    public DateTime ExecutionDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime FacturingDate { get; set; }

    public int ProductId { get; set; }
    public ProductModel? Product { get; set; }

    #region Entrance spray chamber properties
    public string Shift { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string Hammer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    #endregion

    public int QuantityCarcasses { get; set; }
    public int TotalWeighing { get; set; }

    #region Simulator

    /// <summary>
    /// Gets or sets the lower weight limit for the simulator.
    /// </summary>
    public int LowerLimitWeight { get; set; }

    /// <summary>
    /// Gets or sets the upper weight limit for the operation or calculation.   
    /// </summary>
    public int UpperLimitWeight { get; set; }

    /// <summary>
    /// Seconds delay between each record generated in the simulator.
    /// </summary>
    public int DelayGenerator { get; set; }

    /// <summary>
    /// Quantity of records to generate in the simulator.
    /// </summary>
    public int GenerateQuantity { get; set; }

    #endregion

    public List<ProductionOrderItemModel> Items { get; set; }
    public List<ProductionNotesModel> Notes { get; set; }

 }