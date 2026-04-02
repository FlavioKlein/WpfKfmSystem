using System.Timers;
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
    private readonly System.Timers.Timer _timer;
    private ProductionOrderModel? _currentOrder;
    private int _generatedCount;
    private bool _isRunning;
    private readonly Random _random;

    public event EventHandler<SimulatorEventArgs>? DataGenerated;
    public event EventHandler<SimulatorEventArgs>? SimulatorFinalized;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsRunning => _isRunning;
    public int GeneratedCount => _generatedCount;

    public ProductionOrderSimulatorService()
    {
        _database = InMemoryDatabase.Instance;
        _timer = new System.Timers.Timer();
        _timer.Elapsed += OnTimerElapsed;
        _random = new Random();
    }

    /// <summary>
    /// Starts the simulator for a specific production order.
    /// </summary>
    public void Start(int orderId)
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

        if (_currentOrder.DelayGenerator <= 0)
        {
            throw new ArgumentException("Delay generator must be greater than zero.");
        }

        if (_currentOrder.GenerateQuantity <= 0)
        {
            throw new ArgumentException("Generate quantity must be greater than zero.");
        }

        _generatedCount = 0;
        _timer.Interval = _currentOrder.DelayGenerator * 1000; // Convert to milliseconds
        _isRunning = true;
        _timer.Start();
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
        _timer.Stop();
    }

    /// <summary>
    /// Resumes the simulator.
    /// </summary>
    public void Resume()
    {
        if (_isRunning || _currentOrder == null)
        {
            return;
        }

        _isRunning = true;
        _timer.Start();
    }

    /// <summary>
    /// Stops and finalizes the simulator.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _timer.Stop();
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

        // Reset item stocks for entrance orders
        if (order.Type == WeighingType.SprayChamberEntrance && order.Items != null)
        {
            foreach (var item in order.Items)
            {
                item.SprayChamberStock = item.SprayChamberInitialStock;
            }
        }

        // Reset status to Pending
        order.Status = ProductionOrderStatusType.Pending;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_currentOrder == null || !_isRunning)
        {
            return;
        }

        try
        {
            // Generate one record
            if (_currentOrder.Type == WeighingType.SprayChamberEntrance)
            {
                GenerateEntranceRecord();
            }
            else if (_currentOrder.Type == WeighingType.SprayChamberExit)
            {
                GenerateExitRecord();
            }

            _generatedCount++;

            // Raise event
            DataGenerated?.Invoke(this, new SimulatorEventArgs(_currentOrder.Id, _generatedCount));

            // Check if reached limit
            if (_generatedCount >= _currentOrder.GenerateQuantity)
            {
                FinalizeSimulation();
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            Stop();
        }
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
        if (_currentOrder == null)
        {
            throw new InvalidOperationException("No current order configured.");
        }

        // Get entrance order
        ProductionOrderModel? entranceOrder = null;
        
        if (_currentOrder.EntranceOrderNumber.HasValue)
        {
            var allOrders = _database.GetAll<ProductionOrderModel>();
            entranceOrder = allOrders.FirstOrDefault(o => o.Id == _currentOrder.EntranceOrderNumber.Value);
        }

        if (entranceOrder == null)
        {
            throw new InvalidOperationException("Entrance order not found or not configured.");
        }

        if (entranceOrder.Notes == null || entranceOrder.Notes.Count == 0)
        {
            throw new InvalidOperationException("Entrance order has no notes to process.");
        }

        // Find items with available stock
        var itemsWithStock = _currentOrder.Items?
            .Where(i => i.SprayChamberStock < i.SprayChamberInitialStock)
            .ToList();

        if (itemsWithStock == null || itemsWithStock.Count == 0)
        {
            throw new InvalidOperationException("No items with available stock for exit.");
        }

        // Select a random item
        var randomItemIndex = _random.Next(0, itemsWithStock.Count);
        var selectedItem = itemsWithStock[randomItemIndex];

        // Find notes from entrance order for this chamber
        var availableNotes = entranceOrder.Notes
            .Where(n => n.SprayChamberId == selectedItem.SprayChamberId)
            .Where(n => !_currentOrder.Notes.Any(en => en.Id == n.Id && en.SprayChamberId == n.SprayChamberId))
            .ToList();

        if (availableNotes.Count == 0)
        {
            throw new InvalidOperationException($"No available notes for chamber {selectedItem.SprayChamberId}.");
        }

        // Select a random note
        var randomNoteIndex = _random.Next(0, availableNotes.Count);
        var selectedNote = availableNotes[randomNoteIndex];

        // Create exit note
        var exitNote = new ProductionNotesModel
        {
            Id = selectedNote.Id,
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
            Weight = selectedNote.Weight
        };

        // Update stocks and counters
        var chamber = _database.GetById<SprayChamberModel>(selectedItem.SprayChamberId);
        if (chamber != null)
        {
            chamber.Stock--;
        }

        selectedItem.SprayChamberStock++;
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
        _timer?.Dispose();
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
