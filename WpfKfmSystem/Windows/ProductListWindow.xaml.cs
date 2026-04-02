using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

/// <summary>
/// Janela de listagem de produtos - Agora com apenas ~30 linhas! 🎯
/// Herda todo o comportamento CRUD da BaseListagemWindow
/// </summary>
public class ProdutoListagemWindow : BaseListWindow<ProductModel, ProductService>
{
    public ProdutoListagemWindow()
    {
        // All the UI is created automaticaly by the base class constructor
    }

    protected override string GetTitle() => "Products";

    /// <summary>
    /// Configura as colunas do DataGrid
    /// </summary>
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
            Header = "Name Product",
            Binding = new Binding("Name"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
    }

    /// <summary>
    /// Cria o formulário de cadastro/edição
    /// </summary>
    protected override Window CreateForm(int? id = null)
    {
        return id.HasValue
            ? new ProductFormWindow(id.Value)
            : new ProductFormWindow();
    }

    /// <summary>
    /// Define a propriedade usada para pesquisa
    /// </summary>
    protected override Func<ProductModel, string> GetSearchProperty()
    {
        return produto => produto.Name;
    }
}
