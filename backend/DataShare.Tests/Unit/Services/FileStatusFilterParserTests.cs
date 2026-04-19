using DataShare.Application.Services;
using AwesomeAssertions;
using Xunit;

namespace DataShare.Tests.Unit.Services;

public class FileStatusFilterParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryParse_WithNullOrEmpty_ReturnsTrueAndAll(string? input)
    {
        var ok = FileStatusFilterParser.TryParse(input, out var result);

        ok.Should().BeTrue();
        result.Should().Be(FileStatusFilter.All);
    }

    [Theory]
    [InlineData("all", FileStatusFilter.All)]
    [InlineData("ALL", FileStatusFilter.All)]
    [InlineData("active", FileStatusFilter.Active)]
    [InlineData("Active", FileStatusFilter.Active)]
    [InlineData("expired", FileStatusFilter.Expired)]
    [InlineData("EXPIRED", FileStatusFilter.Expired)]
    public void TryParse_WithValidName_ReturnsTrueAndCorrectValue(string input, FileStatusFilter expected)
    {
        var ok = FileStatusFilterParser.TryParse(input, out var result);

        ok.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("banana")]
    [InlineData("unknown")]
    public void TryParse_WithUnknownName_ReturnsFalse(string input)
    {
        var ok = FileStatusFilterParser.TryParse(input, out _);

        ok.Should().BeFalse();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("42")]
    [InlineData("-1")]
    public void TryParse_WithNumericInput_ReturnsFalse(string input)
    {
        var ok = FileStatusFilterParser.TryParse(input, out _);

        ok.Should().BeFalse();
    }
}
