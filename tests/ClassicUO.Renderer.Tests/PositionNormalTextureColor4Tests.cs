using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;
using static ClassicUO.Renderer.UltimaBatcher2D;

namespace ClassicUO.Renderer.Tests
{
    public class PositionNormalTextureColor4Tests
    {
        [Fact]
        public void SizeInBytes_Is192()
        {
            // 4 vertices * 4 Vector3 fields * 3 floats * 4 bytes = 192
            PositionNormalTextureColor4.SIZE_IN_BYTES.Should().Be(sizeof(float) * 12 * 4);
        }

        [Fact]
        public void SizeInBytes_Equals192Numerically()
        {
            PositionNormalTextureColor4.SIZE_IN_BYTES.Should().Be(192);
        }

        [Fact]
        public void DefaultConstructor_AllFieldsZero()
        {
            var v = new PositionNormalTextureColor4();

            v.Position0.Should().Be(Vector3.Zero);
            v.Position1.Should().Be(Vector3.Zero);
            v.Position2.Should().Be(Vector3.Zero);
            v.Position3.Should().Be(Vector3.Zero);

            v.Normal0.Should().Be(Vector3.Zero);
            v.TextureCoordinate0.Should().Be(Vector3.Zero);
            v.Hue0.Should().Be(Vector3.Zero);
        }

        [Fact]
        public void CanSetPositionFields()
        {
            var v = new PositionNormalTextureColor4();

            v.Position0 = new Vector3(1, 2, 3);
            v.Position1 = new Vector3(4, 5, 6);
            v.Position2 = new Vector3(7, 8, 9);
            v.Position3 = new Vector3(10, 11, 12);

            v.Position0.Should().Be(new Vector3(1, 2, 3));
            v.Position1.Should().Be(new Vector3(4, 5, 6));
            v.Position2.Should().Be(new Vector3(7, 8, 9));
            v.Position3.Should().Be(new Vector3(10, 11, 12));
        }

        [Fact]
        public void CanSetHueFields()
        {
            var v = new PositionNormalTextureColor4();
            var hue = new Vector3(23, 1, 0.5f);

            v.Hue0 = hue;
            v.Hue1 = hue;
            v.Hue2 = hue;
            v.Hue3 = hue;

            v.Hue0.Should().Be(hue);
            v.Hue1.Should().Be(hue);
            v.Hue2.Should().Be(hue);
            v.Hue3.Should().Be(hue);
        }

        [Fact]
        public void ImplementsIVertexType()
        {
            var v = new PositionNormalTextureColor4();
            var iface = v as Microsoft.Xna.Framework.Graphics.IVertexType;

            iface.Should().NotBeNull();
            iface.VertexDeclaration.Should().NotBeNull();
        }
    }
}
