using System.Windows;
using WpfPorkProcessSystem.Services;
using WpfPorkProcessSystem.Windows;

namespace WpfPorkProcessSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ProductService _productService;
        private readonly ClassificationWeighingService _classificationService;
        private readonly SprayChamberService _chamberService;

        public MainWindow()
        {
            InitializeComponent();

            _productService = new ProductService();
            _classificationService = new ClassificationWeighingService();
            _chamberService = new SprayChamberService();
        }

        private void MenuProduct_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Open product form...");

            var window = new Windows.ProdutoListagemWindow
            {
                Owner = this
            };
            window.ShowDialog();

            UpdateStatus("Ready");
        }

        private void MenuClassification_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening classification weighting form...");

            var window = new Windows.ClassificationWeighingListWindow
            {
                Owner = this
            };
            window.ShowDialog();

            UpdateStatus("Ready");            
        }

        private void MenuChambers_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening spray chambers form...");

            var window = new Windows.SprayChamberListWindow
            {
                Owner = this
            };
            window.ShowDialog();

            UpdateStatus("Pronto");
        }

        private void MenuProductionOrder_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Production Order selected");
            var window = new ProductionOrderListWindow
            {
                Owner = this
            };
            window.ShowDialog();
            UpdateStatus("Ready");
        }

        private void MenuRepChambers_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Spray Chambers Report selected");
            MessageBox.Show("Spray Chambers Report Form will be here", "Chambers Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuRepProduction_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Production Report selected");
            MessageBox.Show("Production Report Form will be here", "Production Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateStatus(string mensagem)
        {
            StatusText.Text = mensagem;
        }
    }
}