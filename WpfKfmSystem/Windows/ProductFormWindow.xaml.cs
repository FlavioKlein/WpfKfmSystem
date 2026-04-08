using System.Windows;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

public partial class ProductFormWindow : ProductFormWindowBase
{
    public ProductFormWindow() : base()
    {
        InitializeComponent();
        // Conecta o TextBlock de validação da classe base
        base.TxtValidation = TxtValidation;
        Title = WpfPorkProcessSystem.Resources.Strings.Window_NewProduct;
        TxtName.Focus();
    }

    public ProductFormWindow(int id) : base(id)
    {
        InitializeComponent();
        // Conecta o TextBlock de validação da classe base
        base.TxtValidation = TxtValidation;
        Title = WpfPorkProcessSystem.Resources.Strings.Window_EditProduct;
        LoadData(); // Chama o método da classe base
    }

    // Implementa o método abstrato para preencher o formulário
    protected override void FillForm(ProductModel model)
    {
        TxtTitle.Text = WpfPorkProcessSystem.Resources.Strings.Window_EditProduct;
        PnlId.Visibility = Visibility.Visible;
        TxtIdDisplay.Text = model.Id.ToString();
        TxtName.Text = model.Name;
    }

    // Implementa o método abstrato para obter o modelo do formulário
    protected override ProductModel FormDataToModel()
    {
        return new ProductModel
        {
            Id = Id ?? 0, // Usa o Id da classe base
            Name = TxtName.Text.Trim()
        };
    }

    // Implementa o método abstrato de validação
    protected override bool FieldValidate()
    {
        HideValidationError(); // Método da classe base

        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            ShowValidationError("The field Name is mandatory."); // Método da classe base
            TxtName.Focus();
            return false;
        }

        if (TxtName.Text.Trim().Length < 3)
        {
            ShowValidationError("The Name must be more than 2 caracters.");
            TxtName.Focus();
            return false;
        }

        return true;
    }

    // Conecta o evento do botão ao método da classe base
    private void BtnSave_Click(object sender, RoutedEventArgs e)
        => SaveClick(sender, e);

    // Mantém o BtnCancel_Click como está (ou pode mover para a classe base também)
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
