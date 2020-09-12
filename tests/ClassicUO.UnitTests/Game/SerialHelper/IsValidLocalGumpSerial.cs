using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.SerialHelper
{
    public class IsValidLocalGumpSerial
    {
        [Theory]
        [InlineData(ClassicUO.Game.Constants.JOURNAL_LOCALSERIAL)]
        [InlineData(0xFFFFFFFE)]
        public void IsValidLocalGumpSerial_Should_Not_Legal(uint serial)
        {
            ClassicUO.Game.SerialHelper.IsValidLocalGumpSerial(serial)
                .Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0xFFFFFFE0)]
        [InlineData(0xFFFFFFFF)]
        public void IsValidLocalGumpSerial_Should_Not_BeLegal(uint serial)
        {
            ClassicUO.Game.SerialHelper.IsValidLocalGumpSerial(serial)
                .Should()
                .BeFalse();
        }
    }
}