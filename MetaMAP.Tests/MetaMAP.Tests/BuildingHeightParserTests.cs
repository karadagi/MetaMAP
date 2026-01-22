using System.Collections.Generic;
using MetaMap.Core;
using Xunit;

namespace MetaMAP.Tests
{
    public class BuildingHeightParserTests
    {
        [Fact]
        public void GetHeightMeters_ParsesMeters()
        {
            var tags = new Dictionary<string, string> { ["height"] = "12" };
            Assert.Equal(12.0, BuildingHeightParser.GetHeightMeters(tags));
        }

        [Fact]
        public void GetHeightMeters_ParsesFeet()
        {
            var tags = new Dictionary<string, string> { ["height"] = "30ft" };
            var height = BuildingHeightParser.GetHeightMeters(tags);
            Assert.InRange(height, 9.14, 9.15);
        }

        [Fact]
        public void GetHeightMeters_UsesLevels()
        {
            var tags = new Dictionary<string, string> { ["building:levels"] = "4" };
            Assert.Equal(12.0, BuildingHeightParser.GetHeightMeters(tags));
        }

        [Fact]
        public void GetHeightMeters_UsesBuildingTypeDefault()
        {
            var tags = new Dictionary<string, string> { ["building"] = "industrial" };
            Assert.Equal(10.0, BuildingHeightParser.GetHeightMeters(tags));
        }

        [Fact]
        public void GetHeightMeters_UsesHouseDefault()
        {
            var tags = new Dictionary<string, string> { ["building"] = "house" };
            Assert.Equal(6.0, BuildingHeightParser.GetHeightMeters(tags));
        }

        [Fact]
        public void GetHeightMeters_UsesApartmentsDefault()
        {
            var tags = new Dictionary<string, string> { ["building"] = "apartments" };
            Assert.Equal(12.0, BuildingHeightParser.GetHeightMeters(tags));
        }

        [Fact]
        public void GetHeightMeters_UsesCommercialDefault()
        {
            var tags = new Dictionary<string, string> { ["building"] = "commercial" };
            Assert.Equal(8.0, BuildingHeightParser.GetHeightMeters(tags));
        }

        [Fact]
        public void GetHeightMeters_UsesSchoolDefault()
        {
            var tags = new Dictionary<string, string> { ["building"] = "school" };
            Assert.Equal(15.0, BuildingHeightParser.GetHeightMeters(tags));
        }

        [Fact]
        public void GetHeightMeters_UsesFallbackForUnknown()
        {
            Assert.Equal(6.0, BuildingHeightParser.GetHeightMeters(null));
        }

        [Fact]
        public void GetBuildingType_ReturnsUnknownWhenMissing()
        {
            Assert.Equal("Unknown", BuildingHeightParser.GetBuildingType(new Dictionary<string, string>()));
        }
    }
}
