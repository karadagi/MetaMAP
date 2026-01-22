using System.Globalization;
using MetaMap.Core;
using Xunit;

namespace MetaMAP.Tests
{
    public class OverpassQueryBuilderTests
    {
        [Fact]
        public void BuildBuildingQuery_IncludesExpectedTagsAndBounds()
        {
            var bbox = GeoMath.BoundingBox(41.0, 29.0, 200.0);
            var query = OverpassQueryBuilder.BuildBuildingQuery(41.0, 29.0, 200.0);

            Assert.Contains("building", query);
            Assert.Contains("building:part", query);
            Assert.Contains(bbox.South.ToString(CultureInfo.InvariantCulture), query);
            Assert.Contains(bbox.West.ToString(CultureInfo.InvariantCulture), query);
            Assert.Contains(bbox.North.ToString(CultureInfo.InvariantCulture), query);
            Assert.Contains(bbox.East.ToString(CultureInfo.InvariantCulture), query);
        }

        [Fact]
        public void BuildContourQuery_IncludesBounds()
        {
            var query = OverpassQueryBuilder.BuildContourQuery(-1.0, -2.0, 3.0, 4.0);
            Assert.Contains("-1", query);
            Assert.Contains("-2", query);
            Assert.Contains("3", query);
            Assert.Contains("4", query);
            Assert.Contains("contour", query);
            Assert.Contains("ele", query);
        }
    }
}
