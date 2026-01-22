using MetaMap.Core;
using Xunit;

namespace MetaMAP.Tests
{
    public class GeoMathTests
    {
        [Fact]
        public void LatDeltaFromMeters_OneDegreeAt111Km()
        {
            var delta = GeoMath.LatDeltaFromMeters(111000.0);
            Assert.InRange(delta, 0.999, 1.001);
        }

        [Fact]
        public void LonDeltaFromMeters_OneDegreeAtEquator()
        {
            var delta = GeoMath.LonDeltaFromMeters(111000.0, 0.0);
            Assert.InRange(delta, 0.999, 1.001);
        }

        [Fact]
        public void BoundingBox_OrdersBounds()
        {
            var bbox = GeoMath.BoundingBox(10.0, 20.0, 1000.0);
            Assert.True(bbox.South < bbox.North);
            Assert.True(bbox.West < bbox.East);
            Assert.InRange(10.0, bbox.South, bbox.North);
            Assert.InRange(20.0, bbox.West, bbox.East);
        }

        [Fact]
        public void ToLocalXY_CenterIsZero()
        {
            var local = GeoMath.ToLocalXY(41.0, 29.0, 41.0, 29.0);
            Assert.InRange(local.X, -0.0001, 0.0001);
            Assert.InRange(local.Y, -0.0001, 0.0001);
        }
    }
}
