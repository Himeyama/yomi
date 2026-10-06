using Yomi.App.Models;

namespace Yomi.App.Services;

public static class IncomeCalculator
{
    public static decimal CalculateDailyIncome(AppSettings settings, DateTime now)
    {
        if (settings.MonthlyBaseSalary <= 0) return 0;

        var workDuration = Normalize(settings.WorkEndTime, settings.WorkStartTime);
        var lunchStart = Normalize(settings.LunchStartTime, settings.WorkStartTime);
        var lunchEnd = Normalize(settings.LunchEndTime, settings.WorkStartTime);
        var paidDuration = workDuration - (lunchEnd - lunchStart);
        if (lunchStart >= lunchEnd || lunchEnd > workDuration || paidDuration <= TimeSpan.Zero)
            return 0;

        // 秒単位で算出し、夜勤でも暦日の午前0時にリセットする。
        var elapsedToday = TimeSpan.FromTicks(now.TimeOfDay.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);
        long paidTicks = 0;
        foreach (var shiftOffset in new[] { -TimeSpan.TicksPerDay, 0L })
        {
            var start = settings.WorkStartTime.Ticks + shiftOffset;
            paidTicks += Overlap(start, start + lunchStart.Ticks, elapsedToday.Ticks);
            paidTicks += Overlap(start + lunchEnd.Ticks, start + workDuration.Ticks, elapsedToday.Ticks);
        }

        return settings.MonthlyBaseSalary / 20m * ((decimal)paidTicks / paidDuration.Ticks);
    }

    private static long Overlap(long start, long end, long elapsedToday) =>
        Math.Max(0, Math.Min(end, elapsedToday) - Math.Max(start, 0));

    private static TimeSpan Normalize(TimeSpan value, TimeSpan basis)
    {
        var difference = value - basis;
        return difference < TimeSpan.Zero ? difference + TimeSpan.FromDays(1) : difference;
    }
}
