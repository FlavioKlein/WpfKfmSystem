using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows.Base;

/// <summary>
/// Janela base genérica para listagem de dados com CRUD completo
/// NÃO tem XAML separado - Toda a UI é criada programaticamente
/// </summary>
/// <typeparam name="TModel">Tipo do modelo que herda de BaseModel</typeparam>
/// <typeparam name="TService">Tipo do serviço que herda de BaseService</typeparam>
public abstract class BaseListWindow<TModel, TService> : Window
    where TModel : BaseModel
    where TService : BaseService<TModel>, new()
{
    protected readonly TService Service;
    protected ObservableCollection<TModel> Items;
    protected List<TModel> AllItems;

    // Controles UI
    protected TextBlock TxtTitle;
    protected TextBox TxtSearch;
    protected DataGrid DgData;
    protected TextBlock StatusText;
    protected TextBlock StatusCount;
    protected Button BtnNovo, BtnEditar, BtnExcluir, BtnAtualizar, BtnLimparPesquisa;

    protected BaseListWindow()
    {
        Service = new TService();
        Items = new ObservableCollection<TModel>();
        AllItems = new List<TModel>();

        WindowConfigs();

        // Cria a interface
        CreateUiInterface();

        // Carrega os dados
        LoadData();
    }

    protected virtual void WindowConfigs()
    {
        Title = GetTitle();
        Width = 900;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
    }

    /// <summary>
    /// Creates all of the visual interface, programatily
    /// </summary>
    private void CreateUiInterface()
    {
        var mainGrid = new Grid { Margin = new Thickness(20) };
        
        // Window grid lines
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Linha 0: Título
        TxtTitle = new TextBlock
        {
            Text = GetTitle(),
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(TxtTitle, 0);
        mainGrid.Children.Add(TxtTitle);

        // Linha 1: Search bar and Buttons
        var barraFerramentas = CreateToolsBar();
        Grid.SetRow(barraFerramentas, 1);
        mainGrid.Children.Add(barraFerramentas);

        // Linha 2: DataGrid
        var bordaGrid = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3)
        };

        DgData = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            ItemsSource = Items
        };

        DgData.MouseDoubleClick += DgData_MouseDoubleClick;

        // Configura as colunas
        DatagridColumnsConfig(DgData);

        bordaGrid.Child = DgData;
        Grid.SetRow(bordaGrid, 2);
        mainGrid.Children.Add(bordaGrid);

        // Linha 3: Status Bar
        var statusBar = CriarBarraStatus();
        Grid.SetRow(statusBar, 3);
        mainGrid.Children.Add(statusBar);

        Content = mainGrid;
    }

    /// <summary>
    /// Cria a barra de ferramentas com pesquisa e botões
    /// </summary>
    private Grid CreateToolsBar()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Campo de Pesquisa
        var gridPesquisa = new Grid { Margin = new Thickness(0, 0, 10, 0) };
        gridPesquisa.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridPesquisa.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TxtSearch = new TextBox
        {
            Padding = new Thickness(8),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        TxtSearch.TextChanged += TxtSearch_TextChanged;
        Grid.SetColumn(TxtSearch, 0);
        gridPesquisa.Children.Add(TxtSearch);

        BtnLimparPesquisa = CriarBotao("✖", 35, 35, "#f44336", "#da190b");
        BtnLimparPesquisa.Click += BtnSearchClear_Click;
        BtnLimparPesquisa.Margin = new Thickness(5, 0, 0, 0);
        Grid.SetColumn(BtnLimparPesquisa, 1);
        gridPesquisa.Children.Add(BtnLimparPesquisa);

        Grid.SetColumn(gridPesquisa, 0);
        grid.Children.Add(gridPesquisa);

        // Botões de Ação
        var stackBotoes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        BtnNovo = CriarBotao("➕ Novo", 100, 35, "#4CAF50", "#45a049");
        BtnNovo.Click += BtnNew_Click;
        BtnNovo.Margin = new Thickness(0, 0, 10, 0);
        stackBotoes.Children.Add(BtnNovo);

        BtnEditar = CriarBotao("✏️ Editar", 100, 35, "#2196F3", "#0b7dda");
        BtnEditar.Click += BtnEdit_Click;
        BtnEditar.Margin = new Thickness(0, 0, 10, 0);
        stackBotoes.Children.Add(BtnEditar);

        BtnExcluir = CriarBotao("🗑️ Excluir", 100, 35, "#f44336", "#da190b");
        BtnExcluir.Click += BtnDelete_Click;
        BtnExcluir.Margin = new Thickness(0, 0, 10, 0);
        stackBotoes.Children.Add(BtnExcluir);

        BtnAtualizar = CriarBotao("🔄 Atualizar", 110, 35, "#9E9E9E", "#757575");
        BtnAtualizar.Click += BtnRefresh_Click;
        stackBotoes.Children.Add(BtnAtualizar);

        Grid.SetColumn(stackBotoes, 1);
        grid.Children.Add(stackBotoes);

        return grid;
    }

    /// <summary>
    /// Cria a barra de status
    /// </summary>
    private StatusBar CriarBarraStatus()
    {
        var statusBar = new StatusBar
        {
            Margin = new Thickness(0, 10, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
        };

        StatusText = new TextBlock { Text = "Pronto", FontWeight = FontWeights.Bold };
        statusBar.Items.Add(new StatusBarItem { Content = StatusText });

        statusBar.Items.Add(new Separator());

        StatusCount = new TextBlock { Text = "Total: 0" };
        statusBar.Items.Add(new StatusBarItem { Content = StatusCount });

        return statusBar;
    }

    /// <summary>
    /// Helper para criar botões estilizados
    /// </summary>
    private Button CriarBotao(string texto, double largura, double altura, string corNormal, string corHover)
    {
        var botao = new Button
        {
            Content = texto,
            Width = largura,
            Height = altura,
            Background = (SolidColorBrush)new BrushConverter().ConvertFrom(corNormal)!,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(0)
        };

        return botao;
    }

    #region Métodos Abstratos

    /// <summary>
    /// Retrieve the window title.
    /// </summary>
    protected abstract string GetTitle();

    /// <summary>
    /// DataGrid columns config.
    /// </summary>
    protected abstract void DatagridColumnsConfig(DataGrid dataGrid);

    /// <summary>
    /// Create a new Form instance, to create and edit data.
    /// </summary>
    protected abstract Window CreateForm(int? id = null);

    /// <summary>
    /// Patern search property from model. Overrides to custom.
    /// </summary>
    protected virtual Func<TModel, string> GetSearchProperty() => model => model.ToString() ?? string.Empty;

    #endregion

    #region CRUD Methods

    /// <summary>
    /// Load all data
    /// </summary>
    protected virtual void LoadData()
    {
        try
        {
            AllItems = Service.GetAll();
            UpdateGrid(AllItems);
            UpdateStatus($"Success data load. Total: {AllItems.Count}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Load data error: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Atualiza o DataGrid com os dados fornecidos
    /// </summary>
    protected virtual void UpdateGrid(List<TModel> dados)
    {
        Items.Clear();
        foreach (var item in dados.OrderBy(x => x.Id))
        {
            Items.Add(item);
        }
        StatusCount.Text = $"Total: {Items.Count}";
    }

    /// <summary>
    /// Filtra os dados com base no texto de pesquisa
    /// </summary>
    protected virtual List<TModel> DataFilter(string textoPesquisa)
    {
        if (string.IsNullOrEmpty(textoPesquisa))
            return AllItems;

        var pesquisaLower = textoPesquisa.ToLower().Trim();
        var propriedadePesquisa = GetSearchProperty();

        return AllItems
            .Where(item =>
                propriedadePesquisa(item).ToLower().Contains(pesquisaLower) ||
                item.Id.ToString().Contains(pesquisaLower))
            .ToList();
    }

    #endregion

    #region Event Handlers

    protected virtual void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        var formWindow = CreateForm();
        formWindow.Owner = this;

        if (formWindow.ShowDialog() == true)
        {
            LoadData();
            UpdateStatus("Success: Registry added");
        }
    }

    protected virtual void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        EditSelected();
    }

    protected virtual void EditSelected()
    {
        if (DgData.SelectedItem is TModel itemSelecionado)
        {
            var formWindow = CreateForm(itemSelecionado.Id);
            formWindow.Owner = this;

            if (formWindow.ShowDialog() == true)
            {
                LoadData();
                UpdateStatus("Success: Registry updated");
            }
        }
        else
        {
            MessageBox.Show("Select a registry for edit.",
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected virtual void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgData.SelectedItem is TModel itemSelecionado)
        {
            var nomeItem = GetSearchProperty()(itemSelecionado);
            var resultado = MessageBox.Show(
                $"You really want delete'{nomeItem}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    Service.Remove(itemSelecionado.Id);
                    LoadData();
                    UpdateStatus("Success: Registry deleted");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete registry error: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Select a registry for delete.",
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected virtual void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadData();
    }

    protected virtual void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textoPesquisa = TxtSearch.Text;
        var dadosFiltrados = DataFilter(textoPesquisa);
        UpdateGrid(dadosFiltrados);
    }

    protected virtual void BtnSearchClear_Click(object sender, RoutedEventArgs e)
    {
        TxtSearch.Clear();
        UpdateGrid(AllItems);
    }

    protected virtual void DgData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgData.SelectedItem != null)
        {
            EditSelected();
        }
    }

    protected virtual void UpdateStatus(string mensagem)
    {
        StatusText.Text = mensagem;
    }

    #endregion
}
