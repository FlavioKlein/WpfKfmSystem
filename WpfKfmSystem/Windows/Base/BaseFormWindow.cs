using System.Windows;
using System.Windows.Controls;
using WpfPorkProcessSystem.Interfaces;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Windows.Base;

public abstract class BaseFormWindow<TModel, TService> : Window
    where TModel : BaseModel, new()
    where TService : IBaseService<TModel>, new()
{
    protected readonly TService Service;
    protected readonly int? Id;
    protected bool EditMode;
    protected TextBlock TxtValidation;

    protected BaseFormWindow(): base()
    {
        Service = new TService();
        EditMode = false;
    }

    protected BaseFormWindow(int id) : this()
    {
        Id = id;
        EditMode = true;
    }

    protected void LoadData()
    {
        if (Id.HasValue)
        {
            var item = Service.GetById(Id.Value);
            if (item != null)
            {
                FillForm(item);
            }
            else
            {
                ShowError("Registry not found.");
                Close();
            }
        }
    }

    protected abstract void FillForm(TModel model);
    protected abstract TModel FormDataToModel();
    protected abstract bool FieldValidate();

    protected void SaveClick(object sender, RoutedEventArgs e)
    {
        if (!FieldValidate()) return;

        try
        {
            var model = FormDataToModel();

            if (EditMode)
                Service.Update(model);
            else
                Service.Add(model);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Save error: {ex.Message}");
        }
    }

    protected void ShowValidationError(string mensagem)
    {
        if (TxtValidation != null)
        {
            TxtValidation.Text = mensagem;
            TxtValidation.Visibility = Visibility.Visible;
        }
    }

    protected void HideValidationError()
    {
        if (TxtValidation != null)
        {
            TxtValidation.Visibility = Visibility.Collapsed;
        }
    }

    protected void ShowError(string mensagem)
    {
        MessageBox.Show(mensagem, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}