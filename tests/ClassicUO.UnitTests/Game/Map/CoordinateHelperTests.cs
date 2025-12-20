using ClassicUO.Game.Map;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Map
{
    public class CoordinateHelperTest
    {
        [Theory]
        [InlineData(0.0f, 0.0f, 0, 0)]
        [InlineData(44.0f, 0.0f, 1, -1)]  // tileX = (44+0)/44 = 1, tileY = (0-44)/44 = -1
        [InlineData(0.0f, 44.0f, 1, 1)]   // tileX = (0+44)/44 = 1, tileY = (44-0)/44 = 1
        [InlineData(44.0f, 44.0f, 2, 0)]  // tileX = (44+44)/44 = 2, tileY = (44-44)/44 = 0
        [InlineData(22.0f, 22.0f, 1, 0)]  // tileX = (22+22)/44 = 1, tileY = (22-22)/44 = 0
        [InlineData(88.0f, 0.0f, 2, -2)]  // tileX = (88+0)/44 = 2, tileY = (0-88)/44 = -2
        [InlineData(0.0f, 88.0f, 2, 2)]   // tileX = (0+88)/44 = 2, tileY = (88-0)/44 = 2
        public void Should_ConvertIsometricToTileCoordinates(
            float worldX, float worldY, int expectedTileX, int expectedTileY)
        {
            (int targetTileX, int targetTileY) = CoordinateHelper.IsometricToTile(worldX, worldY);

            targetTileX.Should().Be(expectedTileX, 
                $"worldX={worldX}, worldY={worldY} should convert to tileX={expectedTileX}");
            targetTileY.Should().Be(expectedTileY,
                $"worldX={worldX}, worldY={worldY} should convert to tileY={expectedTileY}");
        }
        
        [Fact]
        public void Should_HandleNegativeCoordinates()
        {
            float worldX = -44.0f;
            float worldY = -44.0f;

            (int targetTileX, int targetTileY) = CoordinateHelper.IsometricToTile(worldX, worldY);

            targetTileX.Should().Be(-2);
            targetTileY.Should().Be(0);
        }
        
        [Fact]
        public void Should_HandleLargeCoordinates()
        {
            float worldX = 4400.0f;
            float worldY = 4400.0f;

            (int targetTileX, int targetTileY) = CoordinateHelper.IsometricToTile(worldX, worldY);

            targetTileX.Should().Be(200);
            targetTileY.Should().Be(0);
        }
        
        [Theory]
        [InlineData(21.9f, 22.1f, 1, 0)]  // tileX = (21.9+22.1)/44 = 1, tileY = (22.1-21.9)/44 = 0
        [InlineData(22.1f, 21.9f, 1, 0)]  // tileX = (22.1+21.9)/44 = 1, tileY = (21.9-22.1)/44 = 0
        [InlineData(43.9f, 0.1f, 1, -1)]  // tileX = (43.9+0.1)/44 = 1, tileY = (0.1-43.9)/44 = -1
        [InlineData(44.1f, 0.1f, 1, -1)]  // tileX = (44.1+0.1)/44 = 1, tileY = (0.1-44.1)/44 = -1
        public void Should_HandleRoundingCorrectly(
            float worldX, float worldY, int expectedTileX, int expectedTileY)
        {
            (int targetTileX, int targetTileY) = CoordinateHelper.IsometricToTile(worldX, worldY);

            targetTileX.Should().Be(expectedTileX);
            targetTileY.Should().Be(expectedTileY);
        }
    }
}
