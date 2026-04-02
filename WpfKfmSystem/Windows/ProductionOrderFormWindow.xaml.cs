using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Interfaces;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

public partial class ProductionOrderFormWindow : ProductionOrderFormWindowBase
{
    private readonly ProductService _productService;
    private readonly SprayChamberService _sprayChamberService;
    private readonly ClassificationWeighingService _classificationService;
    private readonly WeighingScaleService _weighingScaleService;
    private ProductionOrderSimulatorService? _simulatorService;
    private DispatcherTimer? _autoRefreshTimer;
    private ObservableCollection<ProductionOrderItemModel> _items;

    public ProductionOrderFormWindow() : base()
    {
        InitializeComponent();
        _productService = new ProductService();
        _sprayChamberService = new SprayChamberService();
        _classificationService = new ClassificationWeighingService();
        _weighingScaleService = new WeighingScaleService();
        _items = new ObservableCollection<ProductionOrderItemModel>();

        base.TxtValidation = TxtValidation;
        InitializeComboBoxes();
        InitializeAutoRefreshTimer();
        DgItems.ItemsSource = _items;
        CmbStatus.Focus();
    }

    public ProductionOrderFormWindow(int id) : base(id)
    {
        InitializeComponent();
        _productService = new ProductService();
        _sprayChamberService = new SprayChamberService();
        _classificationService = new ClassificationWeighingService();
        _weighingScaleService = new WeighingScaleService();
        _items = new ObservableCollection<ProductionOrderItemModel>();

        base.TxtValidation = TxtValidation;
        InitializeComboBoxes();
        InitializeAutoRefreshTimer();
        DgItems.ItemsSource = _items;
        LoadData();
    }

    private void InitializeAutoRefreshTimer()
    {
        _autoRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
    }

    private void AutoRefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (Id.HasValue && ChkAutoRefresh.IsChecked == true)
        {
            RefreshFormData();
        }
    }

    private void RefreshFormData()
    {
        var model = base.Service.GetById(Id.Value);
        if (model != null)
        {
            // Update only dynamic fields (stocks, notes count, etc.)
            TxtQuantityCarcasses.Text = model.QuantityCarcasses.ToString();
            TxtTotalWeighing.Text = model.TotalWeighing.ToString();

            // Refresh items (stocks may have changed)
            _items.Clear();
            if (model.Items != null)
            {
                foreach (var item in model.Items)
                {
                    _items.Add(item);
                }
            }
        }
    }

    private void InitializeComboBoxes()
    {
        // Status ComboBox
        CmbStatus.ItemsSource = Enum.GetValues(typeof(ProductionOrderStatusType));
        CmbStatus.SelectedIndex = 0;

        // Type ComboBox
        CmbType.ItemsSource = Enum.GetValues(typeof(WeighingType));
        CmbType.SelectedIndex = 0;

        // Product ComboBox
        var products = _productService.GetAll();
        CmbProduct.ItemsSource = products;
        if (products.Any())
            CmbProduct.SelectedIndex = 0;

        // Weighing Scale ComboBox
        var weighingScales = _weighingScaleService.GetAll();
        CmbWeighingScale.ItemsSource = weighingScales;
        if (weighingScales.Any())
            CmbWeighingScale.SelectedIndex = 0;

        // Entrance Orders ComboBox
        var orders = Service.GetByType(WeighingType.SprayChamberEntrance).OrderByDescending(x => x.Id);
        CmbEntranceOrder.ItemsSource = orders;
        if (orders.Any())
            CmbEntranceOrder.SelectedIndex = 0;


        // Initialize dates
        DtpExecutionDate.SelectedDate = DateTime.Now;
        DtpExpirationDate.SelectedDate = DateTime.Now.AddDays(7);
        DtpFacturingDate.SelectedDate = DateTime.Now;
    }

    protected override void FillForm(ProductionOrderModel model)
    {
        TxtTitle.Text = "Edit Production Order";
        PnlId.Visibility = Visibility.Visible;
        TxtIdDisplay.Text = model.Id.ToString();

        CmbStatus.SelectedItem = model.Status;
        CmbType.SelectedItem = model.Type;
        CmbProduct.SelectedValue = model.ProductId;
        CmbWeighingScale.SelectedValue = model.WeighingScaleId;

        DtpExecutionDate.SelectedDate = model.ExecutionDate;
        DtpExpirationDate.SelectedDate = model.ExpirationDate;
        DtpFacturingDate.SelectedDate = model.FacturingDate;

        TxtShift.Text = model.Shift;
        TxtBatch.Text = model.Batch;
        TxtHammer.Text = model.Hammer;
        TxtDescription.Text = model.Description;

        TxtQuantityCarcasses.Text = model.QuantityCarcasses.ToString();
        TxtTotalWeighing.Text = model.TotalWeighing.ToString();

        // Simulator fields
        TxtLowerLimitWeight.Text = model.LowerLimitWeight.ToString();
        TxtUpperLimitWeight.Text = model.UpperLimitWeight.ToString();
        TxtDelayGenerator.Text = model.DelayGenerator.ToString();
        TxtGenerateQuantity.Text = model.GenerateQuantity.ToString();

        // Load items
        if (model.Items != null && model.Items.Any())
        {
            _items.Clear();
            foreach (var item in model.Items)
            {
                _items.Add(item);
            }
        }

        // Show/hide entrance fields based on type
        UpdateEntranceFieldsVisibility();
        UpdateFieldsBasedOnStatus();
    }

    protected override ProductionOrderModel FormDataToModel()
    {
        var model = new ProductionOrderModel
        {
            Id = Id ?? 0,
            Status = (ProductionOrderStatusType)(CmbStatus.SelectedItem ?? ProductionOrderStatusType.Pending),
            Type = (WeighingType)(CmbType.SelectedItem ?? WeighingType.SprayChamberEntrance),
            ProductId = CmbProduct.SelectedValue != null ? (int)CmbProduct.SelectedValue : 0,
            Product = CmbProduct.SelectedItem as ProductModel,
            WeighingScaleId = CmbWeighingScale.SelectedValue != null ? (int)CmbWeighingScale.SelectedValue : 0,
            ExecutionDate = DtpExecutionDate.SelectedDate ?? DateTime.Now,
            ExpirationDate = DtpExpirationDate.SelectedDate ?? DateTime.Now.AddDays(7),
            FacturingDate = DtpFacturingDate.SelectedDate ?? DateTime.Now,
            Shift = TxtShift.Text.Trim(),
            Batch = TxtBatch.Text.Trim(),
            Hammer = TxtHammer.Text.Trim(),
            Description = TxtDescription.Text.Trim(),
            QuantityCarcasses = int.TryParse(TxtQuantityCarcasses.Text, out var qtyCarcasses) ? qtyCarcasses : 0,
            TotalWeighing = int.TryParse(TxtTotalWeighing.Text, out var totalWeight) ? totalWeight : 0,
            LowerLimitWeight = int.TryParse(TxtLowerLimitWeight.Text, out var lowerLimit) ? lowerLimit : 0,
            UpperLimitWeight = int.TryParse(TxtUpperLimitWeight.Text, out var upperLimit) ? upperLimit : 0,
            DelayGenerator = int.TryParse(TxtDelayGenerator.Text, out var delay) ? delay : 0,
            GenerateQuantity = int.TryParse(TxtGenerateQuantity.Text, out var genQty) ? genQty : 0,
            Items = _items.ToList(),
            Notes = new System.Collections.Generic.List<ProductionNotesModel>()
        };

        return model;
    }

    protected override bool FieldValidate()
    {
        HideValidationError();

        if (CmbProduct.SelectedItem == null)
        {
            ShowValidationError("Please select a Product.");
            CmbProduct.Focus();
            return false;
        }

        if (CmbWeighingScale.SelectedItem == null)
        {
            ShowValidationError("Please select a Weighing Scale.");
            CmbWeighingScale.Focus();
            return false;
        }
        
        if (DtpExecutionDate.SelectedDate == null)
        {
            ShowValidationError("Please select an Execution Date.");
            DtpExecutionDate.Focus();
            return false;
        }

        if (DtpExpirationDate.SelectedDate == null)
        {
            ShowValidationError("Please select an Expiration Date.");
            DtpExpirationDate.Focus();
            return false;
        }

        if (DtpFacturingDate.SelectedDate == null)
        {
            ShowValidationError("Please select a Facturing Date.");
            DtpFacturingDate.Focus();
            return false;
        }

        // Validate items - Both entrance and exit orders should have items
        if (_items.Count == 0)
        {
            ShowValidationError("At least one chamber item must be added to define the distribution/sequence.");
            return false;
        }

        return true;
    }

    private void CmbType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateEntranceFieldsVisibility();
    }

    private void UpdateEntranceFieldsVisibility()
    {
        if (CmbType.SelectedItem != null)
        {
            var type = (WeighingType)CmbType.SelectedItem;
            var isEntrance = type == WeighingType.SprayChamberEntrance;

            // Only entrance-specific fields are hidden for Exit orders
            // Items section is always visible for both types
            PnlEntranceFields.Visibility = isEntrance ? Visibility.Visible : Visibility.Collapsed;

            PnlEntranceOrderNumber.Visibility = !isEntrance ? Visibility.Visible : Visibility.Collapsed;

            // Update title based on type
            if (TxtItemsTitle != null)
            {
                TxtItemsTitle.Text = isEntrance 
                    ? "Order Items (Chamber Distribution for Entrance)" 
                    : "Order Items (Chamber Sequence for Exit)";
            }
        }
    }

    private void BtnAddItem_Click(object sender, RoutedEventArgs e)
    {
        var itemWindow = new ProductionOrderItemFormWindow(_sprayChamberService, _classificationService);
        if (itemWindow.ShowDialog() == true && itemWindow.Item != null)
        {
            // Set sequential
            itemWindow.Item.Sequential = _items.Count + 1;
            _items.Add(itemWindow.Item);
        }
    }

    private void BtnEditItem_Click(object sender, RoutedEventArgs e)
    {
        if (DgItems.SelectedItem is ProductionOrderItemModel selectedItem)
        {
            var itemWindow = new ProductionOrderItemFormWindow(_sprayChamberService, _classificationService, selectedItem);
            if (itemWindow.ShowDialog() == true && itemWindow.Item != null)
            {
                var index = _items.IndexOf(selectedItem);
                if (index >= 0)
                {
                    _items[index] = itemWindow.Item;
                }
            }
        }
        else
        {
            MessageBox.Show("Please select an item to edit.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (DgItems.SelectedItem is ProductionOrderItemModel selectedItem)
        {
            var result = MessageBox.Show("Are you sure you want to remove this item?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _items.Remove(selectedItem);
                // Reorganize sequential
                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].Sequential = i + 1;
                }
            }
        }
        else
        {
            MessageBox.Show("Please select an item to remove.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
        => SaveClick(sender, e);

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CmbStatus_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbStatus.SelectedItem != null)
        {
            UpdateFieldsBasedOnStatus();
            HandleStatusChange();
        }
    }

    private void UpdateFieldsBasedOnStatus()
    {
        if (CmbStatus.SelectedItem == null) return;

        var status = (ProductionOrderStatusType)CmbStatus.SelectedItem;
        var isExecuting = status == ProductionOrderStatusType.Executing;
        var isFinalized = status == ProductionOrderStatusType.Finalized;

        // Lock header fields when executing or finalized
        var isHeaderEnabled = !isExecuting && !isFinalized;
        CmbType.IsEnabled = isHeaderEnabled;
        CmbProduct.IsEnabled = isHeaderEnabled;
        CmbWeighingScale.IsEnabled = isHeaderEnabled;
        DtpExecutionDate.IsEnabled = isHeaderEnabled;
        DtpExpirationDate.IsEnabled = isHeaderEnabled;
        DtpFacturingDate.IsEnabled = isHeaderEnabled;
        TxtShift.IsEnabled = isHeaderEnabled;
        TxtBatch.IsEnabled = isHeaderEnabled;
        TxtHammer.IsEnabled = isHeaderEnabled;
        TxtDescription.IsEnabled = isHeaderEnabled;
        TxtLowerLimitWeight.IsEnabled = isHeaderEnabled;
        TxtUpperLimitWeight.IsEnabled = isHeaderEnabled;
        TxtDelayGenerator.IsEnabled = isHeaderEnabled;
        TxtGenerateQuantity.IsEnabled = isHeaderEnabled;

        // Item buttons - allow add only when executing, disable all when finalized
        BtnAddItem.IsEnabled = !isFinalized;
        BtnEditItem.IsEnabled = !isExecuting && !isFinalized;
        BtnRemoveItem.IsEnabled = !isExecuting && !isFinalized;

        // Enable/disable auto-refresh checkbox
        ChkAutoRefresh.IsEnabled = Id.HasValue && (isExecuting || status == ProductionOrderStatusType.Paused);

        // Clear simulation button only when finalized
        BtnClearSimulation.IsEnabled = Id.HasValue && isFinalized;

        // Save button disabled when executing or finalized
        BtnSave.IsEnabled = !isExecuting && !isFinalized;
    }

    private void HandleStatusChange()
    {
        if (!Id.HasValue) return; // Only for existing orders

        var status = (ProductionOrderStatusType)CmbStatus.SelectedItem;
        var model = base.Service.GetById(Id.Value);
        if (model == null) return;

        model.Status = status;

        try
        {
            switch (status)
            {
                case ProductionOrderStatusType.Executing:
                    StartSimulator(model);
                    if (ChkAutoRefresh.IsChecked == true)
                    {
                        _autoRefreshTimer?.Start();
                    }
                    break;

                case ProductionOrderStatusType.Paused:
                    PauseSimulator();
                    _autoRefreshTimer?.Stop();
                    break;

                case ProductionOrderStatusType.Finalized:
                    StopSimulator();
                    _autoRefreshTimer?.Stop();
                    ChkAutoRefresh.IsChecked = false;
                    break;

                case ProductionOrderStatusType.Pending:
                    StopSimulator();
                    _autoRefreshTimer?.Stop();
                    ChkAutoRefresh.IsChecked = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
    }

    private void StartSimulator(ProductionOrderModel model)
    {
        if (_simulatorService == null)
        {
            _simulatorService = new ProductionOrderSimulatorService();
            _simulatorService.DataGenerated += OnSimulatorDataGenerated;
            _simulatorService.SimulatorFinalized += OnSimulatorFinalized;
            _simulatorService.ErrorOccurred += OnSimulatorError;
        }

        _simulatorService.Start(model.Id);
    }

    private void PauseSimulator()
    {
        _simulatorService?.Pause();
    }

    private void StopSimulator()
    {
        if (_simulatorService != null)
        {
            _simulatorService.Stop();
            _simulatorService.DataGenerated -= OnSimulatorDataGenerated;
            _simulatorService.SimulatorFinalized -= OnSimulatorFinalized;
            _simulatorService.ErrorOccurred -= OnSimulatorError;
            _simulatorService = null;
        }
    }

    private void OnSimulatorDataGenerated(object? sender, SimulatorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            // Auto-refresh will update the UI if enabled
            if (ChkAutoRefresh.IsChecked == true)
            {
                RefreshFormData();
            }
        });
    }

    private void OnSimulatorFinalized(object? sender, SimulatorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show("Simulation completed! All configured records have been generated.", 
                            "Simulation Finished", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
            RefreshFormData();
            StopSimulator();
        });
    }

    private void OnSimulatorError(object? sender, string errorMessage)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show($"Simulator error: {errorMessage}", 
                            "Error", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
            StopSimulator();
        });
    }

    private void BtnClearSimulation_Click(object sender, RoutedEventArgs e)
    {
        if (!Id.HasValue) return;

        var result = MessageBox.Show(
            "This will clear all simulation data (notes) and revert chamber stocks to their initial state. Continue?",
            "Confirm Clear Simulation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var model = base.Service.GetById(Id.Value);
                if (model != null)
                {
                    var simulatorService = new ProductionOrderSimulatorService();
                    simulatorService.ClearSimulation(model.Id);

                    MessageBox.Show("Simulation data cleared successfully!", 
                                    "Success", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Information);

                    RefreshFormData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing simulation: {ex.Message}", 
                                "Error", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
        }
    }
}
