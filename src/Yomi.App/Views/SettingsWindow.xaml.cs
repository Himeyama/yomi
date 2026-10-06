using System.Globalization;
using System.Windows;
using Yomi.App.Models;
using Yomi.App.Services;

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
        ShowClockCheck.IsChecked = _settings.ShowClock;
        ShowWorkHoursCheck.IsChecked = _settings.ShowWorkHours;
        ShowIncomeCheck.IsChecked = _settings.ShowIncome;
        MonthlyBaseSalaryBox.Text = _settings.MonthlyBaseSalary.ToString("0.##", CultureInfo.InvariantCulture);
        WorkStartTimeBox.Text = FormatTime(_settings.WorkStartTime);
        WorkEndTimeBox.Text = FormatTime(_settings.WorkEndTime);
        LunchStartTimeBox.Text = FormatTime(_settings.LunchStartTime);
        LunchEndTimeBox.Text = FormatTime(_settings.LunchEndTime);
        ShowCpuCheck.IsChecked = _settings.ShowCpu;
        ShowMemoryCheck.IsChecked = _settings.ShowMemory;
        ShowGpuCheck.IsChecked = _settings.ShowGpu;
        ShowVramCheck.IsChecked = _settings.ShowVram;
        ShowDiskCheck.IsChecked = _settings.ShowDisk;
        ShowNetworkCheck.IsChecked = _settings.ShowNetwork;
        StartWithWindowsCheck.IsChecked = _settings.StartWithWindows;
        ShowCustomImageCheck.IsChecked = _settings.ShowCustomImage;
        CustomImagePathBox.Text = _settings.CustomImagePath ?? "";
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueText is null) return;
        OpacityValueText.Text = FormatOpacity(e.NewValue);
    }

    private static string FormatOpacity(double value) => $"{Math.Round(value * 100)}%";

    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalHours:D2}:{time.Minutes:D2}";

    private static bool TryParseTime(string text, out TimeSpan time) =>
        TimeSpan.TryParseExact(text.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out time);

    /// <summary>基準時刻からの経過時間に正規化する。基準より前の時刻は翌日分とみなし24時間加算する。</summary>
    private static TimeSpan Normalize(TimeSpan value, TimeSpan basis)
    {
        var diff = value - basis;
        return diff < TimeSpan.Zero ? diff + TimeSpan.FromHours(24) : diff;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(MonthlyBaseSalaryBox.Text.Trim(),
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out var monthlyBaseSalary) || monthlyBaseSalary < 0)
        {
            System.Windows.MessageBox.Show(this, "基本給 (月)は0以上の金額を入力してください。", "yomi 設定",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseTime(WorkStartTimeBox.Text, out var workStart) ||
            !TryParseTime(WorkEndTimeBox.Text, out var workEnd) ||
            !TryParseTime(LunchStartTimeBox.Text, out var lunchStart) ||
            !TryParseTime(LunchEndTimeBox.Text, out var lunchEnd))
        {
            System.Windows.MessageBox.Show(this, "業務時間は HH:mm 形式で入力してください(例: 09:00)。", "yomi 設定",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 開始 >= 終了は夜勤(例: 22:00-5:00、当日開始・翌日終了)として扱う。
        // 開始時刻を 0 起点に正規化した上で、休憩が勤務時間内に収まっているか判定する。
        var normalizedWorkEnd = Normalize(workEnd, workStart);
        var normalizedLunchStart = Normalize(lunchStart, workStart);
        var normalizedLunchEnd = Normalize(lunchEnd, workStart);
        if (normalizedLunchStart >= normalizedLunchEnd || normalizedLunchEnd > normalizedWorkEnd)
        {
            System.Windows.MessageBox.Show(this, "業務時間・休憩の前後関係が不正です。", "yomi 設定",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var updated = new AppSettings
        {
            Opacity = OpacitySlider.Value,
            BackgroundColor = BackgroundWhiteRadio.IsChecked == true
                ? OverlayBackgroundColor.White
                : OverlayBackgroundColor.Black,
            ShowClock = ShowClockCheck.IsChecked ?? true,
            ShowWorkHours = ShowWorkHoursCheck.IsChecked ?? true,
            ShowIncome = ShowIncomeCheck.IsChecked ?? false,
            MonthlyBaseSalary = monthlyBaseSalary,
            WorkStartTime = workStart,
            WorkEndTime = workEnd,
            LunchStartTime = lunchStart,
            LunchEndTime = lunchEnd,
            ShowCpu = ShowCpuCheck.IsChecked ?? true,
            ShowMemory = ShowMemoryCheck.IsChecked ?? true,
            ShowGpu = ShowGpuCheck.IsChecked ?? true,
            ShowVram = ShowVramCheck.IsChecked ?? true,
            ShowDisk = ShowDiskCheck.IsChecked ?? true,
            ShowNetwork = ShowNetworkCheck.IsChecked ?? true,
            StartWithWindows = StartWithWindowsCheck.IsChecked ?? true,
            ShowCustomImage = ShowCustomImageCheck.IsChecked ?? false,
            CustomImagePath = string.IsNullOrWhiteSpace(CustomImagePathBox.Text) ? null : CustomImagePathBox.Text,
        };

        SettingsSaved?.Invoke(updated);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void SelectCustomImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "カスタム画像を選択",
            Filter = "画像ファイル|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|すべてのファイル|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };
        if (dialog.ShowDialog(this) != true) return;

        if (CustomImageLoader.Load(dialog.FileName) is null)
        {
            System.Windows.MessageBox.Show(this, "画像を読み込めません。PNGやJPEGなどの画像を選択してください。", "yomi 設定",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CustomImagePathBox.Text = dialog.FileName;
    }

    private void ClearCustomImage_Click(object sender, RoutedEventArgs e)
    {
        CustomImagePathBox.Clear();
        ShowCustomImageCheck.IsChecked = false;
    }
}
