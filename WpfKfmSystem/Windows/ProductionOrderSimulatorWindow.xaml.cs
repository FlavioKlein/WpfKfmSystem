using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows;

public partial class ProductionOrderSimulatorWindow : Window
{
    private readonly ProductionOrderService _orderService;
    private ProductionOrderSimulatorService? _simulatorService;
    private DispatcherTimer? _autoRefreshTimer;
    private ObservableCollection<ProductionOrderItemModel> _items;
    private int? _currentOrderId;

    public ProductionOrderSimulatorWindow()
    {
        InitializeComponent();
        _orderService = new ProductionOrderService();
        _items = new ObservableCollection<ProductionOrderItemModel>();
        DgItems.ItemsSource = _items;

        // Set window title from resources
        Title = WpfPorkProcessSystem.Resources.Strings.Window_Simulator;

        InitializeComboBox();
        InitializeDelayComboBox();
        InitializeAutoRefreshTimer();
        UpdateControlsState();
    }

    private void InitializeComboBox()
    {
        var orders = _orderService.GetAll()
            .Where(o => o.Type == WeighingType.SprayChamberEntrance || o.Type == WeighingType.SprayChamberExit)
            .OrderByDescending(o => o.Id)
            .ToList();

        CmbProductionOrder.ItemsSource = orders;
        if (orders.Any())
        {
            CmbProductionOrder.SelectedIndex = 0;
        }
    }

    private void InitializeDelayComboBox()
    {
        var delayOptions = new[] { "0", "0.1", "0.2", "0.5", "0.7", "1", "2", "3" };
        CmbDelayGenerator.ItemsSource = delayOptions;
        CmbDelayGenerator.Text = "0.1"; // Default value
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
        if (_currentOrderId.HasValue && ChkAutoRefresh.IsChecked == true)
        {
            RefreshOrderData();
        }
    }

    private void CmbProductionOrder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Just update the combo, don't load until user clicks Load Order button
    }

    private void BtnLoadOrder_Click(object sender, RoutedEventArgs e)
    {
        if (CmbProductionOrder.SelectedItem is not ProductionOrderModel selectedOrder)
        {
            ShowValidationError("Please select a production order.");
            return;
        }

        LoadOrder(selectedOrder.Id);
    }

    private void LoadOrder(int orderId)
    {
        var order = _orderService.GetById(orderId);
        if (order == null)
        {
            ShowValidationError("Production order not found.");
            return;
        }

        _currentOrderId = orderId;

        // Fill configuration fields from saved order data
        TxtLowerLimitWeight.Text = order.LowerLimitWeight > 0 ? order.LowerLimitWeight.ToString() : string.Empty;
        TxtUpperLimitWeight.Text = order.UpperLimitWeight > 0 ? order.UpperLimitWeight.ToString() : string.Empty;

        // Convert milliseconds back to seconds for display (supports fractional values)
        if (order.DelayGenerator > 0)
        {
            var delayInSeconds = order.DelayGenerator / 1000.0;
            CmbDelayGenerator.Text = delayInSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            CmbDelayGenerator.Text = "0.1"; // Default value
        }

        TxtGenerateQuantity.Text = order.GenerateQuantity > 0 ? order.GenerateQuantity.ToString() : string.Empty;

        // Update UI based on order type
        UpdateUIForOrderType(order.Type);

        // Fill order data
        RefreshOrderData();

        UpdateControlsState();
        HideValidationError();
    }

    private void UpdateUIForOrderType(WeighingType orderType)
    {
        if (orderType == WeighingType.SprayChamberExit)
        {
            // For Exit orders, disable weight limits and generate quantity
            // Only Delay is relevant (for visual simulation timing)
            TxtLowerLimitWeight.IsEnabled = false;
            TxtUpperLimitWeight.IsEnabled = false;
            TxtGenerateQuantity.IsEnabled = false;

            TxtLowerLimitWeight.Background = System.Windows.Media.Brushes.LightGray;
            TxtUpperLimitWeight.Background = System.Windows.Media.Brushes.LightGray;
            TxtGenerateQuantity.Background = System.Windows.Media.Brushes.LightGray;

            // Clear values to avoid confusion
            TxtLowerLimitWeight.Text = "N/A (Exit order)";
            TxtUpperLimitWeight.Text = "N/A (Exit order)";
            TxtGenerateQuantity.Text = "Auto (from entrance)";
        }
        else
        {
            // For Entrance orders, enable all fields
            TxtLowerLimitWeight.IsEnabled = true;
            TxtUpperLimitWeight.IsEnabled = true;
            TxtGenerateQuantity.IsEnabled = true;

            TxtLowerLimitWeight.Background = System.Windows.Media.Brushes.White;
            TxtUpperLimitWeight.Background = System.Windows.Media.Brushes.White;
            TxtGenerateQuantity.Background = System.Windows.Media.Brushes.White;

            // Values will be loaded from order data
        }
    }

    private void RefreshOrderData()
    {
        if (!_currentOrderId.HasValue) return;

        var order = _orderService.GetById(_currentOrderId.Value);
        if (order == null) return;

        TxtOrderId.Text = order.Id.ToString();
        TxtStatus.Text = order.Status.ToString();
        TxtQuantityCarcasses.Text = order.QuantityCarcasses.ToString();
        TxtTotalWeighing.Text = order.TotalWeighing.ToString();

        // Refresh items
        _items.Clear();
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                _items.Add(item);
            }
        }

        UpdateControlsState();
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (!_currentOrderId.HasValue)
        {
            ShowValidationError("Please load a production order first.");
            return;
        }

        if (!ValidateSimulatorConfig())
        {
            return;
        }

        try
        {
            // Save configuration first
            SaveConfiguration();

            var order = _orderService.GetById(_currentOrderId.Value);
            if (order == null)
            {
                ShowValidationError("Order not found.");
                return;
            }

            if (order.Type == WeighingType.SprayChamberExit && !order.EntranceOrderNumber.HasValue)
            {
                ShowValidationError("Exit Order needs a corresponding Entrance Order.");
                return;
            }

            // Update status to Executing
            order.Status = ProductionOrderStatusType.Executing;
            _orderService.Update(order);

            // Start simulator
            if (_simulatorService == null)
            {
                _simulatorService = new ProductionOrderSimulatorService();
                _simulatorService.DataGenerated += OnSimulatorDataGenerated;
                _simulatorService.SimulatorFinalized += OnSimulatorFinalized;
                _simulatorService.ErrorOccurred += OnSimulatorError;
            }

            await _simulatorService.StartAsync(_currentOrderId.Value);

            if (ChkAutoRefresh.IsChecked == true)
            {
                _autoRefreshTimer?.Start();
            }

            UpdateControlsState();
            HideValidationError();
        }
        catch (Exception ex)
        {
            ShowValidationError($"Error starting simulator: {ex.Message}");
        }
    }

    private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
    {
        if (!_currentOrderId.HasValue)
        {
            ShowValidationError("Please load a production order first.");
            return;
        }

        if (!ValidateSimulatorConfig())
        {
            return;
        }

        try
        {
            SaveConfiguration();

            MessageBox.Show("Simulator configuration saved successfully!",
                            "Configuration Saved",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

            HideValidationError();
        }
        catch (Exception ex)
        {
            ShowValidationError($"Error saving configuration: {ex.Message}");
        }
    }

    private void SaveConfiguration()
    {
        if (!_currentOrderId.HasValue) return;

        var order = _orderService.GetById(_currentOrderId.Value);
        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        // For Entrance orders, save all configuration
        if (order.Type == WeighingType.SprayChamberEntrance)
        {
            order.LowerLimitWeight = int.Parse(TxtLowerLimitWeight.Text);
            order.UpperLimitWeight = int.Parse(TxtUpperLimitWeight.Text);
            order.GenerateQuantity = int.Parse(TxtGenerateQuantity.Text);
        }
        // For Exit orders, these values are not used (taken from entrance order)
        else
        {
            order.LowerLimitWeight = 0;
            order.UpperLimitWeight = 0;
            order.GenerateQuantity = 0; // Will be determined by available items in entrance order
        }

        // Parse delay as double to support fractional seconds (e.g., 0.5, 0.1)
        // Store as int (will be converted to milliseconds when starting simulator)
        var delayInSeconds = double.Parse(CmbDelayGenerator.Text, System.Globalization.CultureInfo.InvariantCulture);
        order.DelayGenerator = (int)(delayInSeconds * 1000); // Convert to milliseconds for storage

        // Persist configuration to database
        _orderService.Update(order);
    }

    private async void BtnPauseResume_Click(object sender, RoutedEventArgs e)
    {
        if (_simulatorService == null) return;

        if (_simulatorService.IsRunning)
        {
            _simulatorService.Pause();
            var order = _orderService.GetById(_currentOrderId!.Value);
            if (order != null)
            {
                order.Status = ProductionOrderStatusType.Paused;
                _orderService.Update(order);
            }
            _autoRefreshTimer?.Stop();
            BtnPauseResume.Content = "Resume";
        }
        else
        {
            await _simulatorService.ResumeAsync();
            var order = _orderService.GetById(_currentOrderId!.Value);
            if (order != null)
            {
                order.Status = ProductionOrderStatusType.Executing;
                _orderService.Update(order);
            }
            if (ChkAutoRefresh.IsChecked == true)
            {
                _autoRefreshTimer?.Start();
            }
            BtnPauseResume.Content = "Pause";
        }

        UpdateControlsState();
    }

    private void BtnClearSimulation_Click(object sender, RoutedEventArgs e)
    {
        if (!_currentOrderId.HasValue) return;

        var result = MessageBox.Show(
            "This will clear all simulation data (notes) and revert chamber stocks to their initial state. Continue?",
            "Confirm Clear Simulation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                StopSimulator();

                var clearService = new ProductionOrderSimulatorService();
                clearService.ClearSimulation(_currentOrderId.Value);

                MessageBox.Show("Simulation data cleared successfully!",
                                "Success",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                RefreshOrderData();
                UpdateControlsState();
            }
            catch (Exception ex)
            {
                ShowValidationError($"Error clearing simulation: {ex.Message}");
            }
        }
    }

    private void OnSimulatorDataGenerated(object? sender, SimulatorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (ChkAutoRefresh.IsChecked == true)
            {
                RefreshOrderData();
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
            RefreshOrderData();
            StopSimulator();
            UpdateControlsState();
        });
    }

    private void OnSimulatorError(object? sender, string errorMessage)
    {
        Dispatcher.Invoke(() =>
        {
            ShowValidationError($"Simulator error: {errorMessage}");
            StopSimulator();
            UpdateControlsState();
        });
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
        _autoRefreshTimer?.Stop();
        BtnPauseResume.Content = "Pause";
    }

    private void UpdateControlsState()
    {
        var hasOrder = _currentOrderId.HasValue;
        var order = hasOrder ? _orderService.GetById(_currentOrderId.Value) : null;
        var isRunning = _simulatorService?.IsRunning ?? false;
        var isPaused = order?.Status == ProductionOrderStatusType.Paused;
        var isFinalized = order?.Status == ProductionOrderStatusType.Finalized;

        // Configuration fields - only editable when not running and not finalized
        TxtLowerLimitWeight.IsEnabled = hasOrder && !isRunning && !isFinalized;
        TxtUpperLimitWeight.IsEnabled = hasOrder && !isRunning && !isFinalized;
        CmbDelayGenerator.IsEnabled = hasOrder && !isRunning && !isFinalized;
        TxtGenerateQuantity.IsEnabled = hasOrder && !isRunning && !isFinalized;

        // Buttons
        BtnSaveConfig.IsEnabled = hasOrder && !isRunning && !isFinalized;
        BtnStart.IsEnabled = hasOrder && !isRunning && !isFinalized;
        BtnPauseResume.IsEnabled = hasOrder && (isRunning || isPaused) && !isFinalized;
        BtnClearSimulation.IsEnabled = hasOrder && !isRunning && isFinalized;

        // Close button - only enabled when not running
        BtnClose.IsEnabled = !isRunning;
    }

    private bool ValidateSimulatorConfig()
    {
        if (!_currentOrderId.HasValue) return false;

        var order = _orderService.GetById(_currentOrderId.Value);
        if (order == null) return false;

        // For Entrance orders, validate all fields
        if (order.Type == WeighingType.SprayChamberEntrance)
        {
            if (!int.TryParse(TxtLowerLimitWeight.Text, out var lowerLimit) || lowerLimit <= 0)
            {
                ShowValidationError("Lower Limit Weight must be a positive number.");
                TxtLowerLimitWeight.Focus();
                return false;
            }

            if (!int.TryParse(TxtUpperLimitWeight.Text, out var upperLimit) || upperLimit <= 0)
            {
                ShowValidationError("Upper Limit Weight must be a positive number.");
                TxtUpperLimitWeight.Focus();
                return false;
            }

            if (lowerLimit >= upperLimit)
            {
                ShowValidationError("Lower Limit Weight must be less than Upper Limit Weight.");
                TxtLowerLimitWeight.Focus();
                return false;
            }

            if (!int.TryParse(TxtGenerateQuantity.Text, out var quantity) || quantity <= 0)
            {
                ShowValidationError("Generate Quantity must be a positive number.");
                TxtGenerateQuantity.Focus();
                return false;
            }
        }
        // For Exit orders, only validate delay (weight and quantity come from entrance order)

        // Allow delay >= 0 (including 0 for instant simulation) and support fractional values
        if (!double.TryParse(CmbDelayGenerator.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var delay) || delay < 0)
        {
            ShowValidationError("Delay must be a non-negative number (0 or greater). Use decimal point for fractional seconds (e.g., 0.5 for half second).");
            CmbDelayGenerator.Focus();
            return false;
        }

        // Warn if delay is very low (might overload UI)
        if (delay > 0 && delay < 0.05)
        {
            var result = MessageBox.Show(
                $"Delay is very low ({delay}s). This may cause UI performance issues. Continue?",
                "Low Delay Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                CmbDelayGenerator.Focus();
                return false;
            }
        }

        return true;
    }

    private void ShowValidationError(string message)
    {
        TxtValidation.Text = message;
        TxtValidation.Visibility = Visibility.Visible;
    }

    private void HideValidationError()
    {
        TxtValidation.Text = string.Empty;
        TxtValidation.Visibility = Visibility.Collapsed;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        var isRunning = _simulatorService?.IsRunning ?? false;
        if (isRunning)
        {
            MessageBox.Show("Cannot close window while simulation is running. Please pause the simulation first.",
                            "Cannot Close",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            e.Cancel = true;
            return;
        }

        StopSimulator();
    }
}
