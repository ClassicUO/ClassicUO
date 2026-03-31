using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.HuesHelper
{
    public class RgbaToArgb
    {
        [Theory]
        [InlineData(0x11223344u, 0x44112233u)]
        [InlineData(0x00000000u, 0x00000000u)]
        [InlineData(0xFFFFFFFFu, 0xFFFFFFFFu)]
        public void RgbaToArgb_Should_Rotate_Bytes_Correctly(uint input, uint expected)
        {
            var result = ClassicUO.Utility.HuesHelper.RgbaToArgb(input);

            result.Should().Be(expected);
        }
    }
}
