using System.Collections.ObjectModel;
using System.Windows;
using WpfPorkProcessSystem.Enums;
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
        DgItems.ItemsSource = _items;
        CmbType.Focus();
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
        DgItems.ItemsSource = _items;
        LoadData();
    }

    private void InitializeComboBoxes()
    {
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

        TxtStatus.Text = model.Status.ToString();
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

        // For Exit orders, set the entrance order combo
        if (model.Type == WeighingType.SprayChamberExit && model.EntranceOrderNumber.HasValue)
        {
            CmbEntranceOrder.SelectedValue = model.EntranceOrderNumber.Value;
        }

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
    }

    protected override ProductionOrderModel FormDataToModel()
    {
        var isNewOrder = !Id.HasValue;
        ProductionOrderModel? existingModel = null;

        // Get existing model to preserve simulator configuration and other data
        if (!isNewOrder)
        {
            existingModel = base.Service.GetById(Id.Value);
        }

        var model = new ProductionOrderModel
        {
            Id = Id ?? 0,
            Status = existingModel?.Status ?? ProductionOrderStatusType.Pending,
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
            Items = _items.ToList(),
            Notes = existingModel?.Notes ?? new System.Collections.Generic.List<ProductionNotesModel>()
        };

        // Preserve simulator configuration (from existing model or set to 0 for new orders)
        model.LowerLimitWeight = existingModel?.LowerLimitWeight ?? 0;
        model.UpperLimitWeight = existingModel?.UpperLimitWeight ?? 0;
        model.DelayGenerator = existingModel?.DelayGenerator ?? 0;
        model.GenerateQuantity = existingModel?.GenerateQuantity ?? 0;

        // For Exit orders, set entrance order number from combo box
        if (model.Type == WeighingType.SprayChamberExit)
        {
            if (CmbEntranceOrder.SelectedValue != null)
            {
                model.EntranceOrderNumber = (int)CmbEntranceOrder.SelectedValue;
            }
            else if (existingModel != null)
            {
                // Preserve existing value if combo is not selected (editing)
                model.EntranceOrderNumber = existingModel.EntranceOrderNumber;
            }
        }

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

        // For Exit orders, validate that an entrance order is selected
        if (CmbType.SelectedItem != null && (WeighingType)CmbType.SelectedItem == WeighingType.SprayChamberExit)
        {
            if (CmbEntranceOrder.SelectedItem == null)
            {
                ShowValidationError("Please select an Entrance Order for this Exit order.");
                CmbEntranceOrder.Focus();
                return false;
            }
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
        var orderType = (WeighingType)(CmbType.SelectedItem ?? WeighingType.SprayChamberEntrance);
        var itemWindow = new ProductionOrderItemFormWindow(_sprayChamberService, _classificationService, orderType);
        if (itemWindow.ShowDialog() == true && itemWindow.Item != null)
        {
            // Validate: Check if chamber already exists in the list
            if (_items.Any(i => i.SprayChamberId == itemWindow.Item.SprayChamberId))
            {
                MessageBox.Show($"A chamber with ID {itemWindow.Item.SprayChamberId} is already added to this order. Each chamber can only be added once.",
                                "Duplicate Chamber",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // Set sequential
            itemWindow.Item.Sequential = _items.Count + 1;
            _items.Add(itemWindow.Item);
        }
    }

    private void BtnEditItem_Click(object sender, RoutedEventArgs e)
    {
        if (DgItems.SelectedItem is ProductionOrderItemModel selectedItem)
        {
            var orderType = (WeighingType)(CmbType.SelectedItem ?? WeighingType.SprayChamberEntrance);
            var itemWindow = new ProductionOrderItemFormWindow(_sprayChamberService, _classificationService, selectedItem, orderType);
            if (itemWindow.ShowDialog() == true && itemWindow.Item != null)
            {
                // Validate: Check if the new chamber already exists (excluding the current item)
                if (_items.Any(i => i.SprayChamberId == itemWindow.Item.SprayChamberId && i.Sequential != selectedItem.Sequential))
                {
                    MessageBox.Show($"A chamber with ID {itemWindow.Item.SprayChamberId} is already added to this order. Each chamber can only be added once.",
                                    "Duplicate Chamber",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

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
}
