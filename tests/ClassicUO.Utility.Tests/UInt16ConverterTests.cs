using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class UInt16ConverterTests
    {
        [Fact]
        public void Parse_DecimalString_ReturnsCorrectValue()
        {
            ushort result = UInt16Converter.Parse("12345");

            result.Should().Be(12345);
        }

        [Fact]
        public void Parse_HexString_ReturnsCorrectValue()
        {
            ushort result = UInt16Converter.Parse("0x1234");

            result.Should().Be(0x1234);
        }

        [Fact]
        public void Parse_Zero_ReturnsZero()
        {
            ushort result = UInt16Converter.Parse("0");

            result.Should().Be(0);
        }

        [Fact]
        public void Parse_MaxUshort_ReturnsMaxValue()
        {
            ushort result = UInt16Converter.Parse("65535");

            result.Should().Be(65535);
        }

        [Fact]
        public void Parse_HexMaxUshort_ReturnsMaxValue()
        {
            ushort result = UInt16Converter.Parse("0xFFFF");

            result.Should().Be(0xFFFF);
        }

        [Fact]
        public void Parse_HexLowerCase_ReturnsCorrectValue()
        {
            ushort result = UInt16Converter.Parse("0xff");

            result.Should().Be(0xFF);
        }

        [Fact]
        public void Parse_NegativeValue_ReturnsUshortCast()
        {
            ushort result = UInt16Converter.Parse("-1");

            result.Should().Be(unchecked((ushort)-1));
        }

        [Fact]
        public void Parse_SmallHexValue_ReturnsCorrectValue()
        {
            ushort result = UInt16Converter.Parse("0x0001");

            result.Should().Be(1);
        }
    }
}
