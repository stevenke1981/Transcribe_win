using System.Globalization;
using System.Windows;

namespace TranscribeWin;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set default UI culture to zh-TW
        var culture = new CultureInfo("zh-TW");
        CultureInfo.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}
