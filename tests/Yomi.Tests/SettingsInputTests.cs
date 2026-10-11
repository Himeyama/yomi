using Yomi.App.Services;

namespace Yomi.Tests;

public class SettingsInputTests
{
    [Theory]
    [InlineData("9:00", 9, 0)]
    [InlineData("09:30", 9, 30)]
    [InlineData("900", 9, 0)]
    [InlineData("2359", 23, 59)]
    [InlineData("０９：３０", 9, 30)]
    [InlineData(" ００００ ", 0, 0)]
    public void TimeAcceptsCommonKeyboardAndPasteFormats(string input, int hour, int minute)
    {
        Assert.True(SettingsInput.TryParseTime(input, out var time));
        Assert.Equal(new TimeSpan(hour, minute, 0), time);
    }

    [Theory]
    [InlineData("")]
    [InlineData("24:00")]
    [InlineData("09:60")]
    [InlineData("-1:00")]
    [InlineData("09:00:30")]
    [InlineData("9")]
    [InlineData("9:0")]
    [InlineData("2:99")]
    public void TimeRejectsAmbiguousAndOutOfRangeInput(string input) =>
        Assert.False(SettingsInput.TryParseTime(input, out _));

    [Theory]
    [InlineData("300,000", "300000")]
    [InlineData("３００，０００", "300000")]
    [InlineData(" 0 ", "0")]
    [InlineData("1234.56", "1234.56")]
    public void SalaryAcceptsGroupedAndFullWidthNumbers(string input, string expected)
    {
        Assert.True(SettingsInput.TryParseSalary(input, out var salary));
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), salary);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("")]
    [InlineData("金額")]
    [InlineData("9999999999999999999999999999999")]
    public void SalaryRejectsInvalidInput(string input) =>
        Assert.False(SettingsInput.TryParseSalary(input, out _));

    [Theory]
    [InlineData(9, 18, 12, 13, 8)]
    [InlineData(22, 6, 1, 2, 7)]
    [InlineData(22, 6, 23, 0, 7)]
    public void DurationHandlesDayAndOvernightShifts(int start, int end, int lunchStart, int lunchEnd, int hours)
    {
        Assert.True(SettingsInput.TryGetWorkDuration(TimeSpan.FromHours(start), TimeSpan.FromHours(end),
            TimeSpan.FromHours(lunchStart), TimeSpan.FromHours(lunchEnd), out var duration));
        Assert.Equal(TimeSpan.FromHours(hours), duration);
    }

    [Theory]
    [InlineData(9, 18, 8, 10)]
    [InlineData(9, 18, 17, 19)]
    [InlineData(9, 18, 13, 12)]
    [InlineData(9, 18, 12, 12)]
    [InlineData(9, 18, 9, 18)]
    [InlineData(9, 9, 12, 13)]
    public void DurationRejectsBreaksOutsideShiftAndZeroPaidTime(int start, int end, int lunchStart, int lunchEnd) =>
        Assert.False(SettingsInput.TryGetWorkDuration(TimeSpan.FromHours(start), TimeSpan.FromHours(end),
            TimeSpan.FromHours(lunchStart), TimeSpan.FromHours(lunchEnd), out _));
}
