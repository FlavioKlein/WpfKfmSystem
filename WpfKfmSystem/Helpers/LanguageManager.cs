using System.Globalization;
using System.Threading;

namespace WpfPorkProcessSystem.Helpers;

/// <summary>
/// Manages application language/culture settings.
/// </summary>
public static class LanguageManager
{
    private const string DefaultCulture = "pt-BR";

    /// <summary>
    /// Changes the application culture to the specified language.
    /// </summary>
    /// <param name="cultureName">Culture name (e.g., "pt-BR", "en", "es")</param>
    public static void ChangeLanguage(string cultureName)
    {
        try
        {
            var culture = new CultureInfo(cultureName);
            
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            // Save preference to settings
            Properties.Settings.Default.Language = cultureName;
            Properties.Settings.Default.Save();
        }
        catch (CultureNotFoundException)
        {
            // Fallback to default culture if invalid
            ChangeLanguage(DefaultCulture);
        }
    }

    /// <summary>
    /// Initializes the application culture from saved settings.
    /// </summary>
    public static void InitializeLanguage()
    {
        var savedLanguage = Properties.Settings.Default.Language;
        
        if (string.IsNullOrEmpty(savedLanguage))
        {
            savedLanguage = DefaultCulture;
        }
        
        ChangeLanguage(savedLanguage);
    }

    /// <summary>
    /// Gets the current application culture name.
    /// </summary>
    public static string GetCurrentLanguage()
    {
        return Thread.CurrentThread.CurrentUICulture.Name;
    }
}
