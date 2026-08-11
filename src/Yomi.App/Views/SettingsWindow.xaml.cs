using System.Windows;
using Yomi.App.Models;

namespace Yomi.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        LoadIntoControls();
    }

    private void LoadIntoControls()
    {
        OpacitySlider.Value = _settings.Opacity;
        OpacityValueText.Text = FormatOpacity(_settings.Opacity);
        BackgroundBlackRadio.IsChecked = _settings.BackgroundColor == OverlayBackgroundColor.Black;
        BackgroundWhiteRadio.IsChecked = _settings.BackgroundColor == OverlayBackgroundColor.White;
        ShowCpuCheck.IsChecked = _settings.ShowCpu;
        ShowMemoryCheck.IsChecked = _settings.ShowMemory;
        ShowGpuCheck.IsChecked = _settings.ShowGpu;
        ShowVramCheck.IsChecked = _settings.ShowVram;
        ShowDiskCheck.IsChecked = _settings.ShowDisk;
        ShowNetworkCheck.IsChecked = _settings.ShowNetwork;
        StartWithWindowsCheck.IsChecked = _settings.StartWithWindows;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueText is null) return;
        OpacityValueText.Text = FormatOpacity(e.NewValue);
    }

    private static string FormatOpacity(double value) => $"{Math.Round(value * 100)}%";

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var updated = new AppSettings
        {
            Opacity = OpacitySlider.Value,
            BackgroundColor = BackgroundWhiteRadio.IsChecked == true
                ? OverlayBackgroundColor.White
                : OverlayBackgroundColor.Black,
            ShowCpu = ShowCpuCheck.IsChecked ?? true,
            ShowMemory = ShowMemoryCheck.IsChecked ?? true,
            ShowGpu = ShowGpuCheck.IsChecked ?? true,
            ShowVram = ShowVramCheck.IsChecked ?? true,
            ShowDisk = ShowDiskCheck.IsChecked ?? true,
            ShowNetwork = ShowNetworkCheck.IsChecked ?? true,
            StartWithWindows = StartWithWindowsCheck.IsChecked ?? true,
        };

        SettingsSaved?.Invoke(updated);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
