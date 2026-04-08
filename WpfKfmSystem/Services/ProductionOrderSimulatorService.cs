using WpfPorkProcessSystem.Data;
using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Services;

/// <summary>
/// Service for simulating production order operations (entrance and exit).
/// Works as an independent thread that generates data based on configuration parameters.
/// </summary>
public class ProductionOrderSimulatorService : IDisposable
{
    private readonly InMemoryDatabase _database;
    private readonly Random _random;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _simulationTask;
    private ProductionOrderModel? _currentOrder;
    private int _generatedCount;
    private bool _isRunning;
    private ProductionOrderModel? _originOrder;

    public event EventHandler<SimulatorEventArgs>? DataGenerated;
    public event EventHandler<SimulatorEventArgs>? SimulatorFinalized;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsRunning => _isRunning;
    public int GeneratedCount => _generatedCount;

    public ProductionOrderSimulatorService()
    {
        _database = InMemoryDatabase.Instance;
        _random = new Random();
    }

    /// <summary>
    /// Starts the simulator for a specific production order.
    /// </summary>
    public async Task StartAsync(int orderId)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Simulator is already running.");
        }

        _currentOrder = _database.GetById<ProductionOrderModel>(orderId);

        if (_currentOrder == null)
        {
            throw new ArgumentException($"Production order with ID {orderId} not found.");
        }

        if (_currentOrder.Status != ProductionOrderStatusType.Executing)
        {
            throw new InvalidOperationException("Production order must be in Executing status to start simulation.");
        }

        if (_currentOrder.DelayGenerator < 0)
        {
            throw new ArgumentException("Delay generator must be non-negative.");
        }

        if (_currentOrder.Type == WeighingType.SprayChamberEntrance)
        {
            if (_currentOrder.GenerateQuantity <= 0)
            {
                throw new ArgumentException("Generate quantity must be greater than zero for entrance orders.");
            }
        }

        _generatedCount = 0;
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();

        // Start simulation loop in background
        _simulationTask = Task.Run(() => SimulateLoopAsync(_cancellationTokenSource.Token));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Main simulation loop - generates records with delay between each one.
    /// </summary>
    private async Task SimulateLoopAsync(CancellationToken cancellationToken)
    {
        try
        {

            if (_currentOrder?.Type == WeighingType.SprayChamberExit)
            {
                PrepareOringinOrder();
            }

            while (!cancellationToken.IsCancellationRequested && _currentOrder != null)
            {
                // Check if we should continue based on order type
                if (_currentOrder.Type == WeighingType.SprayChamberEntrance)
                {
                    if (_generatedCount >= _currentOrder.GenerateQuantity)
                    {
                        FinalizeSimulation();
                        break;
                    }

                    GenerateEntranceRecord();
                }
                else if (_currentOrder.Type == WeighingType.SprayChamberExit)
                {
                    if (!HasMoreItemsToExit())
                    {
                        FinalizeSimulation();
                        break;
                    }

                    GenerateExitRecord();
                }

                _generatedCount++;

                // Raise event to notify UI
                DataGenerated?.Invoke(this, new SimulatorEventArgs(_currentOrder.Id, _generatedCount));

                // Apply delay before next generation
                if (_currentOrder.DelayGenerator > 0)
                {
                    await Task.Delay(_currentOrder.DelayGenerator, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation, no error
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            Stop();
        }
    }

    private void PrepareOringinOrder()
    {
        _originOrder = _database.GetById<ProductionOrderModel>(_currentOrder.EntranceOrderNumber.Value);

        if (_originOrder == null)
        {
            throw new InvalidOperationException("Entrance order not found or not configured.");
        }

        if (_originOrder.Notes == null || _originOrder.Notes.Count == 0)
        {
            throw new InvalidOperationException("Entrance order has no notes to process.");
        }
    }

    /// <summary>
    /// Pauses the simulator.
    /// </summary>
    public void Pause()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Resumes the simulator.
    /// </summary>
    public async Task ResumeAsync()
    {
        if (_isRunning || _currentOrder == null)
        {
            return;
        }

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _simulationTask = Task.Run(() => SimulateLoopAsync(_cancellationTokenSource.Token));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops and finalizes the simulator.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        _currentOrder = null;
        _generatedCount = 0;
    }

    /// <summary>
    /// Clears all simulation data and reverts chamber stocks.
    /// </summary>
    public void ClearSimulation(int orderId)
    {
        var order = _database.GetById<ProductionOrderModel>(orderId);
        
        if (order == null)
        {
            throw new ArgumentException($"Production order with ID {orderId} not found.");
        }

        // Stop simulator if running
        if (_isRunning && _currentOrder?.Id == orderId)
        {
            Stop();
        }

        // Revert chamber stocks based on order type
        if (order.Type == WeighingType.SprayChamberEntrance)
        {
            // For entrance orders, decrement stocks
            foreach (var note in order.Notes)
            {
                var chamber = _database.GetById<SprayChamberModel>(note.SprayChamberId);
                if (chamber != null)
                {
                    chamber.Stock--;
                }
            }
        }
        else if (order.Type == WeighingType.SprayChamberExit)
        {
            // For exit orders, increment stocks
            foreach (var note in order.Notes)
            {
                var chamber = _database.GetById<SprayChamberModel>(note.SprayChamberId);
                if (chamber != null)
                {
                    chamber.Stock++;
                }
            }
        }

        // Clear notes and reset counters
        order.Notes.Clear();
        order.QuantityCarcasses = 0;
        order.TotalWeighing = 0;

        // Reset item stocks
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                if (order.Type == WeighingType.SprayChamberEntrance)
                {
                    // For entrance: revert to initial stock
                    item.SprayChamberStock = item.SprayChamberInitialStock;
                }
                else if (order.Type == WeighingType.SprayChamberExit)
                {
                    // For exit: reset processed count to 0
                    item.SprayChamberStock = 0;
                }
            }
        }

        // Reset status to Pending
        order.Status = ProductionOrderStatusType.Pending;
    }

    /// <summary>
    /// Checks if there are more items available to exit from chambers.
    /// </summary>
    private bool HasMoreItemsToExit()
    {
        if (_currentOrder == null || _currentOrder.Items == null || _originOrder == null)
            return false;

        var isPendingItems = _currentOrder.Items.Any(x => x.SprayChamberStock > 0);

        if (isPendingItems && _originOrder.Notes.Any(x => !x.ImportedToExitOrder))
        {
           return true; 
        }

        return false; 
    }

    private void GenerateEntranceRecord()
    {
        if (_currentOrder == null || _currentOrder.Items == null || _currentOrder.Items.Count == 0)
        {
            throw new InvalidOperationException("No items configured for entrance order.");
        }

        // Step 1: Generate random weight for the carcass
        int weight;
        if (_currentOrder.LowerLimitWeight > 0 && _currentOrder.UpperLimitWeight > 0)
        {
            // Use order configuration limits
            weight = _random.Next(_currentOrder.LowerLimitWeight, _currentOrder.UpperLimitWeight + 1);
        }
        else
        {
            // Use default range if not configured
            weight = _random.Next(50, 150);
        }

        // Step 2: Classify the carcass based on its weight
        // Get all classifications for the product
        var allClassifications = _database.GetAll<ClassificationWeighingModel>()
            .Where(c => c.ProductId == _currentOrder.ProductId)
            .ToList();

        if (allClassifications.Count == 0)
        {
            throw new InvalidOperationException($"No classifications found for product {_currentOrder.ProductId}.");
        }

        // Find the classification that matches the weight
        var matchingClassification = allClassifications
            .FirstOrDefault(c => weight >= c.LowerLimit && weight <= c.UpperLimit);

        if (matchingClassification == null)
        {
            throw new InvalidOperationException($"Weight {weight} does not match any classification for product {_currentOrder.ProductId}.");
        }

        // Step 3: Find the first chamber in sequential order that accepts this classification and has capacity
        var orderedItems = _currentOrder.Items.OrderBy(i => i.Sequential).ToList();

        ProductionOrderItemModel? selectedItem = null;
        SprayChamberModel? selectedChamber = null;

        foreach (var item in orderedItems)
        {
            // Check if this item accepts the classification
            if (item.AcceptClassificationIds == null || 
                !item.AcceptClassificationIds.Contains(matchingClassification.Id))
            {
                continue; // This chamber doesn't accept this classification
            }

            // Get chamber and check capacity
            var chamber = _database.GetById<SprayChamberModel>(item.SprayChamberId);
            if (chamber == null)
            {
                continue; // Chamber not found, skip
            }

            if (chamber.Stock >= chamber.Capacity)
            {
                continue; // Chamber is full, try next one
            }

            // Found a suitable chamber!
            selectedItem = item;
            selectedChamber = chamber;
            break;
        }

        // Step 4: Validate that we found a chamber
        if (selectedItem == null || selectedChamber == null)
        {
            throw new InvalidOperationException(
                $"No available chamber found for classification '{matchingClassification.Name}' (weight: {weight}). " +
                $"All chambers are either full or don't accept this classification.");
        }

        // Step 5: Create production note
        var note = new ProductionNotesModel
        {
            ProductionOrderId = _currentOrder.Id,
            ProductId = _currentOrder.ProductId,
            Product = _currentOrder.Product,
            ExecutionDate = _currentOrder.ExecutionDate,
            FacturingDate = _currentOrder.FacturingDate,
            ExpirationDate = _currentOrder.ExpirationDate,
            WeighingScaleId = _currentOrder.WeighingScaleId,
            Shift = _currentOrder.Shift,
            Batch = _currentOrder.Batch,
            Hammer = _currentOrder.Hammer,
            SprayChamberId = selectedItem.SprayChamberId,
            ClassificationId = matchingClassification.Id,
            Weight = weight
        };

        // Step 6: Update stocks and counters
        selectedChamber.Stock++;
        selectedItem.SprayChamberStock++;
        _currentOrder.QuantityCarcasses++;
        _currentOrder.TotalWeighing += weight;
        _currentOrder.Notes.Add(note);
    }

    private void GenerateExitRecord()
    {
        var orderedItem = _currentOrder?.Items?.Where(x => !x.SimulateExecuted).OrderBy(i => i.Sequential).FirstOrDefault();

        if (orderedItem == null) return;

        var selectedNote = _originOrder?.Notes.Where(x => x.SprayChamberId == orderedItem?.SprayChamberId
                        && !x.ImportedToExitOrder).OrderBy(x => x.Id).FirstOrDefault();

        if (selectedNote == null)
        {
            orderedItem.SimulateExecuted = true;
            return;
        }

        // Mark note as imported to exit order to avoid reuse
        selectedNote.ImportedToExitOrder = true;


        // Create exit note (preserving all data from entrance)
        var exitNote = new ProductionNotesModel
        {
            Id = selectedNote.Id, // Keep same ID to track which entrance item was exited
            ProductionOrderId = _currentOrder.Id,
            ProductId = _currentOrder.ProductId,
            Product = _currentOrder.Product,
            ExecutionDate = _currentOrder.ExecutionDate,
            FacturingDate = _currentOrder.FacturingDate,
            ExpirationDate = _currentOrder.ExpirationDate,
            WeighingScaleId = _currentOrder.WeighingScaleId,
            Shift = _currentOrder.Shift,
            Batch = _currentOrder.Batch,
            Hammer = _currentOrder.Hammer,
            SprayChamberId = selectedNote.SprayChamberId,
            ClassificationId = selectedNote.ClassificationId,
            Weight = selectedNote.Weight // Preserve original weight
        };

        // Update stocks and counters
        var chamber = _database.GetById<SprayChamberModel>(orderedItem.SprayChamberId);
        if (chamber != null)
        {
            chamber.Stock--; // Decrement chamber stock (item leaving)            
        }

        // For exit orders, SprayChamberStock tracks how many have been processed
        orderedItem.SprayChamberStock--; // Decrement stock
        _currentOrder.QuantityCarcasses++;
        _currentOrder.TotalWeighing += exitNote.Weight;
        _currentOrder.Notes.Add(exitNote);
    }

    private void FinalizeSimulation()
    {
        if (_currentOrder == null)
        {
            return;
        }

        // Update status to Finalized
        _currentOrder.Status = ProductionOrderStatusType.Finalized;

        // Raise event
        SimulatorFinalized?.Invoke(this, new SimulatorEventArgs(_currentOrder.Id, _generatedCount));

        // Stop simulator
        Stop();
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// Event args for simulator events.
/// </summary>
public class SimulatorEventArgs : EventArgs
{
    public int OrderId { get; }
    public int GeneratedCount { get; }

    public SimulatorEventArgs(int orderId, int generatedCount)
    {
        OrderId = orderId;
        GeneratedCount = generatedCount;
    }
}
