using PixPorter.WPF.Services;
using System.Windows;

namespace PixPorter.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeService.Instance.ApplySystemTheme();
    }
}