using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Utility.MathHelper
{
    public class GetDistance
    {
        [Fact]
        public void GetDistance_Same_Point_Should_Return_Zero()
        {
            var point = new Point(5, 5);

            ClassicUO.Utility.MathHelper.GetDistance(point, point)
                .Should()
                .Be(0);
        }

        [Theory]
        [InlineData(0, 0, 7, 0, 7)]
        [InlineData(3, 0, 10, 0, 7)]
        public void GetDistance_Horizontal_Only_Should_Return_DeltaX(
            int x1, int y1, int x2, int y2, int expected)
        {
            ClassicUO.Utility.MathHelper.GetDistance(new Point(x1, y1), new Point(x2, y2))
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData(0, 0, 0, 5, 5)]
        [InlineData(0, 2, 0, 9, 7)]
        public void GetDistance_Vertical_Only_Should_Return_DeltaY(
            int x1, int y1, int x2, int y2, int expected)
        {
            ClassicUO.Utility.MathHelper.GetDistance(new Point(x1, y1), new Point(x2, y2))
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData(0, 0, 3, 4, 4)]
        [InlineData(0, 0, 5, 3, 5)]
        [InlineData(0, 0, 2, 2, 2)]
        public void GetDistance_Diagonal_Should_Return_Chebyshev_Distance(
            int x1, int y1, int x2, int y2, int expected)
        {
            ClassicUO.Utility.MathHelper.GetDistance(new Point(x1, y1), new Point(x2, y2))
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData(-3, -4, 3, 4, 8)]
        [InlineData(-5, 0, 5, 0, 10)]
        public void GetDistance_Negative_Coordinates_Should_Return_Correct_Distance(
            int x1, int y1, int x2, int y2, int expected)
        {
            ClassicUO.Utility.MathHelper.GetDistance(new Point(x1, y1), new Point(x2, y2))
                .Should()
                .Be(expected);
        }
    }
}
