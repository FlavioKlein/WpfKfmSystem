using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

/// <summary>
/// Production Order list window - Displays only header data from ProductionOrderModel
/// Inherits all CRUD behavior from BaseListWindow
/// </summary>
public class ProductionOrderListWindow : BaseListWindow<ProductionOrderModel, ProductionOrderService>
{
    public ProductionOrderListWindow()
    {
        // All the UI is created automaticaly by the base class constructor
    }

    protected override string GetTitle() => "Production Orders";

    /// <summary>
    /// Configures DataGrid columns - Only header data
    /// </summary>
    protected override void DatagridColumnsConfig(DataGrid dataGrid)
    {
        dataGrid.Columns.Clear();

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "ID",
            Binding = new Binding("Id"),
            Width = new DataGridLength(60)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Order #",
            Binding = new Binding("OrderNumber"),
            Width = new DataGridLength(80)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Status",
            Binding = new Binding("Status"),
            Width = new DataGridLength(100)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Type",
            Binding = new Binding("Type"),
            Width = new DataGridLength(150)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Product",
            Binding = new Binding("Product.Name"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Execution Date",
            Binding = new Binding("ExecutionDate") { StringFormat = "dd/MM/yyyy" },
            Width = new DataGridLength(120)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Qty Carcasses",
            Binding = new Binding("QuantityCarcasses"),
            Width = new DataGridLength(110)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total Weight",
            Binding = new Binding("TotalWeighing"),
            Width = new DataGridLength(100)
        });
    }

    /// <summary>
    /// Creates the form for add/edit
    /// </summary>
    protected override Window CreateForm(int? id = null)
    {
        return id.HasValue
            ? new ProductionOrderFormWindow(id.Value)
            : new ProductionOrderFormWindow();
    }

    /// <summary>
    /// Defines the property used for search
    /// </summary>
    protected override Func<ProductionOrderModel, string> GetSearchProperty()
    {
        return order => $"{order.OrderNumber} {order.Product?.Name} {order.Status} {order.Type}";
    }
}
