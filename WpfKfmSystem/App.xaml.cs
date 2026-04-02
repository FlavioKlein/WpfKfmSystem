using System.Windows;
using WpfPorkProcessSystem.Data;

namespace WpfPorkProcessSystem;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DataSeeder.SeedData();
    }
}
