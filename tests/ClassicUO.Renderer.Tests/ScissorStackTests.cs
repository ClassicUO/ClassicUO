using ClassicUO.Renderer;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Renderer.Tests
{
    public class ScissorStackTests
    {
        // ScissorStack.CalculateScissors is a pure function that transforms
        // a rectangle through a matrix. It does not require a GraphicsDevice.

        [Fact]
        public void CalculateScissors_IdentityMatrix_ReturnsSameRectangle()
        {
            var result = ScissorStack.CalculateScissors(Matrix.Identity, 10, 20, 100, 200);

            result.X.Should().Be(10);
            result.Y.Should().Be(20);
            result.Width.Should().Be(100);
            result.Height.Should().Be(200);
        }

        [Fact]
        public void CalculateScissors_TranslationMatrix_ShiftsRectangle()
        {
            var transform = Matrix.CreateTranslation(50, 100, 0);

            var result = ScissorStack.CalculateScissors(transform, 10, 20, 100, 200);

            result.X.Should().Be(60);  // 10 + 50
            result.Y.Should().Be(120); // 20 + 100
            result.Width.Should().Be(100);
            result.Height.Should().Be(200);
        }

        [Fact]
        public void CalculateScissors_ScaleMatrix_ScalesRectangle()
        {
            var transform = Matrix.CreateScale(2f, 2f, 1f);

            var result = ScissorStack.CalculateScissors(transform, 10, 20, 100, 200);

            result.X.Should().Be(20);  // 10 * 2
            result.Y.Should().Be(40);  // 20 * 2
            result.Width.Should().Be(200); // (10+100)*2 - 20
            result.Height.Should().Be(400); // (20+200)*2 - 40
        }

        [Fact]
        public void CalculateScissors_ZeroSize_ReturnsZeroSizeRectangle()
        {
            var result = ScissorStack.CalculateScissors(Matrix.Identity, 50, 60, 0, 0);

            result.X.Should().Be(50);
            result.Y.Should().Be(60);
            result.Width.Should().Be(0);
            result.Height.Should().Be(0);
        }

        [Fact]
        public void CalculateScissors_OriginZero_CorrectResult()
        {
            var result = ScissorStack.CalculateScissors(Matrix.Identity, 0, 0, 640, 480);

            result.X.Should().Be(0);
            result.Y.Should().Be(0);
            result.Width.Should().Be(640);
            result.Height.Should().Be(480);
        }
    }
}
