using ClassicUO.Renderer;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Renderer.Tests
{
    public class SpriteInfoTests
    {
        [Fact]
        public void Empty_HasNullTexture()
        {
            SpriteInfo.Empty.Texture.Should().BeNull();
        }

        [Fact]
        public void Empty_HasDefaultUVAndCenter()
        {
            SpriteInfo.Empty.UV.Should().Be(Rectangle.Empty);
            SpriteInfo.Empty.Center.Should().Be(Point.Zero);
        }

        [Fact]
        public void DefaultConstructor_TextureIsNull()
        {
            var info = new SpriteInfo();

            info.Texture.Should().BeNull();
            info.UV.Should().Be(Rectangle.Empty);
            info.Center.Should().Be(Point.Zero);
        }

        [Fact]
        public void CanSetUVAndCenter()
        {
            var info = new SpriteInfo
            {
                UV = new Rectangle(10, 20, 30, 40),
                Center = new Point(15, 25)
            };

            info.UV.Should().Be(new Rectangle(10, 20, 30, 40));
            info.Center.Should().Be(new Point(15, 25));
        }
    }
}
