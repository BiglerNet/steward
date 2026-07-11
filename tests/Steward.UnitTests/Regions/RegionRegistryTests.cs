using Steward.Application.Regions;

namespace Steward.UnitTests.Regions;

public class RegionRegistryTests
{
    [Fact]
    public void Has_Two_Countries()
    {
        Assert.Equal(2, RegionRegistry.All.Count);
        Assert.Contains(RegionRegistry.All, c => c.Code == "US");
        Assert.Contains(RegionRegistry.All, c => c.Code == "CA");
    }

    [Fact]
    public void Us_Has_Fifty_States_Plus_Dc()
    {
        var us = RegionRegistry.All.Single(c => c.Code == "US");
        Assert.Equal(51, us.Regions.Count);
        Assert.Contains(us.Regions, r => r.Code == "US-DC");
    }

    [Fact]
    public void Ca_Has_Thirteen_Provinces_And_Territories()
    {
        var ca = RegionRegistry.All.Single(c => c.Code == "CA");
        Assert.Equal(13, ca.Regions.Count);
    }

    [Fact]
    public void All_Region_Codes_Are_Prefixed_By_Their_Country_Code()
    {
        Assert.All(RegionRegistry.All, country =>
            Assert.All(country.Regions, region =>
                Assert.StartsWith($"{country.Code}-", region.Code)));
    }

    [Theory]
    [InlineData("US", true)]
    [InlineData("CA", true)]
    [InlineData("MX", false)]
    [InlineData("", false)]
    public void IsValidCountry_Reflects_Registry(string code, bool expected)
    {
        Assert.Equal(expected, RegionRegistry.IsValidCountry(code));
    }

    [Theory]
    [InlineData("US", "US-WI", true)]
    [InlineData("US", "CA-ON", false)]
    [InlineData("CA", "CA-ON", true)]
    [InlineData("US", "US-XX", false)]
    [InlineData("MX", "US-WI", false)]
    public void IsValidRegion_Reflects_Registry(string country, string region, bool expected)
    {
        Assert.Equal(expected, RegionRegistry.IsValidRegion(country, region));
    }
}
