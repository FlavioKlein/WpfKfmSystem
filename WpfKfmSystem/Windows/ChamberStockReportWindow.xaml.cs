using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfPorkProcessSystem.Data;
using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows;

public partial class ChamberStockReportWindow : Window
{
    private readonly SprayChamberService _chamberService;
    private readonly ClassificationWeighingService _classificationService;
    private readonly InMemoryDatabase _database;
    private List<StockItemReportModel> _stockItems;

    public ChamberStockReportWindow()
    {
        InitializeComponent();
        _chamberService = new SprayChamberService();
        _classificationService = new ClassificationWeighingService();
        _database = InMemoryDatabase.Instance;
        _stockItems = new List<StockItemReportModel>();

        InitializeData();
    }

    private void InitializeData()
    {
        // Load only chambers with stock > 0
        var chambers = _chamberService.GetAll()
            .Where(c => c.Stock > 0)
            .OrderBy(c => c.Name)
            .ToList();

        CmbSprayChamber.ItemsSource = chambers;
        
        if (chambers.Any())
        {
            CmbSprayChamber.SelectedIndex = 0;
        }
        else
        {
            MessageBox.Show("No chambers with stock available.", "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnLoadReport_Click(object sender, RoutedEventArgs e)
    {
        if (CmbSprayChamber.SelectedItem is not SprayChamberModel selectedChamber)
        {
            MessageBox.Show("Please select a spray chamber.", "Validation", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadStockReport(selectedChamber.Id);
    }

    private void LoadStockReport(int chamberId)
    {
        _stockItems.Clear();

        var chamber = _chamberService.GetById(chamberId);
        if (chamber == null || chamber.Stock == 0)
        {
            MessageBox.Show("Selected chamber has no stock.", "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            DgStockItems.ItemsSource = null;
            TxtSummary.Text = string.Empty;
            return;
        }

        // Get all production orders
        var allOrders = _database.GetAll<ProductionOrderModel>();

        // Find all ENTRANCE orders for this chamber (most recent first)
        var entranceOrders = allOrders
            .Where(o => o.Type == WeighingType.SprayChamberEntrance)
            .Where(o => o.Status == ProductionOrderStatusType.Finalized)
            .Where(o => o.Notes != null && o.Notes.Any(n => n.SprayChamberId == chamberId))
            .OrderByDescending(o => o.Id)
            .ToList();

        if (!entranceOrders.Any())
        {
            MessageBox.Show("No entrance orders found for this chamber.", "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            DgStockItems.ItemsSource = null;
            TxtSummary.Text = string.Empty;
            return;
        }

        // For each entrance order, find notes that were NOT processed by exit orders
        var allExitOrders = allOrders
            .Where(o => o.Type == WeighingType.SprayChamberExit)
            .Where(o => o.Status == ProductionOrderStatusType.Finalized)
            .ToList();

        var allClassifications = _classificationService.GetAll();

        foreach (var entranceOrder in entranceOrders)
        {
            if (entranceOrder.Notes == null) continue;

            // Get notes for this chamber in this entrance order
            var chamberNotes = entranceOrder.Notes
                .Where(n => n.SprayChamberId == chamberId)
                .ToList();

            foreach (var note in chamberNotes)
            {
                // Check if this note was already processed by an exit order
                bool wasProcessed = false;

                foreach (var exitOrder in allExitOrders)
                {
                    // Check if this exit order is linked to this entrance order
                    if (exitOrder.EntranceOrderNumber == entranceOrder.Id)
                    {
                        // Check if this specific note was processed (same ID)
                        if (exitOrder.Notes != null && exitOrder.Notes.Any(en => en.Id == note.Id))
                        {
                            wasProcessed = true;
                            break;
                        }
                    }
                }

                // If not processed, add to report
                if (!wasProcessed)
                {
                    var classification = allClassifications.FirstOrDefault(c => c.Id == note.ClassificationId);

                    _stockItems.Add(new StockItemReportModel
                    {
                        Id = note.Id,
                        ProductionOrderId = note.ProductionOrderId,
                        Product = note.Product,
                        ClassificationId = note.ClassificationId,
                        ClassificationName = classification?.Name ?? "N/A",
                        Weight = note.Weight,
                        ExecutionDate = note.ExecutionDate,
                        ExpirationDate = note.ExpirationDate,
                        FacturingDate = note.FacturingDate,
                        Shift = note.Shift,
                        Batch = note.Batch,
                        Hammer = note.Hammer
                    });
                }
            }
        }

        // Order by oldest first (FIFO)
        _stockItems = _stockItems.OrderBy(s => s.Id).ToList();

        DgStockItems.ItemsSource = _stockItems;

        // Update summary
        var totalWeight = _stockItems.Sum(s => s.Weight);
        TxtSummary.Text = $"Total Items: {_stockItems.Count} | Total Weight: {totalWeight} kg | Chamber Stock: {chamber.Stock}";
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Model for stock item report.
/// </summary>
public class StockItemReportModel
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public ProductModel? Product { get; set; }
    public int? ClassificationId { get; set; }
    public string ClassificationName { get; set; } = string.Empty;
    public int Weight { get; set; }
    public DateTime ExecutionDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime FacturingDate { get; set; }
    public string Shift { get; set; } = string.Empty;
    public string Batch { get; set; } = string.Empty;
    public string Hammer { get; set; } = string.Empty;
}
