using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

/// <summary>
/// Spray Chamber list window
/// </summary>
public class SprayChamberListWindow : BaseListWindow<SprayChamberModel, SprayChamberService>
{
    public SprayChamberListWindow()
    {
        // All the UI is created automaticaly by the base class constructor
    }

    protected override string GetTitle() => WpfPorkProcessSystem.Resources.Strings.Window_SprayChamber;

    protected override void DatagridColumnsConfig(DataGrid dataGrid)
    {
        dataGrid.Columns.Clear();

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "ID",
            Binding = new Binding("Id"),
            Width = new DataGridLength(80)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding("Name"),
            Width = new DataGridLength(200)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Description",
            Binding = new Binding("Description"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Capacity",
            Binding = new Binding("Capacity"),
            Width = new DataGridLength(100),
            ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right)
                }
            },
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Stock",
            Binding = new Binding("Stock"),
            Width = new DataGridLength(100),
            ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right)
                }
            },
        });        
    }

    protected override Window CreateForm(int? id = null)
    {
        return id.HasValue
            ? new SprayChamberFormWindow(id.Value)
            : new SprayChamberFormWindow();
    }

    protected override Func<SprayChamberModel, string> GetSearchProperty()
    {
        return camara => camara.Name;
    }
}
