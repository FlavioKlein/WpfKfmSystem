using System.Windows;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

public partial class SprayChamberFormWindow : SprayChamberFormWindowBase
{

    public SprayChamberFormWindow() : base()
    {
        InitializeComponent();
        base.TxtValidation = TxtValidation;
        TxtName.Focus();
    }

    public SprayChamberFormWindow(int id) : base(id)
    {
        InitializeComponent();
        base.TxtValidation = TxtValidation;
        LoadData();
    }

    protected override void FillForm(SprayChamberModel model)
    {
        TxtTitle.Text = "Edit Spray Chamber";
        PnlId.Visibility = Visibility.Visible;
        TxtIdDisplay.Text = model.Id.ToString();
        TxtName.Text = model.Name;
        TxtDescription.Text = model.Description;
        TxtCapacity.Text = model.Capacity.ToString();
        TxtStock.Text = model.Stock.ToString();
    }

    protected override SprayChamberModel FormDataToModel()
    {
        return new SprayChamberModel
        {
            Id = Id ?? 0,
            Name = TxtName.Text.Trim(),
            Description = TxtDescription.Text.Trim(),
            Capacity = int.Parse(TxtCapacity.Text),
            Stock = int.Parse(TxtStock.Text)
        };
    }

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

        if (string.IsNullOrWhiteSpace(TxtCapacity.Text))
        {
            ShowValidationError("The Capacity field is mandatory.");
            TxtCapacity.Focus();
            return false;
        }

        if (!int.TryParse(TxtCapacity.Text, out var capacidade) || capacidade <= 0)
        {
            ShowValidationError("The Capacity field must be more than zero.");
            TxtCapacity.Focus();
            return false;
        }

        if (!int.TryParse(TxtStock.Text, out var estoque) || estoque < 0)
        {
            ShowValidationError("The Stock field must be a integer number more or equal to zero.");
            TxtStock.Focus();
            return false;
        }

        return true;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
        => SaveClick(sender, e);

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

}
