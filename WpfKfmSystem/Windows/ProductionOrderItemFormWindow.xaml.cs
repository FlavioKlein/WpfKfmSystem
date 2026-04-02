using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfPorkProcessSystem.Models;
using WpfPorkProcessSystem.Services;

namespace WpfPorkProcessSystem.Windows;

public partial class ProductionOrderItemFormWindow : Window
{
    private readonly SprayChamberService _sprayChamberService;
    private readonly ClassificationWeighingService _classificationService;
    private readonly bool _editMode;
    private List<ClassificationWeighingModel> _allClassifications;

    public ProductionOrderItemModel? Item { get; private set; }

    public ProductionOrderItemFormWindow(SprayChamberService sprayChamberService, ClassificationWeighingService classificationService)
    {
        InitializeComponent();
        _sprayChamberService = sprayChamberService;
        _classificationService = classificationService;
        _editMode = false;
        _allClassifications = new List<ClassificationWeighingModel>();
        
        InitializeData();
    }

    public ProductionOrderItemFormWindow(SprayChamberService sprayChamberService, ClassificationWeighingService classificationService, ProductionOrderItemModel item)
    {
        InitializeComponent();
        _sprayChamberService = sprayChamberService;
        _classificationService = classificationService;
        _editMode = true;
        _allClassifications = new List<ClassificationWeighingModel>();
        
        InitializeData();
        FillForm(item);
    }

    private void InitializeData()
    {
        // Load Spray Chambers
        var chambers = _sprayChamberService.GetAll();
        CmbSprayChamber.ItemsSource = chambers;
        if (chambers.Any() && !_editMode)
            CmbSprayChamber.SelectedIndex = 0;

        // Load Classifications
        _allClassifications = _classificationService.GetAll();
        IcClassifications.ItemsSource = _allClassifications;
    }

    private void FillForm(ProductionOrderItemModel item)
    {
        TxtTitle.Text = "Edit Production Order Item";
        
        CmbSprayChamber.SelectedValue = item.SprayChamberId;
        TxtChamberCapacity.Text = item.SprayChamberCapacity.ToString();
        TxtChamberStock.Text = item.SprayChamberStock.ToString();

        // Check the classifications
        if (item.AcceptClassificationIds != null && item.AcceptClassificationIds.Length > 0)
        {
            foreach (var checkBox in GetAllCheckBoxes())
            {
                if (checkBox.Tag is int classificationId && item.AcceptClassificationIds.Contains(classificationId))
                {
                    checkBox.IsChecked = true;
                }
            }
        }
    }

    private void CmbSprayChamber_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSprayChamber.SelectedItem is SprayChamberModel chamber)
        {
            TxtChamberCapacity.Text = chamber.Capacity.ToString();
            TxtChamberStock.Text = chamber.Stock.ToString();
        }
    }

    private IEnumerable<CheckBox> GetAllCheckBoxes()
    {
        var checkBoxes = new List<CheckBox>();
        foreach (var item in IcClassifications.Items)
        {
            var container = IcClassifications.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
            if (container != null)
            {
                var checkBox = FindVisualChild<CheckBox>(container);
                if (checkBox != null)
                    checkBoxes.Add(checkBox);
            }
        }
        return checkBoxes;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    private bool ValidateFields()
    {
        TxtValidation.Visibility = Visibility.Collapsed;

        if (CmbSprayChamber.SelectedItem == null)
        {
            TxtValidation.Text = "Please select a Spray Chamber.";
            TxtValidation.Visibility = Visibility.Visible;
            CmbSprayChamber.Focus();
            return false;
        }

        var selectedClassifications = GetSelectedClassifications();
        if (!selectedClassifications.Any())
        {
            TxtValidation.Text = "Please select at least one classification.";
            TxtValidation.Visibility = Visibility.Visible;
            return false;
        }

        return true;
    }

    private List<int> GetSelectedClassifications()
    {
        var selected = new List<int>();
        foreach (var checkBox in GetAllCheckBoxes())
        {
            if (checkBox.IsChecked == true && checkBox.Tag is int classificationId)
            {
                selected.Add(classificationId);
            }
        }
        return selected;
    }

    private string GetClassificationNames(List<int> classificationIds)
    {
        var names = _allClassifications
            .Where(c => classificationIds.Contains(c.Id))
            .Select(c => c.Name)
            .ToList();

        return string.Join(", ", names);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateFields())
            return;

        try
        {
            var chamber = CmbSprayChamber.SelectedItem as SprayChamberModel;
            var selectedClassificationIds = GetSelectedClassifications();

            Item = new ProductionOrderItemModel
            {
                SprayChamberId = chamber?.Id ?? 0,
                SprayChamberCapacity = chamber?.Capacity ?? 0,
                SprayChamberStock = chamber?.Stock ?? 0,
                SprayChamberInitialStock = chamber?.Stock ?? 0,
                AcceptClassificationIds = selectedClassificationIds.ToArray(),
                AcceptClassifications = GetClassificationNames(selectedClassificationIds)
            };

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
