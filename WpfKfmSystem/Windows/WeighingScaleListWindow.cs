using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

/// <summary>
/// Weighing Scale list window
/// </summary>
public class WeighingScaleListWindow : BaseListWindow<WeighingScaleModel, WeighingScaleService>
{
    public WeighingScaleListWindow()
    {
        // All the UI is created automatically by the base class constructor
    }

    protected override string GetTitle() => WpfPorkProcessSystem.Resources.Strings.Window_WeighingScale;

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
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Type",
            Binding = new Binding("Type"),
            Width = new DataGridLength(150)
        });
    }

    protected override Window CreateForm(int? id = null)
    {
        return id.HasValue
            ? new WeighingScaleFormWindow(id.Value)
            : new WeighingScaleFormWindow();
    }

    protected override Func<WeighingScaleModel, string> GetSearchProperty()
    {
        return scale => scale.Name;
    }
}
