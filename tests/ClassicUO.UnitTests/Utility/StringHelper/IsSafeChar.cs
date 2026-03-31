using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class IsSafeChar
    {
        [Theory]
        [InlineData(0x20, true)]
        [InlineData(0x41, true)]
        [InlineData(0xFFFD, true)]
        [InlineData(0x1F, false)]
        [InlineData(0x00, false)]
        [InlineData(0xFFFE, false)]
        public void IsSafeChar_Should_Return_Expected_Result(int input, bool expected)
        {
            var result = ClassicUO.Utility.StringHelper.IsSafeChar(input);

            result
                .Should()
                .Be(expected);
        }
    }
}
