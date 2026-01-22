using System.Collections.Generic;
using MetaMap.Core;
using Xunit;

namespace MetaMAP.Tests
{
    public class IdwInterpolatorTests
    {
        [Fact]
        public void InterpolateElevation_ReturnsZeroWhenNoPoints()
        {
            var elevation = IdwInterpolator.InterpolateElevation(0, 0, new List<IdwInterpolator.ElevationPoint>());
            Assert.Equal(0.0, elevation);
        }

        [Fact]
        public void InterpolateElevation_ReturnsExactMatch()
        {
            var points = new List<IdwInterpolator.ElevationPoint>
            {
                new IdwInterpolator.ElevationPoint(0, 0, 42.0)
            };
            var elevation = IdwInterpolator.InterpolateElevation(0, 0, points);
            Assert.Equal(42.0, elevation);
        }

        [Fact]
        public void InterpolateElevation_ReturnsWeightedAverage()
        {
            var points = new List<IdwInterpolator.ElevationPoint>
            {
                new IdwInterpolator.ElevationPoint(1, 0, 10.0),
                new IdwInterpolator.ElevationPoint(-1, 0, 20.0)
            };

            var elevation = IdwInterpolator.InterpolateElevation(0, 0, points, k: 2, power: 2.0);
            Assert.InRange(elevation, 14.9, 15.1);
        }

        [Fact]
        public void InterpolateElevation_HandlesZeroWeights()
        {
            var points = new List<IdwInterpolator.ElevationPoint>
            {
                new IdwInterpolator.ElevationPoint(double.PositiveInfinity, 0, 7.0)
            };

            var elevation = IdwInterpolator.InterpolateElevation(0, 0, points, k: 1, power: 2.0);
            Assert.Equal(7.0, elevation);
        }
    }
}
