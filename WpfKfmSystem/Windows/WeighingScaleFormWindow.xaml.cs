using System;
using System.Windows;
using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Windows.Base;

namespace WpfPorkProcessSystem.Windows;

public partial class WeighingScaleFormWindow : WeighingScaleFormWindowBase
{
    public WeighingScaleFormWindow() : base()
    {
        InitializeComponent();
        base.TxtValidation = TxtValidation;
        Title = "New Weighing Scale"; // Temporário - adicionar ao resources depois
        InitializeComboBox();
        TxtName.Focus();
    }

    public WeighingScaleFormWindow(int id) : base(id)
    {
        InitializeComponent();
        base.TxtValidation = TxtValidation;
        Title = "Edit Weighing Scale"; // Temporário - adicionar ao resources depois
        InitializeComboBox();
        LoadData();
    }

    private void InitializeComboBox()
    {
        CmbType.ItemsSource = Enum.GetValues(typeof(WeighingScaleType));
        CmbType.SelectedIndex = 0;
    }

    protected override void FillForm(WeighingScaleModel model)
    {
        TxtTitle.Text = "Edit Weighing Scale"; // Temporário - adicionar ao resources depois
        PnlId.Visibility = Visibility.Visible;
        TxtIdDisplay.Text = model.Id.ToString();
        TxtName.Text = model.Name;
        CmbType.SelectedItem = model.Type;
    }

    protected override WeighingScaleModel FormDataToModel()
    {
        return new WeighingScaleModel
        {
            Id = Id ?? 0,
            Name = TxtName.Text.Trim(),
            Type = (WeighingScaleType)(CmbType.SelectedItem ?? WeighingScaleType.Tent)
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

        if (TxtName.Text.Trim().Length < 3)
        {
            ShowValidationError("The Name must be more than 2 characters.");
            TxtName.Focus();
            return false;
        }

        if (CmbType.SelectedItem == null)
        {
            ShowValidationError("Please select a Type.");
            CmbType.Focus();
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
