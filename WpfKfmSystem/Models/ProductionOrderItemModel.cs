namespace WpfPorkProcessSystem.Models;

/// <summary>
/// PO Production Order Item model.
/// It's configure how distribute the carcasses and weighing of a production order 
/// in the different steps of the process, like classification, spray chamber, etc.
/// Used by both entrance and exit type orders:
/// - Entrance: Defines distribution of carcasses into chambers
/// - Exit: Defines sequence for removing carcasses from chambers
/// </summary>
public class ProductionOrderItemModel : BaseModel
{
    public int ProductionOrderId { get; set; }

    /// <summary>
    /// Determine the sequence of the organization to use for distribute carcasses.
    /// </summary>
    public int Sequential { get; set; }

    /// <summary>
    /// Chamber where the carcasses will be stocked.
    /// </summary>
    public int SprayChamberId { get; set; }

    /// <summary>
    /// Registry historical chamber capacity of carcasses.
    /// </summary>
    public int SprayChamberCapacity { get; set; }

    /// <summary>
    /// Registry historical chamber stock of carcasses.
    /// </summary>
    public int SprayChamberStock { get; set; }

    /// <summary>
    /// Registry historical chamber stock of carcasses.
    /// </summary>
    public int SprayChamberInitialStock { get; set; }

    /// <summary>
    /// String with the classifications names, separated by comma.
    /// Used to validate carcass for destinate to chamber.    
    /// </summary>
    public string AcceptClassifications { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of accepted classification identifiers.
    /// </summary>
    public int[] AcceptClassificationIds { get; set; } = [];

    /// <summary>
    /// Helper property to determine if the item was simulate executed.
    /// </summary>
    public bool SimulateExecuted { get; set; }
}