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
        private readonly WeighingScaleService _weighingScaleService;

        public MainWindow()
        {
            InitializeComponent();

            _productService = new ProductService();
            _classificationService = new ClassificationWeighingService();
            _chamberService = new SprayChamberService();
            _weighingScaleService = new WeighingScaleService();
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

        private void MenuWeighingScales_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening weighing scales form...");

            var window = new Windows.WeighingScaleListWindow
            {
                Owner = this
            };
            window.ShowDialog();

            UpdateStatus("Ready");
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

        private void MenuSimulator_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening Production Order Simulator...");
            var window = new ProductionOrderSimulatorWindow
            {
                Owner = this
            };
            window.ShowDialog();
            UpdateStatus("Ready");
        }

        private void MenuRepChambers_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening Chamber Stock Report...");
            var window = new ChamberStockReportWindow
            {
                Owner = this
            };
            window.ShowDialog();
            UpdateStatus("Ready");
        }

        private void MenuRepProduction_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening Production Order Report...");
            var window = new ProductionOrderReportWindow
            {
                Owner = this
            };
            window.ShowDialog();
            UpdateStatus("Ready");
        }

        private void UpdateStatus(string mensagem)
        {
            StatusText.Text = mensagem;
        }
    }
}