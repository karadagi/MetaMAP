using System.Globalization;
using MetaMap.Core;
using Xunit;

namespace MetaMAP.Tests
{
    public class AdvancedBboxCalculatorTests
    {
        [Fact]
        public void CalculateBbox_ReturnsExpectedFormat()
        {
            var bbox = AdvancedBboxCalculator.CalculateBbox(41.0, 29.0, 500.0);
            Assert.EndsWith(",EPSG:4326", bbox);

            var parts = bbox.Replace(",EPSG:4326", "").Split(',');
            Assert.Equal(4, parts.Length);

            var minLon = double.Parse(parts[0], CultureInfo.InvariantCulture);
            var minLat = double.Parse(parts[1], CultureInfo.InvariantCulture);
            var maxLon = double.Parse(parts[2], CultureInfo.InvariantCulture);
            var maxLat = double.Parse(parts[3], CultureInfo.InvariantCulture);

            Assert.True(minLat < maxLat);
            Assert.True(minLon < maxLon);
        }
    }
}
