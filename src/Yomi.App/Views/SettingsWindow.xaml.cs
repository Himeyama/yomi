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
        ShowCpuCheck.IsChecked = _settings.ShowCpu;
        ShowMemoryCheck.IsChecked = _settings.ShowMemory;
        ShowGpuCheck.IsChecked = _settings.ShowGpu;
        ShowVramCheck.IsChecked = _settings.ShowVram;
        ShowNetworkCheck.IsChecked = _settings.ShowNetwork;
        StartWithWindowsCheck.IsChecked = _settings.StartWithWindows;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var updated = new AppSettings
        {
            Opacity = OpacitySlider.Value,
            ShowCpu = ShowCpuCheck.IsChecked ?? true,
            ShowMemory = ShowMemoryCheck.IsChecked ?? true,
            ShowGpu = ShowGpuCheck.IsChecked ?? true,
            ShowVram = ShowVramCheck.IsChecked ?? true,
            ShowNetwork = ShowNetworkCheck.IsChecked ?? true,
            StartWithWindows = StartWithWindowsCheck.IsChecked ?? true,
        };

        SettingsSaved?.Invoke(updated);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
