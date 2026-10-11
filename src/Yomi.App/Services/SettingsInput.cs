using System.Globalization;
using System.Text;

namespace Yomi.App.Services;

/// <summary>設定入力の表記ゆれを吸収し、保存可能な値に変換する。</summary>
public static class SettingsInput
{
    public static bool TryParseTime(string text, out TimeSpan time)
    {
        time = default;
        var value = text.Normalize(NormalizationForm.FormKC).Trim();
        var parts = value.Split(':');
        string hours;
        string minutes;
        if (parts.Length == 2 && parts[0].Length is 1 or 2 && parts[1].Length == 2)
        {
            hours = parts[0];
            minutes = parts[1];
        }
        else if (parts.Length == 1 && value.Length is 3 or 4)
        {
            hours = value[..^2];
            minutes = value[^2..];
        }
        else return false;

        if (!int.TryParse(hours, NumberStyles.None, CultureInfo.InvariantCulture, out var hour) ||
            !int.TryParse(minutes, NumberStyles.None, CultureInfo.InvariantCulture, out var minute) ||
            hour > 23 || minute > 59)
            return false;

        time = new TimeSpan(hour, minute, 0);
        return true;
    }

    public static bool TryParseSalary(string text, out decimal salary) =>
        decimal.TryParse(text.Normalize(NormalizationForm.FormKC).Trim(),
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture, out salary) && salary >= 0;

    public static bool TryGetWorkDuration(TimeSpan start, TimeSpan end,
        TimeSpan lunchStart, TimeSpan lunchEnd, out TimeSpan paidDuration)
    {
        var workDuration = Normalize(end, start);
        var breakStart = Normalize(lunchStart, start);
        var breakEnd = Normalize(lunchEnd, start);
        paidDuration = workDuration - (breakEnd - breakStart);
        return breakStart < breakEnd && breakEnd <= workDuration && paidDuration > TimeSpan.Zero;
    }

    private static TimeSpan Normalize(TimeSpan value, TimeSpan basis)
    {
        var difference = value - basis;
        return difference < TimeSpan.Zero ? difference + TimeSpan.FromDays(1) : difference;
    }
}
