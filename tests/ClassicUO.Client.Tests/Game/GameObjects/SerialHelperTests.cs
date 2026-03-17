using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.GameObjects
{
    public class SerialHelperTests
    {
        [Theory]
        [InlineData(0u, false)]
        [InlineData(1u, true)]
        [InlineData(0x3FFFFFFFu, true)]
        [InlineData(0x7FFFFFFFu, true)]
        [InlineData(0x80000000u, false)]
        [InlineData(0xFFFFFFFFu, false)]
        public void IsValid_ReturnsExpected(uint serial, bool expected)
        {
            SerialHelper.IsValid(serial).Should().Be(expected);
        }

        [Theory]
        [InlineData(1u, true)]
        [InlineData(0x3FFFFFFFu, true)]
        [InlineData(0x40000000u, false)]
        [InlineData(0u, false)]
        [InlineData(0x80000000u, false)]
        public void IsMobile_ReturnsExpected(uint serial, bool expected)
        {
            SerialHelper.IsMobile(serial).Should().Be(expected);
        }

        [Theory]
        [InlineData(0x40000000u, true)]
        [InlineData(0x7FFFFFFFu, true)]
        [InlineData(0x3FFFFFFFu, false)]
        [InlineData(0x80000000u, false)]
        [InlineData(0u, false)]
        public void IsItem_ReturnsExpected(uint serial, bool expected)
        {
            SerialHelper.IsItem(serial).Should().Be(expected);
        }

        [Theory]
        [InlineData("123", 123u)]
        [InlineData("0x1A", 26u)]
        [InlineData("0", 0u)]
        [InlineData("0x0", 0u)]
        [InlineData("0xFF", 255u)]
        [InlineData("4294967295", 4294967295u)]
        public void Parse_ReturnsExpected(string input, uint expected)
        {
            SerialHelper.Parse(input).Should().Be(expected);
        }

        [Fact]
        public void Parse_NegativeString_ReturnsUintRepresentation()
        {
            // "-1" should parse as (uint)(-1) = 0xFFFFFFFF
            SerialHelper.Parse("-1").Should().Be(0xFFFFFFFFu);
        }
    }
}
