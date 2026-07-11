using Steward.Application.VinDecode;

namespace Steward.UnitTests.VinDecode;

public class VinFormatTests
{
    [Theory]
    [InlineData("1HGCM82633A004352")]
    [InlineData("1hgcm82633a004352")]
    public void IsValid_Accepts_Well_Formed_Vin(string vin)
    {
        Assert.True(VinFormat.IsValid(vin));
    }

    [Theory]
    [InlineData("1HGCM82633A00435")]
    [InlineData("1HGCM82633A0043522")]
    [InlineData("1HGCM8263IA004352")]
    [InlineData("1HGCM8263OA004352")]
    [InlineData("1HGCM8263QA004352")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_Rejects_Malformed_Vin(string? vin)
    {
        Assert.False(VinFormat.IsValid(vin));
    }
}
