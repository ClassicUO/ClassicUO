using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.HuesHelper
{
    public class ColorConversion
    {
        [Fact]
        public void Color16To32_Black_Should_Return_Zero()
        {
            var result = ClassicUO.Utility.HuesHelper.Color16To32(0x0000);

            result.Should().Be(0x00000000u);
        }

        [Fact]
        public void Color16To32_White_Should_Return_Full_RGB()
        {
            // 0x7FFF = all five-bit channels at max (31)
            // _table[31] = 0xFF for each channel
            // Result: 0xFF | (0xFF << 8) | (0xFF << 16) = 0x00FFFFFF
            var result = ClassicUO.Utility.HuesHelper.Color16To32(0x7FFF);

            result.Should().Be(0x00FFFFFFu);
        }

        [Theory]
        [InlineData((ushort)0x0000)]
        [InlineData((ushort)0x0421)]
        [InlineData((ushort)0x7FFF)]
        public void Color16To32_Then_Color32To16_Should_Approximate_RoundTrip_When_Symmetric(ushort input)
        {
            // Color16To32 stores R in byte0, G in byte1, B in byte2.
            // Color32To16 reads byte0 as low bits, byte2 as high bits,
            // effectively swapping R and B. For symmetric colors (R == B),
            // the round-trip produces the original value.
            var color32 = ClassicUO.Utility.HuesHelper.Color16To32(input);
            var result = ClassicUO.Utility.HuesHelper.Color32To16(color32);

            // These inputs have equal R and B channels, so round-trip is exact
            result.Should().Be(input);
        }

        [Fact]
        public void Color32To16_Should_Reduce_Bit_Depth()
        {
            // 0x00FFFFFF -> each 8-bit channel is 0xFF
            // ((0xFF << 5) >> 8) = 31 for each component
            // Result: 31 | (31 << 10) | (31 << 5) = 31 + 31744 + 992 = 0x7FFF
            var result = ClassicUO.Utility.HuesHelper.Color32To16(0x00FFFFFFu);

            result.Should().Be(0x7FFF);
        }

        [Fact]
        public void ConvertToGray_Black_Should_Return_Zero()
        {
            var result = ClassicUO.Utility.HuesHelper.ConvertToGray(0x0000);

            result.Should().Be(0);
        }

        [Fact]
        public void ConvertToGray_White_Should_Return_MaxChannel()
        {
            // 0x7FFF: B=31, G=31, R=31
            // gray = (31*299 + 31*587 + 31*114) / 1000 = 31*1000/1000 = 31
            var result = ClassicUO.Utility.HuesHelper.ConvertToGray(0x7FFF);

            result.Should().Be(31);
        }

        [Theory]
        [InlineData((ushort)0x0421, (ushort)1)]
        [InlineData((ushort)0x0000, (ushort)0)]
        [InlineData((ushort)0x7FFF, (ushort)31)]
        public void ConvertToGray_Should_Produce_Weighted_Luminance(ushort input, ushort expected)
        {
            // Luminance formula: (B*299 + G*587 + R*114) / 1000
            var result = ClassicUO.Utility.HuesHelper.ConvertToGray(input);

            result.Should().Be(expected);
        }
    }
}
