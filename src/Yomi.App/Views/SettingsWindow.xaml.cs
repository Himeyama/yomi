using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Yomi.App.Models;
using Yomi.App.Services;

namespace Yomi.App.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _controlsLoaded;
    private bool _validationRequested;

    public event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        LoadIntoControls();
        _controlsLoaded = true;
        UpdateWorkDuration();
        UpdateImagePreview();
    }

    private void LoadIntoControls()
    {
        // 5%刻みで選べるようにし、既存の任意の透明度も保存し直すまで保持する。
        var opacityValues = Enumerable.Range(0, 21).Select(index => index / 20d)
            .Append(_settings.Opacity).Distinct().OrderBy(value => value);
        foreach (var value in opacityValues)
            OpacityCombo.Items.Add(new ComboBoxItem { Content = FormatOpacity(value), Tag = value });
        OpacityCombo.SelectedValuePath = "Tag";
        OpacityCombo.SelectedValue = _settings.Opacity;
        BackgroundColorCombo.SelectedValue = _settings.BackgroundColor.ToString();
        EModeCheck.IsChecked = _settings.EMode;
        ShowClockCheck.IsChecked = _settings.ShowClock;
        ShowWorkHoursCheck.IsChecked = _settings.ShowWorkHours;
        ShowIncomeCheck.IsChecked = _settings.ShowIncome;
        MonthlyBaseSalaryBox.Text = _settings.MonthlyBaseSalary.ToString("#,0.##", CultureInfo.InvariantCulture);
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

    private void EModeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (OpacityCombo is null || BackgroundColorCombo is null) return;
        var isStandardTheme = EModeCheck.IsChecked != true;
        OpacityCombo.IsEnabled = isStandardTheme;
        BackgroundColorCombo.IsEnabled = isStandardTheme;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    private static string FormatOpacity(double value) => $"{Math.Round(value * 100)}%";

    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalHours:D2}:{time.Minutes:D2}";

    private void Input_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_controlsLoaded) return;
        UpdateWorkDuration();
        if (_validationRequested) ValidateInputs(false);
    }

    private void TimeBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var box = (System.Windows.Controls.TextBox)sender;
        if (SettingsInput.TryParseTime(box.Text, out var time)) box.Text = FormatTime(time);
    }

    private void SalaryBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (SettingsInput.TryParseSalary(MonthlyBaseSalaryBox.Text, out var salary))
            MonthlyBaseSalaryBox.Text = salary.ToString("#,0.##", CultureInfo.InvariantCulture);
    }

    private void UpdateWorkDuration()
    {
        if (SettingsInput.TryParseTime(WorkStartTimeBox.Text, out var start) &&
            SettingsInput.TryParseTime(WorkEndTimeBox.Text, out var end) &&
            SettingsInput.TryParseTime(LunchStartTimeBox.Text, out var lunchStart) &&
            SettingsInput.TryParseTime(LunchEndTimeBox.Text, out var lunchEnd) &&
            SettingsInput.TryGetWorkDuration(start, end, lunchStart, lunchEnd, out var duration))
        {
            var overnight = end < start ? "（翌日まで）" : "";
            WorkDurationText.Text = $"実働 {(int)duration.TotalHours}時間{duration.Minutes:00}分{overnight} / 日";
        }
        else WorkDurationText.Text = "勤務・休憩の時刻を入力すると、実働時間を確認できます。";
    }

    private bool ValidateInputs(bool focusError)
    {
        System.Windows.Controls.Control? firstInvalid = null;
        string? message = null;
        void MarkInvalid(System.Windows.Controls.Control control, string error)
        {
            control.Tag = "Invalid";
            firstInvalid ??= control;
            message ??= error;
        }

        var fields = new[] { WorkStartTimeBox, WorkEndTimeBox, LunchStartTimeBox, LunchEndTimeBox };
        var names = new[] { "勤務開始", "勤務終了", "休憩開始", "休憩終了" };
        var times = new TimeSpan[4];
        var validTimes = true;
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i].Tag = null;
            if (SettingsInput.TryParseTime(fields[i].Text, out times[i])) continue;
            validTimes = false;
            MarkInvalid(fields[i], $"{names[i]}を 9:00 または 0900 の形式で入力してください（0:00〜23:59）。");
        }
        if (validTimes && !SettingsInput.TryGetWorkDuration(times[0], times[1], times[2], times[3], out _))
        {
            foreach (var field in fields) field.Tag = "Invalid";
            MarkInvalid(WorkStartTimeBox, "休憩は勤務時間内に設定し、休憩を除く勤務時間を確保してください。");
        }
        MonthlyBaseSalaryBox.Tag = null;
        if (!SettingsInput.TryParseSalary(MonthlyBaseSalaryBox.Text, out _))
            MarkInvalid(MonthlyBaseSalaryBox, "基本給（月）には0以上の金額を入力してください（例：300,000）。");

        CustomImagePathBox.Tag = null;
        if (ShowCustomImageCheck.IsChecked == true && CustomImageLoader.Load(CustomImagePathBox.Text) is null)
            MarkInvalid(CustomImagePathBox, "表示する画像を選択してください。読み込めない場合は、画像を選び直してください。");

        ValidationText.Text = message ?? "";
        ValidationBanner.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
        if (focusError && firstInvalid is not null)
        {
            SettingsTabs.SelectedItem = firstInvalid == CustomImagePathBox ? ImageTab : WorkTab;
            // タブを切り替えた後、レイアウト完了時に対象項目へ移動する。
            var target = firstInvalid;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                target.BringIntoView();
                target.Focus();
                if (target is System.Windows.Controls.TextBox box) box.SelectAll();
            }));
        }
        return message is null;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _validationRequested = true;
        if (!ValidateInputs(true)) return;
        SettingsInput.TryParseSalary(MonthlyBaseSalaryBox.Text, out var monthlyBaseSalary);
        SettingsInput.TryParseTime(WorkStartTimeBox.Text, out var workStart);
        SettingsInput.TryParseTime(WorkEndTimeBox.Text, out var workEnd);
        SettingsInput.TryParseTime(LunchStartTimeBox.Text, out var lunchStart);
        SettingsInput.TryParseTime(LunchEndTimeBox.Text, out var lunchEnd);

        var updated = new AppSettings
        {
            EMode = EModeCheck.IsChecked == true,
            Opacity = OpacityCombo.SelectedValue is double opacity ? opacity : _settings.Opacity,
            BackgroundColor = BackgroundColorCombo.SelectedValue as string == "White"
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
            ImageErrorText.Text = "画像を読み込めません。PNGやJPEGなどの画像を選択してください。";
            ImageErrorText.Visibility = Visibility.Visible;
            return;
        }

        CustomImagePathBox.Text = dialog.FileName;
        ShowCustomImageCheck.IsChecked = true;
        UpdateImagePreview();
        if (_validationRequested) ValidateInputs(false);
    }

    private void ClearCustomImage_Click(object sender, RoutedEventArgs e)
    {
        CustomImagePathBox.Clear();
        ShowCustomImageCheck.IsChecked = false;
        UpdateImagePreview();
        if (_validationRequested) ValidateInputs(false);
    }

    private void ImageSetting_Changed(object sender, RoutedEventArgs e)
    {
        if (_controlsLoaded && _validationRequested) ValidateInputs(false);
    }

    private void UpdateImagePreview()
    {
        CustomImagePreview.Source = CustomImageLoader.Load(CustomImagePathBox.Text);
        ImagePlaceholder.Visibility = CustomImagePreview.Source is null ? Visibility.Visible : Visibility.Collapsed;
        ImagePlaceholder.Text = string.IsNullOrWhiteSpace(CustomImagePathBox.Text)
            ? "ロゴやお気に入りの画像を選択" : "画像が見つからないか、読み込めません";
        ClearImageButton.IsEnabled = !string.IsNullOrWhiteSpace(CustomImagePathBox.Text);
        ImageErrorText.Visibility = Visibility.Collapsed;
    }
}
