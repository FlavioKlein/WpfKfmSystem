using System.Windows;
using WpfPorkProcessSystem.Helpers;
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
            // Initialize language BEFORE InitializeComponent
            LanguageManager.InitializeLanguage();

            InitializeComponent();

            _productService = new ProductService();
            _classificationService = new ClassificationWeighingService();
            _chamberService = new SprayChamberService();
            _weighingScaleService = new WeighingScaleService();

            LoadLocalizedStrings();
        }

        private void LoadLocalizedStrings()
        {
            // Window title
            Title = WpfPorkProcessSystem.Resources.Strings.App_Title;

            // Central content
            TxtSubtitle.Text = WpfPorkProcessSystem.Resources.Strings.App_Subtitle;
            TxtDescription1.Text = WpfPorkProcessSystem.Resources.Strings.App_Description1;
            TxtDescription2.Text = WpfPorkProcessSystem.Resources.Strings.App_Description2;
            TxtDescription3.Text = WpfPorkProcessSystem.Resources.Strings.App_Description3;
            TxtDescription4.Text = WpfPorkProcessSystem.Resources.Strings.App_Description4;

            // Menu items
            MenuForms.Header = WpfPorkProcessSystem.Resources.Strings.Menu_Forms;
            MenuProduct.Header = WpfPorkProcessSystem.Resources.Strings.Menu_Product;
            MenuClassification.Header = WpfPorkProcessSystem.Resources.Strings.Menu_ClassificationWeighing;
            MenuSprayChambers.Header = WpfPorkProcessSystem.Resources.Strings.Menu_SprayChambers;
            MenuWeighingScales.Header = WpfPorkProcessSystem.Resources.Strings.Menu_WeighingScales;

            MenuOperation.Header = WpfPorkProcessSystem.Resources.Strings.Menu_Operation;
            MenuProductionOrder.Header = WpfPorkProcessSystem.Resources.Strings.Menu_ProductionOrder;
            MenuSimulator.Header = WpfPorkProcessSystem.Resources.Strings.Menu_Simulator;

            MenuReports.Header = WpfPorkProcessSystem.Resources.Strings.Menu_Reports;
            MenuChamberStatus.Header = WpfPorkProcessSystem.Resources.Strings.Menu_ChamberStatus;
            MenuProduction.Header = WpfPorkProcessSystem.Resources.Strings.Menu_Production;

            MenuLanguage.Header = WpfPorkProcessSystem.Resources.Strings.Menu_Language;

            // Status bar
            StatusText.Text = WpfPorkProcessSystem.Resources.Strings.Status_Ready;
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
            UpdateStatus(WpfPorkProcessSystem.Resources.Strings.Status_Ready);
        }

        private void MenuRepProduction_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening Production Order Report...");
            var window = new ProductionOrderReportWindow
            {
                Owner = this
            };
            window.ShowDialog();
            UpdateStatus(WpfPorkProcessSystem.Resources.Strings.Status_Ready);
        }

        private void MenuLanguagePtBR_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage("pt-BR");
            MessageBox.Show(WpfPorkProcessSystem.Resources.Strings.Msg_LanguageChanged, 
                WpfPorkProcessSystem.Resources.Strings.Dialog_Information, 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private void MenuLanguageEn_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage("en");
            MessageBox.Show(WpfPorkProcessSystem.Resources.Strings.Msg_LanguageChanged, 
                WpfPorkProcessSystem.Resources.Strings.Dialog_Information, 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private void MenuLanguageEs_Click(object sender, RoutedEventArgs e)
        {
            LanguageManager.ChangeLanguage("es");
            MessageBox.Show(WpfPorkProcessSystem.Resources.Strings.Msg_LanguageChanged, 
                WpfPorkProcessSystem.Resources.Strings.Dialog_Information, 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private void UpdateStatus(string mensagem)
        {
            StatusText.Text = mensagem;
        }
    }
}
