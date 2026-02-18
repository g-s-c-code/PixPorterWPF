using Microsoft.Win32;
using System.Windows;

namespace PixPorter.WPF.Services;

public sealed class ThemeService
{
    private static readonly ThemeService _instance = new();
    public static ThemeService Instance => _instance;

    private bool _isDark = true;
    public bool IsDark => _isDark;

    private ThemeService() { }

    public void ApplySystemTheme()
    {
        Apply(IsSystemDarkMode());
    }

    public void Apply(bool dark)
    {
        _isDark = dark;
        string themeUri = dark ? "Themes/Dark.xaml" : "Themes/Light.xaml";

        var dict = new ResourceDictionary
        {
            Source = new Uri(themeUri, UriKind.Relative)
        };

        var merged = Application.Current.Resources.MergedDictionaries;
        merged.Clear();
        merged.Add(dict);
    }

    private static bool IsSystemDarkMode()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return true;
        }
    }
}