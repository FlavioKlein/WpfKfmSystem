using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

public class ClassificationWeighingListWindow : BaseListWindow<ClassificationWeighingModel, ClassificationWeighingService>
{
    public ClassificationWeighingListWindow() : base()
    {
        Width = 950;
    }

    protected override string GetTitle() => "Weighing Classification";

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
            Header = "ProductId",
            Binding = new Binding("ProductId"),
            Width = new DataGridLength(80)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Product Name",
            Binding = new Binding("Product.Name"),
            Width = new DataGridLength(200)
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Lower Limit",
            Binding = new Binding("LowerLimit") { StringFormat = "N2" },
            Width = new DataGridLength(150),
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
            Header = "Upper Limit",
            Binding = new Binding("UpperLimit") { StringFormat = "N2" },            
            Width = new DataGridLength(150),
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
            ? new ClassificationWeighingFormWindow(id.Value)
            : new ClassificationWeighingFormWindow();
    }

    protected override Func<ClassificationWeighingModel, string> GetSearchProperty()
    {
        return classificacao => classificacao.Name;
    }
}
