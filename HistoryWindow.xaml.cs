using System.IO;
using System.Windows;
using TranscribeWin.Services;

namespace TranscribeWin;

public partial class HistoryWindow : Window
{
    private readonly string _historyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");

    public HistoryWindow()
    {
        InitializeComponent();
        ApplyI18n();
        LoadHistory();
    }

    private void ApplyI18n()
    {
        Title = I18n.Get("HistoryTooltip");
        TitleText.Text = $"🗂️ {I18n.Get("HistoryTooltip")}";
        BtnRefresh.Content = $"🔄 {I18n.Get("HistoryRefresh")}";
        BtnClear.Content = $"🗑️ {I18n.Get("HistoryClear")}";
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
                TxtHistory.Text = File.ReadAllText(_historyPath);
            else
                TxtHistory.Text = I18n.Get("HistoryEmpty");
                
            TxtHistory.ScrollToEnd();
        }
        catch { }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadHistory();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show(
                I18n.Get("HistoryClearConfirm"), 
                I18n.Get("HistoryClear"), 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (File.Exists(_historyPath))
                    File.Delete(_historyPath);
                LoadHistory();
            }
        }
        catch { }
    }
}
