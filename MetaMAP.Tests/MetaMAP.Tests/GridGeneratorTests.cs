using System.Linq;
using MetaMap.Core;
using Xunit;

namespace MetaMAP.Tests
{
    public class GridGeneratorTests
    {
        [Fact]
        public void GenerateLocalGridPoints_ReturnsExpectedCount()
        {
            var points = GridGenerator.GenerateLocalGridPoints(41.0, 29.0, 100.0, 3);
            Assert.Equal(9, points.Count);
        }

        [Fact]
        public void GenerateLocalGridPoints_IncludesCenter()
        {
            var points = GridGenerator.GenerateLocalGridPoints(41.0, 29.0, 100.0, 3);
            var hasCenter = points.Any(p => System.Math.Abs(p.X) < 0.001 && System.Math.Abs(p.Y) < 0.001);
            Assert.True(hasCenter);
        }
    }
}
