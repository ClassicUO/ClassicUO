using Microsoft.Xna.Framework;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class HuesHelperTests
    {
        // GetBGRA tests

        [Fact]
        public void GetBGRA_DecomposesKnownValue()
        {
            // 0xAABBCCDD: A=0xAA, R=0xBB, G=0xCC, B=0xDD
            // Stored as BGRA in the uint: byte0=B, byte1=G, byte2=R, byte3=A
            var (b, g, r, a) = HuesHelper.GetBGRA(0xAABBCCDD);

            b.Should().Be(0xDD);
            g.Should().Be(0xCC);
            r.Should().Be(0xBB);
            a.Should().Be(0xAA);
        }

        [Fact]
        public void GetBGRA_AllZeros()
        {
            var (b, g, r, a) = HuesHelper.GetBGRA(0x00000000);

            b.Should().Be(0);
            g.Should().Be(0);
            r.Should().Be(0);
            a.Should().Be(0);
        }

        [Fact]
        public void GetBGRA_AllOnes()
        {
            var (b, g, r, a) = HuesHelper.GetBGRA(0xFFFFFFFF);

            b.Should().Be(0xFF);
            g.Should().Be(0xFF);
            r.Should().Be(0xFF);
            a.Should().Be(0xFF);
        }

        [Fact]
        public void GetBGRA_OnlyBlueChannel()
        {
            var (b, g, r, a) = HuesHelper.GetBGRA(0x000000FF);

            b.Should().Be(0xFF);
            g.Should().Be(0);
            r.Should().Be(0);
            a.Should().Be(0);
        }

        // RgbaToArgb tests

        [Fact]
        public void RgbaToArgb_ConvertsCorrectly()
        {
            // RGBA: 0xRRGGBBAA -> ARGB: 0xAARRGGBB
            uint rgba = 0x11223344; // R=0x11, G=0x22, B=0x33, A=0x44
            uint argb = HuesHelper.RgbaToArgb(rgba);

            // (0x11223344 >> 8) | (0x11223344 << 24) = 0x00112233 | 0x44000000 = 0x44112233
            argb.Should().Be(0x44112233);
        }

        [Fact]
        public void RgbaToArgb_AllZeros()
        {
            HuesHelper.RgbaToArgb(0x00000000).Should().Be(0x00000000);
        }

        [Fact]
        public void RgbaToArgb_AllOnes()
        {
            HuesHelper.RgbaToArgb(0xFFFFFFFF).Should().Be(0xFFFFFFFF);
        }

        // Color16To32 tests

        [Fact]
        public void Color16To32_BlackReturnsZero()
        {
            HuesHelper.Color16To32(0).Should().Be(0u);
        }

        [Fact]
        public void Color16To32_MaxValueProducesNonZero()
        {
            // 0x7FFF = all 15 bits set (5-5-5 format, ignoring top bit)
            uint result = HuesHelper.Color16To32(0x7FFF);
            result.Should().BeGreaterThan(0u);
        }

        [Fact]
        public void Color16To32_WhiteProducesExpectedChannels()
        {
            // 0x7FFF: R=31, G=31, B=31 in 5-5-5 format
            uint result = HuesHelper.Color16To32(0x7FFF);

            // Extract channels: Color16To32 returns _table[R] | (_table[G] << 8) | (_table[B] << 16)
            // _table[31] = 0xFF
            byte channel0 = (byte)(result & 0xFF);
            byte channel1 = (byte)((result >> 8) & 0xFF);
            byte channel2 = (byte)((result >> 16) & 0xFF);

            channel0.Should().Be(0xFF);
            channel1.Should().Be(0xFF);
            channel2.Should().Be(0xFF);
        }

        // Color32To16 tests

        [Fact]
        public void Color32To16_BlackReturnsZero()
        {
            HuesHelper.Color32To16(0u).Should().Be((ushort)0);
        }

        [Fact]
        public void Color32To16_WhiteProducesNonZero()
        {
            ushort result = HuesHelper.Color32To16(0x00FFFFFF);
            result.Should().BeGreaterThan((ushort)0);
        }

        // Round-trip test (lossy)

        [Fact]
        public void Color16To32_ThenColor32To16_ApproximateRoundTrip()
        {
            // Due to bit depth differences, the round-trip is lossy.
            // But for values that map cleanly, we should get close.
            ushort original = 0x7FFF; // max 5-5-5 color
            uint color32 = HuesHelper.Color16To32(original);
            ushort backTo16 = HuesHelper.Color32To16(color32);

            // The values should be close but may not be identical due to table lookup vs bit math
            // Extract 5-bit components
            int origR = (original >> 10) & 0x1F;
            int origG = (original >> 5) & 0x1F;
            int origB = original & 0x1F;

            int resultR = (backTo16 >> 10) & 0x1F;
            int resultG = (backTo16 >> 5) & 0x1F;
            int resultB = backTo16 & 0x1F;

            resultR.Should().BeCloseTo(origR, 2);
            resultG.Should().BeCloseTo(origG, 2);
            resultB.Should().BeCloseTo(origB, 2);
        }

        [Fact]
        public void Color16To32_ThenColor32To16_ZeroRoundTrips()
        {
            ushort original = 0;
            uint color32 = HuesHelper.Color16To32(original);
            ushort backTo16 = HuesHelper.Color32To16(color32);

            backTo16.Should().Be(original);
        }

        // ConvertToGray tests

        [Fact]
        public void ConvertToGray_BlackReturnsZero()
        {
            HuesHelper.ConvertToGray(0).Should().Be(0);
        }

        [Fact]
        public void ConvertToGray_ReturnsGrayscaleValue()
        {
            // 5-5-5 format: R bits [14:10], G bits [9:5], B bits [4:0]
            // For a color with known channel values
            ushort color = (ushort)((10 << 10) | (20 << 5) | 5);
            ushort gray = HuesHelper.ConvertToGray(color);

            // gray = (B*299 + G*587 + R*114) / 1000
            // = (5*299 + 20*587 + 10*114) / 1000
            // = (1495 + 11740 + 1140) / 1000
            // = 14375 / 1000 = 14
            gray.Should().Be(14);
        }

        [Fact]
        public void ConvertToGray_WhiteReturnsMaxGray()
        {
            // All channels at 31
            ushort white = 0x7FFF;
            ushort gray = HuesHelper.ConvertToGray(white);

            // (31*299 + 31*587 + 31*114) / 1000 = 31 * 1000 / 1000 = 31
            gray.Should().Be(31);
        }

        // ColorToHue tests

        [Fact]
        public void ColorToHue_BlackReturnsZero()
        {
            var color = new Color(0, 0, 0, 255);
            HuesHelper.ColorToHue(color).Should().Be(0);
        }

        [Fact]
        public void ColorToHue_WhiteReturnsMax()
        {
            var color = new Color(255, 255, 255, 255);
            ushort hue = HuesHelper.ColorToHue(color);

            // 255 * (31/255) = 31 for each channel
            // (31 << 10) | (31 << 5) | 31 = 0x7FFF
            hue.Should().Be(0x7FFF);
        }

        [Fact]
        public void ColorToHue_PureRed()
        {
            var color = new Color(255, 0, 0, 255);
            ushort hue = HuesHelper.ColorToHue(color);

            // R=31, G=0, B=0 => (31 << 10) = 0x7C00
            hue.Should().Be(0x7C00);
        }

        [Fact]
        public void ColorToHue_VerySmallNonZeroChannel_BecomesOne()
        {
            // A very small non-zero value (e.g., 1) scaled by 31/255 ≈ 0.12
            // Truncates to 0, but source code bumps it to 1 if original was non-zero
            var color = new Color(1, 0, 0, 255);
            ushort hue = HuesHelper.ColorToHue(color);

            // R should be 1 (bumped), G=0, B=0
            int r = (hue >> 10) & 0x1F;
            r.Should().Be(1);
        }

        [Fact]
        public void ColorToHue_MidGray()
        {
            var color = new Color(128, 128, 128, 255);
            ushort hue = HuesHelper.ColorToHue(color);

            // 128 * 31/255 ≈ 15.56 => 15
            int r = (hue >> 10) & 0x1F;
            int g = (hue >> 5) & 0x1F;
            int b = hue & 0x1F;

            r.Should().Be(15);
            g.Should().Be(15);
            b.Should().Be(15);
        }
    }
}
