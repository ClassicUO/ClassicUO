using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.SerialHelper
{
    public class IsMobile
    {
        [Theory]
        [InlineData(1)]
        [InlineData(0x1FFFFFFF)]
        [InlineData(0x3FFFFFFF)]
        public void IsMobile_Serial_Should_Be_Legal(uint serial)
        {
            ClassicUO.Game.SerialHelper.IsMobile(serial)
                .Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x40000000)]
        [InlineData(0x7FFFFFFF)]
        [InlineData(0x80000000)]
        public void IsMobile_Serial_Should_Not_Be_Legal(uint serial)
        {
            ClassicUO.Game.SerialHelper.IsMobile(serial)
                .Should()
                .BeFalse();
        }
    }
}