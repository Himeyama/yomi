using Yomi.App.Models;
using Yomi.App.Services;

namespace Yomi.Tests;

public sealed class IncomeCalculatorTests
{
    private static AppSettings DaySettings() => new()
    {
        MonthlyBaseSalary = 300_000m,
        WorkStartTime = new TimeSpan(8, 0, 0),
        WorkEndTime = new TimeSpan(17, 0, 0),
        LunchStartTime = new TimeSpan(12, 0, 0),
        LunchEndTime = new TimeSpan(13, 0, 0),
    };

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(7, 59, 59, 0)]
    [InlineData(8, 0, 0, 0)]
    [InlineData(8, 0, 1, 0.5208333333)]
    [InlineData(8, 1, 0, 31.25)]
    [InlineData(12, 0, 0, 7500)]
    [InlineData(12, 30, 0, 7500)]
    [InlineData(12, 59, 59, 7500)]
    [InlineData(13, 0, 0, 7500)]
    [InlineData(13, 0, 1, 7500.5208333333)]
    [InlineData(17, 0, 0, 15000)]
    [InlineData(23, 59, 59, 15000)]
    public void DayShift_AccruesOnlyWholeWorkingSeconds(int hour, int minute, int second, double expected)
    {
        var now = new DateTime(2026, 10, 7, hour, minute, second).AddMilliseconds(999);
        var income = IncomeCalculator.CalculateDailyIncome(DaySettings(), now);

        Assert.InRange(Math.Abs(income - (decimal)expected), 0m, 0.00000001m);
    }

    [Fact]
    public void NextDay_StartsAtZero()
    {
        var settings = DaySettings();
        Assert.Equal(15000m, IncomeCalculator.CalculateDailyIncome(settings, new DateTime(2026, 10, 7, 23, 59, 59)));
        Assert.Equal(0m, IncomeCalculator.CalculateDailyIncome(settings, new DateTime(2026, 10, 8)));
    }

    [Theory]
    [InlineData(23, 0, 21600)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 3600)]
    [InlineData(2, 0, 7200)]
    [InlineData(2, 30, 7200)]
    [InlineData(3, 0, 7200)]
    [InlineData(6, 0, 18000)]
    [InlineData(22, 0, 18000)]
    public void NightShift_CountsOnlyWorkWithinCalendarDay(int hour, int minute, int expected)
    {
        var settings = new AppSettings
        {
            MonthlyBaseSalary = 504_000m,
            WorkStartTime = new TimeSpan(22, 0, 0),
            WorkEndTime = new TimeSpan(6, 0, 0),
            LunchStartTime = new TimeSpan(2, 0, 0),
            LunchEndTime = new TimeSpan(3, 0, 0),
        };

        var income = IncomeCalculator.CalculateDailyIncome(settings, new DateTime(2026, 10, 7, hour, minute, 0));
        Assert.InRange(Math.Abs(income - expected), 0m, 0.00000001m);
    }

    [Fact]
    public void UnsetSalary_ProducesZero()
    {
        var settings = DaySettings();
        settings.MonthlyBaseSalary = 0;
        Assert.Equal(0m, IncomeCalculator.CalculateDailyIncome(settings, new DateTime(2026, 10, 7, 17, 0, 0)));
    }

    [Fact]
    public void NoPaidWorkingTime_ProducesZero()
    {
        var settings = DaySettings();
        settings.LunchStartTime = settings.WorkStartTime;
        settings.LunchEndTime = settings.WorkEndTime;
        Assert.Equal(0m, IncomeCalculator.CalculateDailyIncome(settings, new DateTime(2026, 10, 7, 17, 0, 0)));
    }
}
