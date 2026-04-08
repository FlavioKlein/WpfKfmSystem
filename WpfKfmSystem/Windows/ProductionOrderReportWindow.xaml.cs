using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WpfPorkProcessSystem.Data;
using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows;

public partial class ProductionOrderReportWindow : Window
{
    private readonly ProductionOrderService _orderService;
    private readonly SprayChamberService _chamberService;
    private readonly ClassificationWeighingService _classificationService;
    private readonly InMemoryDatabase _database;

    public ProductionOrderReportWindow()
    {
        InitializeComponent();
        _orderService = new ProductionOrderService();
        _chamberService = new SprayChamberService();
        _classificationService = new ClassificationWeighingService();
        _database = InMemoryDatabase.Instance;

        InitializeData();
    }

    private void InitializeData()
    {
        // Load all finalized production orders (entrance and exit)
        var orders = _orderService.GetAll()
            .Where(o => o.Status == ProductionOrderStatusType.Finalized)
            .Where(o => o.QuantityCarcasses > 0)
            .OrderByDescending(o => o.Id)
            .ToList();

        CmbProductionOrder.ItemsSource = orders;
        
        if (orders.Any())
        {
            CmbProductionOrder.SelectedIndex = 0;
        }
        else
        {
            MessageBox.Show("No finalized production orders available.", "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnLoadReport_Click(object sender, RoutedEventArgs e)
    {
        if (CmbProductionOrder.SelectedItem is not ProductionOrderModel selectedOrder)
        {
            MessageBox.Show("Please select a production order.", "Validation", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadOrderReport(selectedOrder.Id);
    }

    private void LoadOrderReport(int orderId)
    {
        var order = _orderService.GetById(orderId);
        if (order == null)
        {
            MessageBox.Show("Production order not found.", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Update order info
        GridOrderInfo.Visibility = Visibility.Visible;
        TxtProduct.Text = order.Product?.Name ?? "N/A";
        TxtType.Text = order.Type.ToString();
        TxtTotalItems.Text = order.QuantityCarcasses.ToString();
        TxtTotalWeight.Text = $"{order.TotalWeighing} kg";

        if (order.Notes == null || !order.Notes.Any())
        {
            MessageBox.Show("This order has no notes.", "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            DgNotes.ItemsSource = null;
            return;
        }

        // Get all chambers and classifications
        var allChambers = _chamberService.GetAll();
        var allClassifications = _classificationService.GetAll();

        // Create report items
        var reportItems = new List<ProductionNoteReportModel>();

        foreach (var note in order.Notes)
        {
            var chamber = allChambers.FirstOrDefault(c => c.Id == note.SprayChamberId);
            var classification = allClassifications.FirstOrDefault(c => c.Id == note.ClassificationId);

            reportItems.Add(new ProductionNoteReportModel
            {
                Id = note.Id,
                SprayChamberId = note.SprayChamberId,
                ChamberName = chamber?.Name ?? $"Chamber {note.SprayChamberId}",
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

        // Group by chamber
        var groupedData = CollectionViewSource.GetDefaultView(reportItems);
        groupedData.GroupDescriptions.Clear();
        groupedData.GroupDescriptions.Add(new PropertyGroupDescription("ChamberName"));

        // Sort within groups by note ID
        groupedData.SortDescriptions.Clear();
        groupedData.SortDescriptions.Add(new System.ComponentModel.SortDescription("ChamberName", System.ComponentModel.ListSortDirection.Ascending));
        groupedData.SortDescriptions.Add(new System.ComponentModel.SortDescription("Id", System.ComponentModel.ListSortDirection.Ascending));

        DgNotes.ItemsSource = groupedData;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Model for production note report.
/// </summary>
public class ProductionNoteReportModel
{
    public int Id { get; set; }
    public int SprayChamberId { get; set; }
    public string ChamberName { get; set; } = string.Empty;
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
