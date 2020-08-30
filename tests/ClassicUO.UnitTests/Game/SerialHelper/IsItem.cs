using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.SerialHelper
{
    public class IsItem
    {
        [Theory]
        [InlineData(0x40000001)]
        [InlineData(0x7FFFFFFF)]
        public void IsItem_Serial_Should_Be_Legal(uint serial)
        {
            ClassicUO.Game.SerialHelper.IsItem(serial)
                .Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x3FFFFFFF)]
        [InlineData(0x80000000)]
        public void IsItem_Serial_Should_Not_Be_Legal(uint serial)
        {
            ClassicUO.Game.SerialHelper.IsItem(serial)
                .Should()
                .BeFalse();
        }
    }
}