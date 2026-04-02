using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    private ObservableCollection<ProductionOrderItemModel> _items;

    public ProductionOrderFormWindow() : base()
    {
        InitializeComponent();
        _productService = new ProductService();
        _sprayChamberService = new SprayChamberService();
        _classificationService = new ClassificationWeighingService();
        _items = new ObservableCollection<ProductionOrderItemModel>();
        
        base.TxtValidation = TxtValidation;
        InitializeComboBoxes();
        DgItems.ItemsSource = _items;
        TxtOrderNumber.Focus();
    }

    public ProductionOrderFormWindow(int id) : base(id)
    {
        InitializeComponent();
        _productService = new ProductService();
        _sprayChamberService = new SprayChamberService();
        _classificationService = new ClassificationWeighingService();
        _items = new ObservableCollection<ProductionOrderItemModel>();
        
        base.TxtValidation = TxtValidation;
        InitializeComboBoxes();
        DgItems.ItemsSource = _items;
        LoadData();
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
        
        TxtOrderNumber.Text = model.OrderNumber.ToString();
        CmbStatus.SelectedItem = model.Status;
        CmbType.SelectedItem = model.Type;
        CmbProduct.SelectedValue = model.ProductId;
        TxtWeighingScaleId.Text = model.WeighingScaleId.ToString();
        
        DtpExecutionDate.SelectedDate = model.ExecutionDate;
        DtpExpirationDate.SelectedDate = model.ExpirationDate;
        DtpFacturingDate.SelectedDate = model.FacturingDate;
        
        TxtShift.Text = model.Shift;
        TxtBatch.Text = model.Batch;
        TxtHammer.Text = model.Hammer;
        TxtDescription.Text = model.Description;
        
        TxtQuantityCarcasses.Text = model.QuantityCarcasses.ToString();
        TxtTotalWeighing.Text = model.TotalWeighing.ToString();

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
        var model = new ProductionOrderModel
        {
            Id = Id ?? 0,
            OrderNumber = int.TryParse(TxtOrderNumber.Text, out var orderNum) ? orderNum : 0,
            Status = (ProductionOrderStatusType)(CmbStatus.SelectedItem ?? ProductionOrderStatusType.Active),
            Type = (WeighingType)(CmbType.SelectedItem ?? WeighingType.SprayChamberEntrance),
            ProductId = CmbProduct.SelectedValue != null ? (int)CmbProduct.SelectedValue : 0,
            Product = CmbProduct.SelectedItem as ProductModel,
            WeighingScaleId = int.TryParse(TxtWeighingScaleId.Text, out var scaleId) ? scaleId : 0,
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
            Notes = new System.Collections.Generic.List<ProductionNotesModel>()
        };

        return model;
    }

    protected override bool FieldValidate()
    {
        HideValidationError();

        if (string.IsNullOrWhiteSpace(TxtOrderNumber.Text) || !int.TryParse(TxtOrderNumber.Text, out _))
        {
            ShowValidationError("The field Order Number is mandatory and must be a valid number.");
            TxtOrderNumber.Focus();
            return false;
        }

        if (CmbProduct.SelectedItem == null)
        {
            ShowValidationError("Please select a Product.");
            CmbProduct.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(TxtWeighingScaleId.Text) || !int.TryParse(TxtWeighingScaleId.Text, out _))
        {
            ShowValidationError("The field Weighing Scale ID is mandatory and must be a valid number.");
            TxtWeighingScaleId.Focus();
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
}
