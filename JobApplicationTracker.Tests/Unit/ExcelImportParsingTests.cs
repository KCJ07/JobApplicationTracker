using JobApplicationTracker.Services;
using Xunit;

namespace JobApplicationTracker.Tests.Unit;

// ParseYesNo and TryParseDate are internal on ExcellImportService, exposed here via InternalsVisibleTo
public class ExcelImportParsingTests
{
    [Theory]
    [InlineData("yes", true)]
    [InlineData("Y", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("x", true)] // checkbox style columns often just have an x
    [InlineData("no", false)]
    [InlineData("", false)]
    [InlineData("maybe", false)]
    public void ParseYesNo_RecognizesCommonAffirmativeValues(string input, bool expected)
    {
        var result = ExcellImportService.ParseYesNo(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-01-15", true)] // ISO, what CellText normalizes real Excel dates to
    [InlineData("01/15/2026", true)] // still accepted if typed in manually
    [InlineData("not a date", false)]
    [InlineData("", false)]
    public void TryParseDate_HandlesValidAndInvalidInput(string input, bool expectSuccess)
    {
        var success = ExcellImportService.TryParseDate(input, out var date);

        Assert.Equal(expectSuccess, success);
    }

    [Fact]
    public void TryParseDate_ParsesToTheExpectedCalendarDate()
    {
        var success = ExcellImportService.TryParseDate("2026-03-05", out var date);

        Assert.True(success);
        Assert.Equal(new DateOnly(2026, 3, 5), date);
    }
}
