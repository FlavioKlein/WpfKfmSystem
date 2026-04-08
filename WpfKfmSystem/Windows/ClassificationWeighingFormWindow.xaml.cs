using System.Globalization;
using System.Windows;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

public partial class ClassificationWeighingFormWindow : ClassificationWeighingFormWindowBase
{
    private readonly ProductService _productService;

    public ClassificationWeighingFormWindow()
    {
        InitializeComponent();
        _productService = new ProductService();
        base.TxtValidation = TxtValidation;
        Title = WpfPorkProcessSystem.Resources.Strings.Window_NewClassification;
        LoadProducts();
        TxtName.Focus();
    }

    public ClassificationWeighingFormWindow(int id) : base(id)
    {
        InitializeComponent();
        _productService = new ProductService();
        base.TxtValidation = TxtValidation;
        Title = WpfPorkProcessSystem.Resources.Strings.Window_EditClassification;
        LoadProducts();
        LoadData(); 
    }

    protected override void FillForm(ClassificationWeighingModel model)
    {
        TxtTitle.Text = WpfPorkProcessSystem.Resources.Strings.Window_EditClassification;
        PnlId.Visibility = Visibility.Visible;
        TxtIdDisplay.Text = model.Id.ToString();
        TxtName.Text = model.Name;
        TxtLowerLimit.Text = model.LowerLimit.ToString();
        TxtUpperLimit.Text = model.UpperLimit.ToString();

        // Seleciona o produto na ComboBox
        if (model.ProductId > 0)
        {
            CmbProduct.SelectedValue = model.ProductId;
        }
    }

    protected override ClassificationWeighingModel FormDataToModel()
    {
        return new ClassificationWeighingModel
        {
            Id = Id ?? 0,
            Name = TxtName.Text.Trim(),
            ProductId = CmbProduct.SelectedValue != null ? (int)CmbProduct.SelectedValue : 0,
            Product = CmbProduct.SelectedItem as ProductModel,
            LowerLimit = decimal.Parse(TxtLowerLimit.Text, NumberStyles.Any, CultureInfo.CurrentCulture),
            UpperLimit = decimal.Parse(TxtUpperLimit.Text, NumberStyles.Any, CultureInfo.CurrentCulture)
        };
    }

    protected override bool FieldValidate()
    {
        HideValidationError(); 

        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            ShowValidationError("The field Name is mandatory."); 
            TxtName.Focus();
            return false;
        }

        if (CmbProduct.SelectedValue == null)
        {
            ShowValidationError("Please select a product.");
            CmbProduct.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(TxtLowerLimit.Text))
        {
            ShowValidationError("The lower limit is mandatory.");
            TxtLowerLimit.Focus();
            return false;
        }

        if (!decimal.TryParse(TxtLowerLimit.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var lowerLimit))
        {
            ShowValidationError("The lower limit must be valid.");
            TxtLowerLimit.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(TxtUpperLimit.Text))
        {
            ShowValidationError("The upper limit is mandatory.");
            TxtUpperLimit.Focus();
            return false;
        }

        if (!decimal.TryParse(TxtUpperLimit.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var upperLimit))
        {
            ShowValidationError("The uppper limit must be valid.");
            TxtUpperLimit.Focus();
            return false;
        }

        if (upperLimit <= lowerLimit)
        {
            ShowValidationError("The upper limit must be above lower limit.");
            TxtUpperLimit.Focus();
            return false;
        }

        return true;
    }

    private void LoadProducts()
    {
        try
        {
            var products = _productService.GetAll();
            CmbProduct.ItemsSource = products;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading products: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbProduct_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // You could add additional logic here when a product is selected
        // For example, fetch additional product information
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
        => SaveClick(sender, e);

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
